namespace GridPuzzle.Core
{
    /// <summary>
    /// Contract for deterministic state history tracking and undo management.
    /// Encapsulates history buffer discipline and prevents memory leaks.
    /// </summary>
    public interface IStateHistory
    {
        int HistoryCount { get; }
        int MaxCapacity { get; }
        bool CanUndo { get; }

        void Record(TransitionRecord record);
        bool TryPop(out TransitionRecord record);
        TransitionRecord Peek();
        void Clear();
    }
}
