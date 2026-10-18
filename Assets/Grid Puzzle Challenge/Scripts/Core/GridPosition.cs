using System;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Immutable 2D grid coordinate representing a cell in an N x M matrix.
    /// Provides spatial comparison, hashing, and distance calculations.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int Row { get; }
        public int Col { get; }

        public GridPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool Equals(GridPosition other) => Row == other.Row && Col == other.Col;

        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Row, Col);

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);

        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);

        public GridPosition Move(Direction direction)
        {
            return new GridPosition(Row + direction.ToRowDelta(), Col + direction.ToColDelta());
        }

        public int ManhattanDistanceTo(GridPosition other)
        {
            return Math.Abs(Row - other.Row) + Math.Abs(Col - other.Col);
        }

        public override string ToString() => $"({Row}, {Col})";
    }
}
