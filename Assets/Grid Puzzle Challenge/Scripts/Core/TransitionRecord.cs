using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Delta state for a single tile during a move transition.
    /// Captures only the changed attributes to avoid full object cloning.
    /// </summary>
    public struct TileDelta
    {
        public int TileId { get; set; }
        public GridPosition OriginalPos { get; set; }
        public GridPosition FinalPos { get; set; }
        public int OriginalValue { get; set; }
        public int FinalValue { get; set; }
        public TileType OriginalType { get; set; }
        public int OriginalDurability { get; set; }
        public bool WasMergedAndRemoved { get; set; }
        public int? MergedIntoTileId { get; set; }
    }

    /// <summary>
    /// Lightweight, memory-safe transition record for a single move step.
    /// Uses delta encoding rather than deep-copy snapshots to achieve maximum algorithmic efficiency.
    /// </summary>
    public class TransitionRecord
    {
        public int MoveIndex { get; set; }
        public Direction Direction { get; set; }
        public int ScoreBefore { get; set; }
        public int StepsRemainingBefore { get; set; }
        public int ComboCountBefore { get; set; }
        public float ComboMultiplierBefore { get; set; }

        public List<TileDelta> Deltas { get; } = new List<TileDelta>();
        public GridTile SpawnedTile { get; set; }
        public GridPosition? SpawnPosition { get; set; }

        public void Clear()
        {
            MoveIndex = 0;
            ScoreBefore = 0;
            StepsRemainingBefore = 0;
            ComboCountBefore = 0;
            ComboMultiplierBefore = 1.0f;
            Deltas.Clear();
            SpawnedTile = null;
            SpawnPosition = null;
        }
    }

    /// <summary>
    /// Output data of an Undo operation for the visual layer to reverse animations.
    /// </summary>
    public class UndoResult
    {
        public int MoveIndex { get; set; }
        public int ScoreRestored { get; set; }
        public int StepsRestored { get; set; }
        public int ComboRestored { get; set; }
        public List<TileMotion> ReverseMotions { get; } = new List<TileMotion>();
        public List<int> DespawnedTileIds { get; } = new List<int>();
        public List<(GridPosition Pos, GridTile Tile)> RestoredTiles { get; } = new List<(GridPosition, GridTile)>();

        public void Clear()
        {
            MoveIndex = 0;
            ScoreRestored = 0;
            StepsRestored = 0;
            ComboRestored = 0;
            ReverseMotions.Clear();
            DespawnedTileIds.Clear();
            RestoredTiles.Clear();
        }
    }
}
