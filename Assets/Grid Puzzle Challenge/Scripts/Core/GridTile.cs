using System;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Pure logical representation of a tile entity on the grid matrix.
    /// Fully decoupled from visual MonoBehaviours and rendering frameworks.
    /// Tracks unique identity, numerical value, obstacle state, and merge lifecycle.
    /// </summary>
    public class GridTile : IEquatable<GridTile>
    {
        public int Id { get; }
        public int Value { get; set; }
        public TileType Type { get; set; }
        public int IceDurability { get; set; }
        public int MaxIceDurability { get; }
        public bool IsMergedThisTurn { get; set; }

        public bool IsObstacle => Type == TileType.Stone || Type == TileType.Ice;
        public bool CanSlide => Type == TileType.Normal || Type == TileType.Bomb;
        public bool CanMerge => Type == TileType.Normal && !IsMergedThisTurn;

        public GridTile(int id, int value, TileType type = TileType.Normal, int iceDurability = 0)
        {
            Id = id;
            Value = value;
            Type = type;
            IceDurability = iceDurability;
            MaxIceDurability = iceDurability;
            IsMergedThisTurn = false;
        }

        public static GridTile CreateNormal(int id, int value)
        {
            return new GridTile(id, value, TileType.Normal, 0);
        }

        public static GridTile CreateIce(int id, int value, int durability = 2)
        {
            return new GridTile(id, value, TileType.Ice, durability);
        }

        public static GridTile CreateStone(int id)
        {
            return new GridTile(id, 0, TileType.Stone, 0);
        }

        public static GridTile CreateBomb(int id)
        {
            return new GridTile(id, 0, TileType.Bomb, 0);
        }

        /// <summary>
        /// Applies damage to an ice tile. Returns true if the ice shatters.
        /// </summary>
        public bool DamageIce()
        {
            if (Type != TileType.Ice) return false;
            IceDurability--;
            if (IceDurability <= 0)
            {
                Type = TileType.Normal;
                return true; // Shattered!
            }
            return false;
        }

        public GridTile Clone()
        {
            return new GridTile(Id, Value, Type, IceDurability)
            {
                IsMergedThisTurn = this.IsMergedThisTurn
            };
        }

        public bool Equals(GridTile other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as GridTile);

        public override int GetHashCode() => Id;

        public override string ToString()
        {
            return Type switch
            {
                TileType.Ice => $"IceTile[Id={Id}, Val={Value}, HP={IceDurability}]",
                TileType.Stone => $"StoneTile[Id={Id}]",
                TileType.Bomb => $"BombTile[Id={Id}]",
                _ => $"Tile[Id={Id}, Val={Value}]"
            };
        }
    }
}
