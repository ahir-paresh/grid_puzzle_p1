using System;
using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// High-performance N x M grid simulation model.
    /// Manages logical spatial tracking using a flat array for cache locality.
    /// Completely independent from visual rendering frameworks and GameObjects.
    /// </summary>
    public class GridMatrix
    {
        public int Rows { get; }
        public int Columns { get; }
        public int TotalCells => Rows * Columns;

        private readonly GridTile[] _cells;

        public GridMatrix(int rows, int columns)
        {
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be > 0");
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be > 0");

            Rows = rows;
            Columns = columns;
            _cells = new GridTile[rows * columns];
        }

        public int ToIndex(int row, int col) => row * Columns + col;

        public int ToIndex(GridPosition pos) => pos.Row * Columns + pos.Col;

        public GridPosition ToPosition(int index) => new GridPosition(index / Columns, index % Columns);

        public bool InBounds(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Columns;

        public bool InBounds(GridPosition pos) => InBounds(pos.Row, pos.Col);

        public GridTile GetTile(int row, int col)
        {
            if (!InBounds(row, col)) return null;
            return _cells[ToIndex(row, col)];
        }

        public GridTile GetTile(GridPosition pos) => GetTile(pos.Row, pos.Col);

        public void SetTile(int row, int col, GridTile tile)
        {
            if (!InBounds(row, col))
                throw new ArgumentOutOfRangeException($"Coordinate ({row}, {col}) out of grid bounds ({Rows}x{Columns})");
            _cells[ToIndex(row, col)] = tile;
        }

        public void SetTile(GridPosition pos, GridTile tile) => SetTile(pos.Row, pos.Col, tile);

        public GridTile RemoveTile(GridPosition pos)
        {
            if (!InBounds(pos)) return null;
            int idx = ToIndex(pos);
            var tile = _cells[idx];
            _cells[idx] = null;
            return tile;
        }

        public bool IsEmpty(GridPosition pos)
        {
            return InBounds(pos) && _cells[ToIndex(pos)] == null;
        }

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
        }

        public void ResetMergeFlags()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].IsMergedThisTurn = false;
                }
            }
        }

        public List<GridPosition> GetEmptyPositions(List<GridPosition> buffer = null)
        {
            var list = buffer ?? new List<GridPosition>(_cells.Length);
            list.Clear();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (_cells[ToIndex(r, c)] == null)
                    {
                        list.Add(new GridPosition(r, c));
                    }
                }
            }
            return list;
        }

        public List<(GridPosition Pos, GridTile Tile)> GetAllOccupied(List<(GridPosition, GridTile)> buffer = null)
        {
            var list = buffer ?? new List<(GridPosition, GridTile)>(_cells.Length);
            list.Clear();
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    var tile = _cells[ToIndex(r, c)];
                    if (tile != null)
                    {
                        list.Add((new GridPosition(r, c), tile));
                    }
                }
            }
            return list;
        }

        public GridMatrix Clone()
        {
            var clone = new GridMatrix(Rows, Columns);
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != null)
                {
                    clone._cells[i] = _cells[i].Clone();
                }
            }
            return clone;
        }
    }
}
