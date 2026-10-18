namespace GridPuzzle.Core
{
    /// <summary>
    /// Value object snapshot of all primary system metrics for UI presentation and analytics.
    /// </summary>
    public struct GameMetrics
    {
        public int Score { get; set; }
        public int BestScore { get; set; }
        public int MovesMade { get; set; }
        public int StepsRemaining { get; set; }
        public int InitialStepBudget { get; set; }
        public int TargetScore { get; set; }
        public int IceObstaclesRemaining { get; set; }
        public int ComboCount { get; set; }
        public float ComboMultiplier { get; set; }
        public bool CanUndo { get; set; }
        public int UndoCount { get; set; }
        public GameState State { get; set; }
        public GameMode Mode { get; set; }
    }
}
