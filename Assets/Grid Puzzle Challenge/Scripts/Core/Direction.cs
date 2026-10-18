using System;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Discrete directional commands supported by the swipe and input tracking pipeline.
    /// </summary>
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExtensions
    {
        /// <summary>
        /// Row delta for the direction (assuming Row 0 is bottom, Up is +1, or Row 0 is top, Up is -1).
        /// Standard 2D grid matrix convention:
        /// Up: row - 1 (towards row 0 at top) or row + 1.
        /// Let's use standard screen coordinate / board convention:
        /// Up: Row + 1, Down: Row - 1, Left: Col - 1, Right: Col + 1.
        /// </summary>
        public static int ToRowDelta(this Direction direction)
        {
            return direction switch
            {
                Direction.Up => 1,
                Direction.Down => -1,
                Direction.Left => 0,
                Direction.Right => 0,
                _ => 0
            };
        }

        public static int ToColDelta(this Direction direction)
        {
            return direction switch
            {
                Direction.Up => 0,
                Direction.Down => 0,
                Direction.Left => -1,
                Direction.Right => 1,
                _ => 0
            };
        }

        public static Direction Opposite(this Direction direction)
        {
            return direction switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public static bool IsHorizontal(this Direction direction) =>
            direction == Direction.Left || direction == Direction.Right;

        public static bool IsVertical(this Direction direction) =>
            direction == Direction.Up || direction == Direction.Down;
    }
}
