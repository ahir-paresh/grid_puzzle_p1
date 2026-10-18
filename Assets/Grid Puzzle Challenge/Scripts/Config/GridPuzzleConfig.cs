using UnityEngine;

namespace GridPuzzle.Config
{
    /// <summary>
    /// ScriptableObject defining all gameplay parameters, visual styling,
    /// animation timings, and tier color schemes for the Grid Puzzle Challenge.
    /// </summary>
    [CreateAssetMenu(fileName = "GridPuzzleConfig", menuName = "Grid Puzzle/Config", order = 1)]
    public class GridPuzzleConfig : ScriptableObject
    {
        [Header("Grid Geometry")]
        [Range(3, 8)] public int rows = 4;
        [Range(3, 8)] public int columns = 4;
        public float cellSize = 1.6f;
        public float cellSpacing = 0.2f;

        [Header("Challenge Rules")]
        public int initialStepBudget = 30;
        public int targetScore = 2048;
        public int targetTileValue = 2048;
        public int startingTileCount = 2;
        public int startingIceCount = 3;
        public int startingStoneCount = 1;

        [Header("Animation Durations")]
        public float slideDuration = 0.14f;
        public float spawnScaleDuration = 0.18f;
        public float mergePopDuration = 0.16f;
        public float shatterDuration = 0.25f;

        [Header("Visual Colors - Board")]
        public Color boardBackgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        public Color cellSlotColor = new Color(0.18f, 0.21f, 0.27f, 1f);

        [Header("Visual Colors - Obstacles")]
        public Color iceColorIntact = new Color(0.35f, 0.75f, 0.95f, 0.9f);
        public Color iceColorCracked = new Color(0.65f, 0.88f, 1.0f, 0.85f);
        public Color stoneColor = new Color(0.3f, 0.32f, 0.35f, 1f);
        public Color bombColor = new Color(0.9f, 0.25f, 0.2f, 1f);

        [Header("Tile Tier Colors")]
        public Color tile2 = new Color(0.93f, 0.89f, 0.85f, 1f);
        public Color tile4 = new Color(0.93f, 0.88f, 0.78f, 1f);
        public Color tile8 = new Color(0.95f, 0.69f, 0.47f, 1f);
        public Color tile16 = new Color(0.96f, 0.58f, 0.39f, 1f);
        public Color tile32 = new Color(0.96f, 0.48f, 0.37f, 1f);
        public Color tile64 = new Color(0.96f, 0.37f, 0.23f, 1f);
        public Color tile128 = new Color(0.93f, 0.81f, 0.45f, 1f);
        public Color tile256 = new Color(0.93f, 0.80f, 0.38f, 1f);
        public Color tile512 = new Color(0.93f, 0.78f, 0.31f, 1f);
        public Color tile1024 = new Color(0.93f, 0.77f, 0.25f, 1f);
        public Color tile2048 = new Color(0.93f, 0.76f, 0.18f, 1f);
        public Color tileSuper = new Color(0.38f, 0.25f, 0.72f, 1f);

        public Color GetTileColor(int value)
        {
            return value switch
            {
                2 => tile2,
                4 => tile4,
                8 => tile8,
                16 => tile16,
                32 => tile32,
                64 => tile64,
                128 => tile128,
                256 => tile256,
                512 => tile512,
                1024 => tile1024,
                2048 => tile2048,
                _ => tileSuper
            };
        }

        public Color GetTextColor(int value)
        {
            return value <= 4
                ? new Color(0.42f, 0.38f, 0.35f, 1f)
                : Color.white;
        }
    }
}
