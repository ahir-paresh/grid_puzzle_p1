namespace GridPuzzle.Core
{
    public enum GameState
    {
        Ready,
        Playing,
        Animating,
        Won,
        GameOver,
        Paused
    }

    public enum GameMode
    {
        PuzzleChallenge, // Rigid step framework with target goal & obstacles
        ClassicEndless   // High-score mode with undo support
    }
}
