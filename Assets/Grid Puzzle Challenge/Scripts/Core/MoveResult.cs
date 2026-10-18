using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Tracks individual tile movement and merge dynamics during a swipe transition.
    /// Used by visual view layers to interpolate animations deterministically.
    /// </summary>
    public struct TileMotion
    {
        public int TileId { get; set; }
        public GridPosition From { get; set; }
        public GridPosition To { get; set; }
        public int? MergedIntoTileId { get; set; }
        public int PromotedValue { get; set; }
        public int ScoreGained { get; set; }

        public bool IsMerge => MergedIntoTileId.HasValue;

        public override string ToString() =>
            IsMerge
                ? $"Tile {TileId} [{From} -> {To}] MERGED into {MergedIntoTileId.Value} (NewVal={PromotedValue})"
                : $"Tile {TileId} [{From} -> {To}]";
    }

    /// <summary>
    /// Event data for damage dealt to frozen or special obstacles during a swipe.
    /// </summary>
    public struct ObstacleDamageEvent
    {
        public int ObstacleId { get; set; }
        public GridPosition Position { get; set; }
        public int RemainingDurability { get; set; }
        public bool Shattered { get; set; }
        public int BonusScore { get; set; }
    }

    /// <summary>
    /// Encapsulates the complete logical outcome of a directional swipe transition.
    /// </summary>
    public class MoveResult
    {
        public Direction Direction { get; set; }
        public bool HasMoved { get; set; }
        public List<TileMotion> Motions { get; } = new List<TileMotion>();
        public List<ObstacleDamageEvent> ObstacleDamages { get; } = new List<ObstacleDamageEvent>();
        public GridTile SpawnedTile { get; set; }
        public GridPosition? SpawnPosition { get; set; }
        public int BaseScoreGained { get; set; }
        public int BonusScoreGained { get; set; }
        public int TotalScoreGained { get; set; }
        public int ComboCount { get; set; }
        public float ComboMultiplier { get; set; }
        public int MergesCount { get; set; }

        public void Clear()
        {
            HasMoved = false;
            Motions.Clear();
            ObstacleDamages.Clear();
            SpawnedTile = null;
            SpawnPosition = null;
            BaseScoreGained = 0;
            BonusScoreGained = 0;
            TotalScoreGained = 0;
            ComboCount = 0;
            ComboMultiplier = 1.0f;
            MergesCount = 0;
        }
    }
}
