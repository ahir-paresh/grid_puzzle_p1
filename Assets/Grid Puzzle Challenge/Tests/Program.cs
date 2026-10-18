#if !UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using GridPuzzle.Core;

namespace GridPuzzle.Tests
{
    public static class Assert
    {
        public static void True(bool condition, string message = "")
        {
            if (!condition) throw new Exception($"Assertion Failed: Expected True but got False. {message}");
        }

        public static void False(bool condition, string message = "")
        {
            if (condition) throw new Exception($"Assertion Failed: Expected False but got True. {message}");
        }

        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Assertion Failed: Expected <{expected}> but got <{actual}>. {message}");
            }
        }

        public static void AreNotEqual<T>(T expected, T actual, string message = "")
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Assertion Failed: Did not expect <{expected}>. {message}");
            }
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("   GRID PUZZLE CHALLENGE - AUTOMATED TEST SUITE   ");
            Console.WriteLine("=================================================");

            var tests = new List<(string Name, Action TestMethod)>
            {
                ("GridMatrix: N x M Dimensions, Bounds & Spatial Indexing", Test_GridMatrix_DimensionsAndBounds),
                ("Movement: Discrete Swipe Traversal (Up, Down, Left, Right)", Test_TryExecuteMove_SlidesTiles),
                ("Merging: Matching Tile Arithmetic & Score Calculation", Test_TryExecuteMove_MergesMatchingTiles),
                ("Merging: Prevents Double Merges in Single Swipe", Test_TryExecuteMove_PreventsDoubleMerge),
                ("History: Deterministic Single Move Undo Restoration", Test_DeterministicUndo_SingleMove),
                ("History: Sequential Multi-Move Undo Reversal", Test_DeterministicUndo_MultipleMoves),
                ("Memory Safety: Bounded History Buffer & Record Pooling", Test_DeltaStateHistory_ZeroLeak_BoundedRingBuffer),
                ("Gameplay Hook: Ice Obstacle Durability Damage & Shatter", Test_IceObstacle_DamageAndShatter),
                ("Gameplay Hook: Stone Obstacle Impassable Barrier", Test_StoneObstacle_BlocksMovement),
                ("Gameplay Hook: Synergistic Combo Multiplier Progression", Test_SynergisticCombos_Multipliers),
                ("Step Budget: Rigid Step Framework & Game Over State", Test_StepBudget_GameOver),
                ("Power-Ups: Bomb Destroys Tile & Obstacle", Test_PowerUp_Bomb),
                ("Power-Ups: Grid Shuffle Permutation", Test_PowerUp_Shuffle),
                ("Power-Ups: Targeted Bomb Destroys Tile & Reversible By Undo", Test_PowerUp_TargetedBomb_DestroysTileAndCanUndo),
                ("Power-Ups: Targeted Swap Exchanges Two Tiles & Reversible By Undo", Test_PowerUp_TargetedSwap_SwapsTilesAndCanUndo),
                ("Power-Ups: Targeted Swap Moves Tile To Empty Slot & Reversible By Undo", Test_PowerUp_TargetedSwap_MovesToEmptySlotAndCanUndo)
            };

            int passed = 0;
            int failed = 0;

            foreach (var (name, testMethod) in tests)
            {
                try
                {
                    testMethod();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[PASS] ");
                    Console.ResetColor();
                    Console.WriteLine(name);
                    passed++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[FAIL] ");
                    Console.ResetColor();
                    Console.WriteLine($"{name}: {ex.Message}");
                    failed++;
                }
            }

            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine($"Total Tests: {tests.Count} | Passed: {passed} | Failed: {failed}");
            Console.WriteLine("=================================================");

            return failed == 0 ? 0 : 1;
        }

        private static void Test_GridMatrix_DimensionsAndBounds()
        {
            var matrix = new GridMatrix(5, 3);
            Assert.AreEqual(5, matrix.Rows);
            Assert.AreEqual(3, matrix.Columns);
            Assert.AreEqual(15, matrix.TotalCells);

            Assert.True(matrix.InBounds(0, 0));
            Assert.True(matrix.InBounds(4, 2));
            Assert.False(matrix.InBounds(-1, 0));
            Assert.False(matrix.InBounds(5, 0));
            Assert.False(matrix.InBounds(0, 3));

            var empty = matrix.GetEmptyPositions();
            Assert.AreEqual(15, empty.Count);

            var tile = GridTile.CreateNormal(1, 2);
            matrix.SetTile(new GridPosition(2, 1), tile);

            Assert.False(matrix.IsEmpty(new GridPosition(2, 1)));
            Assert.AreEqual(14, matrix.GetEmptyPositions().Count);
            Assert.AreEqual(tile, matrix.GetTile(2, 1));
        }

        private static void Test_TryExecuteMove_SlidesTiles()
        {
            // 4x4 grid, seed for determinism
            var game = new GridPuzzleGame(4, 4, seed: 42);
            game.Initialize(startingTileCount: 0);

            // Place a tile at (0, 0)
            var tile = GridTile.CreateNormal(100, 2);
            game.Matrix.SetTile(new GridPosition(0, 0), tile);

            // Swipe Right: tile should slide from (0,0) to (0, 3)
            bool moved = game.TryExecuteMove(Direction.Right, out var result);
            Assert.True(moved, "Tile should slide right");
            Assert.AreEqual(1, result.Motions.Count);
            Assert.AreEqual(new GridPosition(0, 0), result.Motions[0].From);
            Assert.AreEqual(new GridPosition(0, 3), result.Motions[0].To);
            Assert.AreEqual(tile, game.Matrix.GetTile(0, 3));

            // Clear any spawned tile from the first move so the column is completely unobstructed
            if (result.SpawnPosition.HasValue)
            {
                game.Matrix.SetTile(result.SpawnPosition.Value, null);
            }

            // Swipe Up: tile should slide from (0, 3) to (3, 3)
            moved = game.TryExecuteMove(Direction.Up, out result);
            Assert.True(moved, "Tile should slide up");
            Assert.AreEqual(tile, game.Matrix.GetTile(3, 3));
        }

        private static void Test_TryExecuteMove_MergesMatchingTiles()
        {
            var game = new GridPuzzleGame(4, 4, seed: 100);
            game.Initialize(startingTileCount: 0);

            // Place two matching tiles in row 0 at col 0 and col 1
            var t1 = GridTile.CreateNormal(1, 2);
            var t2 = GridTile.CreateNormal(2, 2);
            game.Matrix.SetTile(new GridPosition(0, 0), t1);
            game.Matrix.SetTile(new GridPosition(0, 1), t2);

            // Move Right: t1 should slide into t2 at (0, 3), merging into value 4
            bool moved = game.TryExecuteMove(Direction.Right, out var result);
            Assert.True(moved);
            Assert.AreEqual(1, result.MergesCount);
            Assert.AreEqual(4, result.BaseScoreGained);

            var mergedTile = game.Matrix.GetTile(0, 3);
            Assert.True(mergedTile != null);
            Assert.AreEqual(4, mergedTile.Value);
            Assert.AreEqual(4, game.Score);
        }

        private static void Test_TryExecuteMove_PreventsDoubleMerge()
        {
            var game = new GridPuzzleGame(4, 4, seed: 200);
            game.Initialize(startingTileCount: 0);

            // Row 0 has: [2, 2, 2, 2]
            for (int c = 0; c < 4; c++)
            {
                game.Matrix.SetTile(new GridPosition(0, c), GridTile.CreateNormal(c + 1, 2));
            }

            // Swipe Right: should result in [., ., 4, 4], NOT [., ., ., 8]
            bool moved = game.TryExecuteMove(Direction.Right, out var result);
            Assert.True(moved);
            Assert.AreEqual(2, result.MergesCount);

            var tileCol2 = game.Matrix.GetTile(0, 2);
            var tileCol3 = game.Matrix.GetTile(0, 3);
            Assert.True(tileCol2 != null && tileCol2.Value == 4, "Col 2 should have merged tile value 4");
            Assert.True(tileCol3 != null && tileCol3.Value == 4, "Col 3 should have merged tile value 4");
        }

        private static void Test_DeterministicUndo_SingleMove()
        {
            var game = new GridPuzzleGame(4, 4, seed: 300);
            game.Initialize(startingTileCount: 0);

            var t1 = GridTile.CreateNormal(1, 2);
            var t2 = GridTile.CreateNormal(2, 2);
            game.Matrix.SetTile(new GridPosition(0, 0), t1);
            game.Matrix.SetTile(new GridPosition(0, 1), t2);

            int initialScore = game.Score;
            int initialSteps = game.StepsRemaining;

            // Execute move Right
            bool moved = game.TryExecuteMove(Direction.Right, out var moveRes);
            Assert.True(moved);
            Assert.AreNotEqual(initialScore, game.Score);
            Assert.AreEqual(initialSteps - 1, game.StepsRemaining);
            Assert.True(game.History.CanUndo);

            // Execute Undo
            bool undone = game.TryUndo(out var undoRes);
            Assert.True(undone, "Undo should succeed");
            Assert.AreEqual(initialScore, game.Score, "Score should revert to initial");
            Assert.AreEqual(initialSteps, game.StepsRemaining, "Steps should revert to initial");

            // Verify tiles at original positions
            var restoredT1 = game.Matrix.GetTile(0, 0);
            var restoredT2 = game.Matrix.GetTile(0, 1);
            Assert.True(restoredT1 != null, "Tile 1 must be restored at (0,0)");
            Assert.True(restoredT2 != null, "Tile 2 must be restored at (0,1)");
            Assert.AreEqual(2, restoredT1.Value);
            Assert.AreEqual(2, restoredT2.Value);
        }

        private static void Test_DeterministicUndo_MultipleMoves()
        {
            var game = new GridPuzzleGame(4, 4, seed: 400);
            game.Initialize(startingTileCount: 2);

            int movesToMake = 4;
            int movesActuallyMade = 0;

            for (int i = 0; i < movesToMake; i++)
            {
                var dir = (Direction)(i % 4);
                if (game.TryExecuteMove(dir, out _))
                {
                    movesActuallyMade++;
                }
            }

            Assert.AreEqual(movesActuallyMade, game.MovesMade);
            Assert.AreEqual(movesActuallyMade, game.History.HistoryCount);

            // Undo all moves back
            for (int i = 0; i < movesActuallyMade; i++)
            {
                bool undone = game.TryUndo(out _);
                Assert.True(undone, $"Undo step {i + 1} should succeed");
            }

            Assert.AreEqual(0, game.MovesMade);
            Assert.AreEqual(0, game.History.HistoryCount);
            Assert.False(game.History.CanUndo);
        }

        private static void Test_DeltaStateHistory_ZeroLeak_BoundedRingBuffer()
        {
            var history = new DeltaStateHistory(maxCapacity: 5);
            Assert.AreEqual(0, history.HistoryCount);
            Assert.AreEqual(5, history.MaxCapacity);

            // Push 10 records: should gracefully bound at 5 without memory leak
            for (int i = 1; i <= 10; i++)
            {
                var rec = history.ObtainRecord();
                rec.MoveIndex = i;
                history.Record(rec);
                Assert.True(history.HistoryCount <= 5, "Count must never exceed MaxCapacity");
            }

            Assert.AreEqual(5, history.HistoryCount);
            Assert.AreEqual(10, history.Peek().MoveIndex);

            // Pop all 5
            for (int i = 10; i >= 6; i--)
            {
                bool popped = history.TryPop(out var rec);
                Assert.True(popped);
                Assert.AreEqual(i, rec.MoveIndex);
                history.ReleaseRecord(rec);
            }

            Assert.AreEqual(0, history.HistoryCount);
            Assert.False(history.CanUndo);
        }

        private static void Test_IceObstacle_DamageAndShatter()
        {
            var game = new GridPuzzleGame(4, 4, seed: 500);
            game.Initialize(startingTileCount: 0);

            // Ice tile at (0, 3) with durability 2
            var ice = GridTile.CreateIce(50, 4, durability: 2);
            game.Matrix.SetTile(new GridPosition(0, 3), ice);

            // Normal tile at (0, 0)
            var normal = GridTile.CreateNormal(1, 4);
            game.Matrix.SetTile(new GridPosition(0, 0), normal);

            // Hit 1: Swipe Right -> slides to (0, 2) and hits ice at (0, 3)
            bool moved = game.TryExecuteMove(Direction.Right, out var result);
            Assert.True(moved);
            Assert.AreEqual(1, result.ObstacleDamages.Count);
            Assert.AreEqual(1, ice.IceDurability, "Ice durability should decrease from 2 to 1");
            Assert.False(result.ObstacleDamages[0].Shattered);

            // Move normal tile back to (0, 0) manually to test hit 2
            game.Matrix.SetTile(new GridPosition(0, 2), null);
            game.Matrix.SetTile(new GridPosition(0, 0), normal);

            // Hit 2: Swipe Right -> hits ice again
            moved = game.TryExecuteMove(Direction.Right, out result);
            Assert.True(moved);
            Assert.AreEqual(1, result.ObstacleDamages.Count);
            Assert.True(result.ObstacleDamages[0].Shattered, "Ice should shatter on durability reaching 0");
            Assert.AreEqual(TileType.Normal, ice.Type, "Shattered ice converts to normal tile");
        }

        private static void Test_StoneObstacle_BlocksMovement()
        {
            var game = new GridPuzzleGame(4, 4, seed: 600);
            game.Initialize(startingTileCount: 0);

            // Stone obstacle at (0, 2)
            var stone = GridTile.CreateStone(999);
            game.Matrix.SetTile(new GridPosition(0, 2), stone);

            // Normal tile at (0, 0)
            var normal = GridTile.CreateNormal(1, 2);
            game.Matrix.SetTile(new GridPosition(0, 0), normal);

            // Swipe Right: tile should stop at (0, 1), blocked by stone at (0, 2)
            bool moved = game.TryExecuteMove(Direction.Right, out var result);
            Assert.True(moved);
            Assert.AreEqual(new GridPosition(0, 1), result.Motions[0].To);
            Assert.AreEqual(stone, game.Matrix.GetTile(0, 2), "Stone obstacle must remain in place");
            Assert.AreEqual(normal, game.Matrix.GetTile(0, 1));
        }

        private static void Test_SynergisticCombos_Multipliers()
        {
            var game = new GridPuzzleGame(4, 4, seed: 700);
            game.Initialize(startingTileCount: 0);

            // Move 1 with merge:
            game.Matrix.SetTile(new GridPosition(0, 0), GridTile.CreateNormal(1, 2));
            game.Matrix.SetTile(new GridPosition(0, 1), GridTile.CreateNormal(2, 2));
            game.TryExecuteMove(Direction.Right, out _);
            Assert.AreEqual(1, game.ComboCount);
            Assert.AreEqual(1.0f, game.ComboMultiplier);

            // Move 2 with merge:
            game.Matrix.SetTile(new GridPosition(1, 0), GridTile.CreateNormal(3, 4));
            game.Matrix.SetTile(new GridPosition(1, 1), GridTile.CreateNormal(4, 4));
            game.TryExecuteMove(Direction.Right, out _);
            Assert.AreEqual(2, game.ComboCount);
            Assert.AreEqual(1.5f, game.ComboMultiplier, "Combo multiplier should increase to 1.5x");

            // Move 3 with merge:
            game.Matrix.SetTile(new GridPosition(2, 0), GridTile.CreateNormal(5, 8));
            game.Matrix.SetTile(new GridPosition(2, 1), GridTile.CreateNormal(6, 8));
            game.TryExecuteMove(Direction.Right, out _);
            Assert.AreEqual(3, game.ComboCount);
            Assert.AreEqual(2.0f, game.ComboMultiplier, "Combo multiplier should increase to 2.0x");
        }

        private static void Test_StepBudget_GameOver()
        {
            // Initial step budget = 2
            var game = new GridPuzzleGame(4, 4, stepBudget: 2, seed: 800);
            game.Initialize(startingTileCount: 2);

            Assert.AreEqual(2, game.StepsRemaining);
            Assert.AreEqual(GameState.Playing, game.State);

            game.TryExecuteMove(Direction.Down, out _);
            Assert.AreEqual(1, game.StepsRemaining);
            Assert.AreEqual(GameState.Playing, game.State);

            game.TryExecuteMove(Direction.Up, out _);
            Assert.AreEqual(0, game.StepsRemaining);
            Assert.AreEqual(GameState.GameOver, game.State, "Game should enter GameOver state when step budget hits 0");
        }

        private static void Test_PowerUp_Bomb()
        {
            var game = new GridPuzzleGame(4, 4, seed: 900);
            game.Initialize(startingTileCount: 0);

            var stone = GridTile.CreateStone(10);
            game.Matrix.SetTile(new GridPosition(1, 1), stone);

            bool blasted = game.TryUseBomb(new GridPosition(1, 1), out var destroyedTile);
            Assert.True(blasted);
            Assert.AreEqual(stone, destroyedTile);
            Assert.True(game.Matrix.IsEmpty(new GridPosition(1, 1)), "Cell should be empty after bomb");
        }

        private static void Test_PowerUp_Shuffle()
        {
            var game = new GridPuzzleGame(4, 4, seed: 1000);
            game.Initialize(startingTileCount: 0);

            game.Matrix.SetTile(new GridPosition(0, 0), GridTile.CreateNormal(1, 2));
            game.Matrix.SetTile(new GridPosition(3, 3), GridTile.CreateNormal(2, 8));

            bool shuffled = game.TryUseShuffle();
            Assert.True(shuffled);

            // Verify both values 2 and 8 are still on the board
            bool found2 = false;
            bool found8 = false;
            foreach (var item in game.Matrix.GetAllOccupied())
            {
                if (item.Tile.Value == 2) found2 = true;
                if (item.Tile.Value == 8) found8 = true;
            }
            Assert.True(found2 && found8, "Values must be preserved during shuffle");
        }

        private static void Test_PowerUp_TargetedBomb_DestroysTileAndCanUndo()
        {
            var game = new GridPuzzleGame(4, 4, seed: 101);
            game.Initialize(startingTileCount: 0);

            var tile = GridTile.CreateNormal(42, 64);
            game.Matrix.SetTile(new GridPosition(2, 3), tile);
            int scoreBefore = game.Score;

            // Bomb the specific tile at (2, 3)
            bool bombed = game.TryUseBomb(new GridPosition(2, 3), out var destroyedTile);
            Assert.True(bombed, "Bomb should succeed on occupied tile");
            Assert.AreEqual(tile, destroyedTile);
            Assert.True(game.Matrix.IsEmpty(new GridPosition(2, 3)), "Targeted cell must be empty after bomb");
            Assert.AreEqual(scoreBefore + 25, game.Score, "Bomb should award power-up score");

            // Undo the bomb
            Assert.True(game.History.CanUndo, "Should be able to undo bomb");
            bool undone = game.TryUndo(out var undoResult);
            Assert.True(undone, "Undo operation must succeed");
            Assert.AreEqual(scoreBefore, game.Score, "Score must be restored after undo");
            var restored = game.Matrix.GetTile(new GridPosition(2, 3));
            Assert.AreEqual(42, restored.Id);
            Assert.AreEqual(64, restored.Value);
        }

        private static void Test_PowerUp_TargetedSwap_SwapsTilesAndCanUndo()
        {
            var game = new GridPuzzleGame(4, 4, seed: 202);
            game.Initialize(startingTileCount: 0);

            var tileA = GridTile.CreateNormal(10, 4);
            var tileB = GridTile.CreateNormal(20, 16);
            game.Matrix.SetTile(new GridPosition(0, 0), tileA);
            game.Matrix.SetTile(new GridPosition(3, 3), tileB);

            // Swap (0, 0) and (3, 3)
            bool swapped = game.TrySwapTiles(new GridPosition(0, 0), new GridPosition(3, 3), out var result);
            Assert.True(swapped, "Swap between two valid tiles should succeed");
            Assert.AreEqual(tileA.Id, result.TileAId);
            Assert.AreEqual(tileB.Id, result.TileBId.Value);

            // Verify matrix after swap
            Assert.AreEqual(tileB, game.Matrix.GetTile(0, 0), "(0, 0) should now contain Tile B");
            Assert.AreEqual(tileA, game.Matrix.GetTile(3, 3), "(3, 3) should now contain Tile A");

            // Undo swap
            bool undone = game.TryUndo(out _);
            Assert.True(undone, "Undo swap must succeed");
            Assert.AreEqual(tileA, game.Matrix.GetTile(0, 0), "(0, 0) should be restored to Tile A");
            Assert.AreEqual(tileB, game.Matrix.GetTile(3, 3), "(3, 3) should be restored to Tile B");
        }

        private static void Test_PowerUp_TargetedSwap_MovesToEmptySlotAndCanUndo()
        {
            var game = new GridPuzzleGame(4, 4, seed: 303);
            game.Initialize(startingTileCount: 0);

            var tileA = GridTile.CreateNormal(30, 128);
            game.Matrix.SetTile(new GridPosition(1, 1), tileA);
            Assert.True(game.Matrix.IsEmpty(new GridPosition(2, 2)), "(2, 2) must be empty");

            // Swap (move) Tile A from (1, 1) into empty slot (2, 2)
            bool moved = game.TrySwapTiles(new GridPosition(1, 1), new GridPosition(2, 2), out var result);
            Assert.True(moved, "Moving tile to empty slot via swap should succeed");
            Assert.AreEqual(tileA.Id, result.TileAId);
            Assert.False(result.TileBId.HasValue, "TileB should be null for empty slot destination");

            Assert.True(game.Matrix.IsEmpty(new GridPosition(1, 1)), "Original slot should now be empty");
            Assert.AreEqual(tileA, game.Matrix.GetTile(2, 2), "Destination slot should now hold Tile A");

            // Undo move
            bool undone = game.TryUndo(out _);
            Assert.True(undone, "Undo move must succeed");
            Assert.AreEqual(tileA, game.Matrix.GetTile(1, 1), "Tile A must be restored to original slot (1, 1)");
            Assert.True(game.Matrix.IsEmpty(new GridPosition(2, 2)), "Destination slot (2, 2) must be empty again");
        }
    }
}
#endif
