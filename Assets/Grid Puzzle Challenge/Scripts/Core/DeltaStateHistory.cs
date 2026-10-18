using System;
using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Memory-safe, zero-allocation bounded circular buffer for state transition history.
    /// Reuses pooled TransitionRecord instances to guarantee O(1) time and eliminate GC pressure.
    /// </summary>
    public class DeltaStateHistory : IStateHistory
    {
        private readonly TransitionRecord[] _buffer;
        private readonly Queue<TransitionRecord> _recordPool;
        private int _head;
        private int _tail;
        private int _count;

        public int HistoryCount => _count;
        public int MaxCapacity { get; }
        public bool CanUndo => _count > 0;

        public DeltaStateHistory(int maxCapacity = 30)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity must be > 0");

            MaxCapacity = maxCapacity;
            _buffer = new TransitionRecord[maxCapacity];
            _recordPool = new Queue<TransitionRecord>(maxCapacity + 5);

            for (int i = 0; i < maxCapacity + 5; i++)
            {
                _recordPool.Enqueue(new TransitionRecord());
            }

            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public TransitionRecord ObtainRecord()
        {
            if (_recordPool.Count > 0)
            {
                var rec = _recordPool.Dequeue();
                rec.Clear();
                return rec;
            }
            return new TransitionRecord();
        }

        public void ReleaseRecord(TransitionRecord record)
        {
            if (record == null) return;
            record.Clear();
            _recordPool.Enqueue(record);
        }

        public void Record(TransitionRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            if (_count == MaxCapacity)
            {
                // Overwriting oldest entry: reclaim record to pool
                var overwritten = _buffer[_head];
                if (overwritten != null)
                {
                    ReleaseRecord(overwritten);
                }
                _head = (_head + 1) % MaxCapacity;
            }
            else
            {
                _count++;
            }

            _buffer[_tail] = record;
            _tail = (_tail + 1) % MaxCapacity;
        }

        public bool TryPop(out TransitionRecord record)
        {
            if (_count == 0)
            {
                record = null;
                return false;
            }

            _tail = (_tail - 1 + MaxCapacity) % MaxCapacity;
            record = _buffer[_tail];
            _buffer[_tail] = null;
            _count--;
            return true;
        }

        public TransitionRecord Peek()
        {
            if (_count == 0) return null;
            int lastIndex = (_tail - 1 + MaxCapacity) % MaxCapacity;
            return _buffer[lastIndex];
        }

        public void Clear()
        {
            while (_count > 0)
            {
                if (TryPop(out var rec))
                {
                    ReleaseRecord(rec);
                }
            }
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
