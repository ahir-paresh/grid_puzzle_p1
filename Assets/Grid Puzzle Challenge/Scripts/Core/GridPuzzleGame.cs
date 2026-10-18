using System;
using System.Collections.Generic;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Pure logical coordinator for the Grid Puzzle Challenge.
    /// Completely decoupled from visual rendering frameworks (zero UnityEngine dependencies).
    /// Enforces N x M grid physics, deterministic swipe evaluation, delta state history,
    /// synergistic combos, obstacle interactions, and win/loss rules.
    /// </summary>
    public class GridPuzzleGame
    {
        public GridMatrix Matrix { get; }
        public IStateHistory History { get; }

        public GameMode Mode { get; private set; }
        public GameState State { get; private set; }

        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public int MovesMade { get; private set; }
        public int StepsRemaining { get; private set; }
        public int InitialStepBudget { get; private set; }
        public int TargetScore { get; private set; }
        public int TargetTileValue { get; private set; }

        public int ComboCount { get; private set; }
        public float ComboMultiplier { get; private set; } = 1.0f;

        private int _nextTileId = 1;
        private readonly Random _random;

        // Reusable buffers to avoid heap allocation during move loops
        private readonly List<GridPosition> _emptyPosBuffer;
        private readonly List<(GridPosition Pos, GridTile Tile)> _occupiedBuffer;

        // Events for UI and View layer observers
        public event Action<MoveResult> OnMoveExecuted;
        public event Action<UndoResult> OnStateUndone;
        public event Action<GameMetrics> OnMetricsChanged;
        public event Action<GameState> OnStateChanged;
        public event Action<int, float> OnComboSurge; // (comboCount, multiplier)
        public event Action<GridPosition> OnTileSpawned;

        public GridPuzzleGame(
            int rows = 4,
            int columns = 4,
            GameMode mode = GameMode.PuzzleChallenge,
            int stepBudget = 30,
            int targetScore = 2048,
            int targetTileValue = 2048,
            int maxHistoryDepth = 30,
            int? seed = null)
        {
            Matrix = new GridMatrix(rows, columns);
            History = new DeltaStateHistory(maxHistoryDepth);
            Mode = mode;
            InitialStepBudget = stepBudget;
            StepsRemaining = stepBudget;
            TargetScore = targetScore;
            TargetTileValue = targetTileValue;

            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _emptyPosBuffer = new List<GridPosition>(rows * columns);
            _occupiedBuffer = new List<(GridPosition, GridTile)>(rows * columns);

            State = GameState.Ready;
        }

        public void Initialize(int startingTileCount = 2, int iceObstacleCount = 0, int stoneObstacleCount = 0)
        {
            Matrix.Clear();
            History.Clear();

            Score = 0;
            MovesMade = 0;
            StepsRemaining = InitialStepBudget;
            ComboCount = 0;
            ComboMultiplier = 1.0f;
            _nextTileId = 1;

            // 1. Spawn Stone Obstacles
            for (int i = 0; i < stoneObstacleCount; i++)
            {
                var empty = Matrix.GetEmptyPositions(_emptyPosBuffer);
                if (empty.Count == 0) break;
                var pos = empty[_random.Next(empty.Count)];
                var stone = GridTile.CreateStone(_nextTileId++);
                Matrix.SetTile(pos, stone);
            }

            // 2. Spawn Ice Obstacles
            for (int i = 0; i < iceObstacleCount; i++)
            {
                var empty = Matrix.GetEmptyPositions(_emptyPosBuffer);
                if (empty.Count == 0) break;
                var pos = empty[_random.Next(empty.Count)];
                int iceVal = _random.Next(0, 2) == 0 ? 2 : 4;
                var ice = GridTile.CreateIce(_nextTileId++, iceVal, durability: 2);
                Matrix.SetTile(pos, ice);
            }

            // 3. Spawn Initial Normal Tiles
            for (int i = 0; i < startingTileCount; i++)
            {
                SpawnRandomTile();
            }

            State = GameState.Playing;
            NotifyMetricsChanged();
            OnStateChanged?.Invoke(State);
        }

        /// <summary>
        /// Executes a discrete directional swipe command.
        /// Evaluates spatial motion, obstacle damage, tile merges, combo multipliers,
        /// and records deterministic delta history for zero-allocation undo.
        /// </summary>
        public bool TryExecuteMove(Direction direction, out MoveResult result)
        {
            result = new MoveResult { Direction = direction };

            if (State != GameState.Playing)
            {
                return false;
            }

            Matrix.ResetMergeFlags();

            var record = History is DeltaStateHistory deltaHist
                ? deltaHist.ObtainRecord()
                : new TransitionRecord();

            record.MoveIndex = MovesMade + 1;
            record.Direction = direction;
            record.ScoreBefore = Score;
            record.StepsRemainingBefore = StepsRemaining;
            record.ComboCountBefore = ComboCount;
            record.ComboMultiplierBefore = ComboMultiplier;

            int mergesCount = 0;
            int baseScoreGain = 0;
            int bonusScoreGain = 0;
            bool anyMovedOrMerged = false;

            // Scan order:
            // Up: row N-2 down to 0 (top row is N-1 in +Row Up convention)
            // Down: row 1 to N-1
            // Right: col M-2 down to 0 (right col is M-1 in +Col Right convention)
            // Left: col 1 to M-1
            int rowStart = direction == Direction.Up ? Matrix.Rows - 2 : (direction == Direction.Down ? 1 : 0);
            int rowEnd = direction == Direction.Up ? -1 : (direction == Direction.Down ? Matrix.Rows : Matrix.Rows);
            int rowStep = direction == Direction.Up ? -1 : 1;

            int colStart = direction == Direction.Right ? Matrix.Columns - 2 : (direction == Direction.Left ? 1 : 0);
            int colEnd = direction == Direction.Right ? -1 : (direction == Direction.Left ? Matrix.Columns : Matrix.Columns);
            int colStep = direction == Direction.Right ? -1 : 1;

            // Track initial tile states before move for delta recording
            var preMoveTiles = Matrix.GetAllOccupied(_occupiedBuffer);
            var preMoveMap = new Dictionary<int, (GridPosition Pos, GridTile Tile)>(preMoveTiles.Count);
            foreach (var item in preMoveTiles)
            {
                preMoveMap[item.Tile.Id] = (item.Pos, item.Tile.Clone());
            }

            // Directional traversal
            for (int r = rowStart; r != rowEnd; r += rowStep)
            {
                for (int c = colStart; c != colEnd; c += colStep)
                {
                    var currentPos = new GridPosition(r, c);
                    var currentTile = Matrix.GetTile(currentPos);

                    if (currentTile == null || !currentTile.CanSlide)
                        continue;

                    var scanPos = currentPos;
                    var nextPos = scanPos.Move(direction);

                    while (Matrix.InBounds(nextPos))
                    {
                        var targetTile = Matrix.GetTile(nextPos);

                        if (targetTile == null)
                        {
                            // Move into empty space
                            Matrix.SetTile(scanPos, null);
                            Matrix.SetTile(nextPos, currentTile);
                            scanPos = nextPos;
                            nextPos = scanPos.Move(direction);
                            anyMovedOrMerged = true;
                        }
                        else
                        {
                            // Collided with occupied cell
                            if (targetTile.Type == TileType.Ice)
                            {
                                // Damage ice obstacle
                                bool shattered = targetTile.DamageIce();
                                int bonus = shattered ? 50 : 15;
                                bonusScoreGain += bonus;
                                anyMovedOrMerged = true;

                                result.ObstacleDamages.Add(new ObstacleDamageEvent
                                {
                                    ObstacleId = targetTile.Id,
                                    Position = nextPos,
                                    RemainingDurability = targetTile.IceDurability,
                                    Shattered = shattered,
                                    BonusScore = bonus
                                });

                                break; // Stop sliding at current cell
                            }
                            else if (targetTile.Type == TileType.Stone)
                            {
                                // Impassable obstacle
                                break;
                            }
                            else if (targetTile.CanMerge && currentTile.CanMerge && targetTile.Value == currentTile.Value)
                            {
                                // Merge!
                                Matrix.SetTile(scanPos, null);
                                targetTile.Value *= 2;
                                targetTile.IsMergedThisTurn = true;

                                mergesCount++;
                                baseScoreGain += targetTile.Value;
                                anyMovedOrMerged = true;

                                result.Motions.Add(new TileMotion
                                {
                                    TileId = currentTile.Id,
                                    From = currentPos,
                                    To = nextPos,
                                    MergedIntoTileId = targetTile.Id,
                                    PromotedValue = targetTile.Value,
                                    ScoreGained = targetTile.Value
                                });

                                // Record delta for the merged-away tile
                                record.Deltas.Add(new TileDelta
                                {
                                    TileId = currentTile.Id,
                                    OriginalPos = currentPos,
                                    FinalPos = nextPos,
                                    OriginalValue = currentTile.Value,
                                    FinalValue = currentTile.Value,
                                    OriginalType = currentTile.Type,
                                    OriginalDurability = currentTile.IceDurability,
                                    WasMergedAndRemoved = true,
                                    MergedIntoTileId = targetTile.Id
                                });

                                break;
                            }
                            else
                            {
                                // Blocked by different value tile
                                break;
                            }
                        }
                    }

                    // If tile slid to a new position without merging
                    if (scanPos != currentPos && Matrix.GetTile(scanPos) == currentTile)
                    {
                        result.Motions.Add(new TileMotion
                        {
                            TileId = currentTile.Id,
                            From = currentPos,
                            To = scanPos,
                            MergedIntoTileId = null,
                            PromotedValue = currentTile.Value,
                            ScoreGained = 0
                        });
                    }
                }
            }

            if (!anyMovedOrMerged)
            {
                result.HasMoved = false;
                if (History is DeltaStateHistory dHist) dHist.ReleaseRecord(record);
                return false;
            }

            // Record deltas for all surviving tiles that changed position, value, or durability
            var postMoveTiles = Matrix.GetAllOccupied(_occupiedBuffer);
            foreach (var item in postMoveTiles)
            {
                if (preMoveMap.TryGetValue(item.Tile.Id, out var pre))
                {
                    if (pre.Pos != item.Pos || pre.Tile.Value != item.Tile.Value || pre.Tile.IceDurability != item.Tile.IceDurability || pre.Tile.Type != item.Tile.Type)
                    {
                        record.Deltas.Add(new TileDelta
                        {
                            TileId = item.Tile.Id,
                            OriginalPos = pre.Pos,
                            FinalPos = item.Pos,
                            OriginalValue = pre.Tile.Value,
                            FinalValue = item.Tile.Value,
                            OriginalType = pre.Tile.Type,
                            OriginalDurability = pre.Tile.IceDurability,
                            WasMergedAndRemoved = false,
                            MergedIntoTileId = null
                        });
                    }
                }
            }

            // Calculate Combos
            if (mergesCount > 0)
            {
                ComboCount++;
                // Multiplier scales: 1.0 -> 1.5 -> 2.0 -> 2.5 -> max 4.0
                ComboMultiplier = Math.Min(4.0f, 1.0f + (ComboCount - 1) * 0.5f);
                if (mergesCount > 1)
                {
                    // Synergistic cascade bonus for multiple merges in single swipe
                    bonusScoreGain += (mergesCount - 1) * 20;
                }
                OnComboSurge?.Invoke(ComboCount, ComboMultiplier);
            }
            else
            {
                ComboCount = 0;
                ComboMultiplier = 1.0f;
            }

            int totalMoveScore = (int)(baseScoreGain * ComboMultiplier) + bonusScoreGain;
            Score += totalMoveScore;
            if (Score > BestScore) BestScore = Score;

            MovesMade++;
            StepsRemaining--;

            result.HasMoved = true;
            result.BaseScoreGained = baseScoreGain;
            result.BonusScoreGained = bonusScoreGain;
            result.TotalScoreGained = totalMoveScore;
            result.ComboCount = ComboCount;
            result.ComboMultiplier = ComboMultiplier;
            result.MergesCount = mergesCount;

            // Spawn new tile
            var spawned = SpawnRandomTile();
            if (spawned.HasValue)
            {
                result.SpawnedTile = spawned.Value.Tile;
                result.SpawnPosition = spawned.Value.Pos;
                record.SpawnedTile = spawned.Value.Tile;
                record.SpawnPosition = spawned.Value.Pos;
            }

            // Save transition history
            History.Record(record);

            // Check Win / Game Over conditions
            EvaluateGameState();

            NotifyMetricsChanged();
            OnMoveExecuted?.Invoke(result);

            return true;
        }

        /// <summary>
        /// Reverses the most recent board transition using delta history.
        /// Guarantees zero deep-copy overhead and mathematically deterministic state restoration.
        /// </summary>
        public bool TryUndo(out UndoResult result)
        {
            result = new UndoResult();

            if (!History.TryPop(out var record))
            {
                return false;
            }

            // 1. Remove spawned tile
            if (record.SpawnPosition.HasValue && record.SpawnedTile != null)
            {
                var currentAtSpawn = Matrix.GetTile(record.SpawnPosition.Value);
                if (currentAtSpawn != null && currentAtSpawn.Id == record.SpawnedTile.Id)
                {
                    Matrix.RemoveTile(record.SpawnPosition.Value);
                    result.DespawnedTileIds.Add(record.SpawnedTile.Id);
                }
            }

            // 2. Revert deltas in reverse
            for (int i = record.Deltas.Count - 1; i >= 0; i--)
            {
                var delta = record.Deltas[i];

                if (delta.WasMergedAndRemoved)
                {
                    // Recreate the absorbed tile at its original position
                    var restoredTile = new GridTile(delta.TileId, delta.OriginalValue, delta.OriginalType, delta.OriginalDurability);
                    Matrix.SetTile(delta.OriginalPos, restoredTile);
                    result.RestoredTiles.Add((delta.OriginalPos, restoredTile));

                    result.ReverseMotions.Add(new TileMotion
                    {
                        TileId = delta.TileId,
                        From = delta.FinalPos,
                        To = delta.OriginalPos,
                        PromotedValue = delta.OriginalValue
                    });
                }
                else
                {
                    // Surviving tile: move back to OriginalPos and restore original attributes
                    var tile = Matrix.GetTile(delta.FinalPos);
                    if (tile != null && tile.Id == delta.TileId)
                    {
                        Matrix.SetTile(delta.FinalPos, null);
                        tile.Value = delta.OriginalValue;
                        tile.Type = delta.OriginalType;
                        tile.IceDurability = delta.OriginalDurability;
                        Matrix.SetTile(delta.OriginalPos, tile);

                        if (delta.OriginalPos != delta.FinalPos)
                        {
                            result.ReverseMotions.Add(new TileMotion
                            {
                                TileId = tile.Id,
                                From = delta.FinalPos,
                                To = delta.OriginalPos,
                                PromotedValue = delta.OriginalValue
                            });
                        }
                    }
                }
            }

            // 3. Restore metrics
            Score = record.ScoreBefore;
            StepsRemaining = record.StepsRemainingBefore;
            ComboCount = record.ComboCountBefore;
            ComboMultiplier = record.ComboMultiplierBefore;
            MovesMade--;

            result.MoveIndex = MovesMade;
            result.ScoreRestored = Score;
            result.StepsRestored = StepsRemaining;
            result.ComboRestored = ComboCount;

            // If game was over or won, return to playing state
            if (State == GameState.GameOver || State == GameState.Won)
            {
                State = GameState.Playing;
                OnStateChanged?.Invoke(State);
            }

            if (History is DeltaStateHistory deltaHist)
            {
                deltaHist.ReleaseRecord(record);
            }

            NotifyMetricsChanged();
            OnStateUndone?.Invoke(result);

            return true;
        }

        /// <summary>
        /// Tactical power-up: Destroys tile or obstacle at specified coordinate.
        /// </summary>
        public bool TryUseBomb(GridPosition pos, out GridTile blastedTile)
        {
            blastedTile = null;
            if (State != GameState.Playing || !Matrix.InBounds(pos)) return false;

            var tile = Matrix.GetTile(pos);
            if (tile == null) return false;

            blastedTile = Matrix.RemoveTile(pos);

            // Record as power-up move
            Score += 25;
            NotifyMetricsChanged();
            return true;
        }

        /// <summary>
        /// Tactical power-up: Scrambles all active sliding tiles to break stalemates.
        /// </summary>
        public bool TryUseShuffle()
        {
            if (State != GameState.Playing) return false;

            var occupied = Matrix.GetAllOccupied(_occupiedBuffer);
            var movable = new List<(GridPosition Pos, GridTile Tile)>();
            foreach (var item in occupied)
            {
                if (item.Tile.CanSlide) movable.Add(item);
            }

            if (movable.Count <= 1) return false;

            // Collect tiles and positions
            var positions = new List<GridPosition>(movable.Count);
            var tiles = new List<GridTile>(movable.Count);
            foreach (var item in movable)
            {
                positions.Add(item.Pos);
                tiles.Add(item.Tile);
                Matrix.SetTile(item.Pos, null);
            }

            // Fisher-Yates shuffle
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int swapIdx = _random.Next(i + 1);
                var temp = tiles[i];
                tiles[i] = tiles[swapIdx];
                tiles[swapIdx] = temp;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                Matrix.SetTile(positions[i], tiles[i]);
            }

            NotifyMetricsChanged();
            return true;
        }

        public (GridPosition Pos, GridTile Tile)? SpawnRandomTile(TileType type = TileType.Normal)
        {
            var empty = Matrix.GetEmptyPositions(_emptyPosBuffer);
            if (empty.Count == 0) return null;

            var pos = empty[_random.Next(empty.Count)];
            int val = (_random.NextDouble() < 0.9) ? 2 : 4;
            var tile = type switch
            {
                TileType.Ice => GridTile.CreateIce(_nextTileId++, val, 2),
                TileType.Stone => GridTile.CreateStone(_nextTileId++),
                TileType.Bomb => GridTile.CreateBomb(_nextTileId++),
                _ => GridTile.CreateNormal(_nextTileId++, val)
            };

            Matrix.SetTile(pos, tile);
            OnTileSpawned?.Invoke(pos);
            return (pos, tile);
        }

        public int CountRemainingIceObstacles()
        {
            int count = 0;
            var all = Matrix.GetAllOccupied(_occupiedBuffer);
            foreach (var item in all)
            {
                if (item.Tile.Type == TileType.Ice) count++;
            }
            return count;
        }

        public bool HasAnyValidMove()
        {
            // If any cell is empty, at least one move/slide is possible
            var empty = Matrix.GetEmptyPositions(_emptyPosBuffer);
            if (empty.Count > 0) return true;

            // Otherwise check if any adjacent movable tiles have identical values
            for (int r = 0; r < Matrix.Rows; r++)
            {
                for (int c = 0; c < Matrix.Columns; c++)
                {
                    var current = Matrix.GetTile(r, c);
                    if (current == null || !current.CanSlide) continue;

                    // Check Right neighbor
                    if (c + 1 < Matrix.Columns)
                    {
                        var right = Matrix.GetTile(r, c + 1);
                        if (right != null && right.CanSlide && right.Value == current.Value) return true;
                        if (right != null && right.Type == TileType.Ice) return true; // Can damage ice
                    }

                    // Check Up neighbor
                    if (r + 1 < Matrix.Rows)
                    {
                        var up = Matrix.GetTile(r + 1, c);
                        if (up != null && up.CanSlide && up.Value == current.Value) return true;
                        if (up != null && up.Type == TileType.Ice) return true; // Can damage ice
                    }
                }
            }

            return false;
        }

        private void EvaluateGameState()
        {
            if (Mode == GameMode.PuzzleChallenge)
            {
                // Win Condition: All ice obstacles shattered AND target score/tile reached
                bool allIceCleared = CountRemainingIceObstacles() == 0;
                bool targetReached = Score >= TargetScore || HasTileWithValue(TargetTileValue);

                if (allIceCleared && targetReached)
                {
                    State = GameState.Won;
                    OnStateChanged?.Invoke(State);
                    return;
                }

                // Loss Condition: Steps ran out
                if (StepsRemaining <= 0)
                {
                    State = GameState.GameOver;
                    OnStateChanged?.Invoke(State);
                    return;
                }
            }

            // Loss Condition: Grid is locked with no valid moves
            if (!HasAnyValidMove())
            {
                State = GameState.GameOver;
                OnStateChanged?.Invoke(State);
            }
        }

        public bool HasTileWithValue(int value)
        {
            var tiles = Matrix.GetAllOccupied(_occupiedBuffer);
            foreach (var item in tiles)
            {
                if (item.Tile.Value >= value) return true;
            }
            return false;
        }

        public GameMetrics GetMetrics()
        {
            return new GameMetrics
            {
                Score = Score,
                BestScore = BestScore,
                MovesMade = MovesMade,
                StepsRemaining = StepsRemaining,
                InitialStepBudget = InitialStepBudget,
                TargetScore = TargetScore,
                IceObstaclesRemaining = CountRemainingIceObstacles(),
                ComboCount = ComboCount,
                ComboMultiplier = ComboMultiplier,
                CanUndo = History.CanUndo,
                UndoCount = History.HistoryCount,
                State = State,
                Mode = Mode
            };
        }

        private void NotifyMetricsChanged()
        {
            OnMetricsChanged?.Invoke(GetMetrics());
        }
    }
}
