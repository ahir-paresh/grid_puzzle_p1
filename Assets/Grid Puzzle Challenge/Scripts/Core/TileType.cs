namespace GridPuzzle.Core
{
    /// <summary>
    /// Categorizes tiles according to their gameplay behaviors and physics in the grid.
    /// </summary>
    public enum TileType
    {
        Normal,
        Ice,        // Frozen obstacle: blocks sliding until damaged/shattered
        Stone,      // Immovable obstacle: completely impassable wall
        Bomb        // Power-up tile: can be detonated to clear space or obstacles
    }
}
