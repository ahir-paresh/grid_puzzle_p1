using System.Collections;
using System.Linq;
using GridPuzzle.Audio;
using GridPuzzle.Config;
using GridPuzzle.Input;
using GridPuzzle.UI;
using GridPuzzle.View;
using UnityEngine;

namespace GridPuzzle.Core
{
    /// <summary>
    /// Master coordinator orchestrating the complete mobile game loop.
    /// Mediates data flow across pure C# model (GridPuzzleGame), input pipeline (SwipeInputDetector),
    /// visual rendering (GridBoardView), presentation UI (GridPuzzleUIManager), and sound (PuzzleAudioManager).
    /// </summary>
    public class GridPuzzleGameController : MonoBehaviour
    {
        [Header("Architecture References")]
        [SerializeField] private GridBoardView boardView;
        [SerializeField] private GridPuzzleUIManager uiManager;
        [SerializeField] private SwipeInputDetector swipeInput;
        [SerializeField] private PuzzleAudioManager audioManager;
        [SerializeField] private GridPuzzleConfig config;

        [Header("Tactical Power-Ups Available")]
        [SerializeField] private int bombCharges = 2;
        [SerializeField] private int shuffleCharges = 2;

        public GridPuzzleGame GameLogic { get; private set; }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            int rows = config != null ? config.rows : 4;
            int cols = config != null ? config.columns : 4;
            int stepBudget = config != null ? config.initialStepBudget : 30;
            int targetScore = config != null ? config.targetScore : 2048;
            int targetTile = config != null ? config.targetTileValue : 2048;

            GameLogic = new GridPuzzleGame(
                rows: rows,
                columns: cols,
                mode: GameMode.PuzzleChallenge,
                stepBudget: stepBudget,
                targetScore: targetScore,
                targetTileValue: targetTile
            );

            // Bind Event Listeners
            if (swipeInput != null)
            {
                swipeInput.OnSwipeDetected += HandleSwipeDetected;
            }

            if (uiManager != null)
            {
                uiManager.OnUndoClicked += HandleUndoClicked;
                uiManager.OnBombClicked += HandleBombClicked;
                uiManager.OnShuffleClicked += HandleShuffleClicked;
                uiManager.OnRestartClicked += RestartGame;
            }

            GameLogic.OnMetricsChanged += HandleMetricsChanged;
            GameLogic.OnComboSurge += HandleComboSurge;
            GameLogic.OnStateChanged += HandleStateChanged;

            if (boardView != null)
            {
                boardView.Initialize(config, rows, cols);
            }

            RestartGame();
        }

        private void OnDestroy()
        {
            if (swipeInput != null)
            {
                swipeInput.OnSwipeDetected -= HandleSwipeDetected;
            }

            if (uiManager != null)
            {
                uiManager.OnUndoClicked -= HandleUndoClicked;
                uiManager.OnBombClicked -= HandleBombClicked;
                uiManager.OnShuffleClicked -= HandleShuffleClicked;
                uiManager.OnRestartClicked -= RestartGame;
            }

            if (GameLogic != null)
            {
                GameLogic.OnMetricsChanged -= HandleMetricsChanged;
                GameLogic.OnComboSurge -= HandleComboSurge;
                GameLogic.OnStateChanged -= HandleStateChanged;
            }
        }

        public void RestartGame()
        {
            if (swipeInput != null) swipeInput.IsInputBlocked = false;

            int startingTiles = config != null ? config.startingTileCount : 2;
            int startingIce = config != null ? config.startingIceCount : 3;
            int startingStone = config != null ? config.startingStoneCount : 1;

            GameLogic.Initialize(startingTiles, startingIce, startingStone);

            if (boardView != null)
            {
                boardView.RenderInitialTiles(GameLogic.Matrix.GetAllOccupied());
            }

            if (uiManager != null)
            {
                uiManager.UpdateMetrics(GameLogic.GetMetrics());
                uiManager.HideAllModals();
            }
        }

        private void HandleSwipeDetected(Direction direction)
        {
            if (GameLogic == null || GameLogic.State != GameState.Playing)
                return;

            // Block input while animating move sequence
            if (swipeInput != null) swipeInput.IsInputBlocked = true;

            if (audioManager != null) audioManager.PlaySwipe();

            bool moved = GameLogic.TryExecuteMove(direction, out var moveResult);

            if (!moved)
            {
                // Unblock immediately on invalid move
                if (swipeInput != null) swipeInput.IsInputBlocked = false;
                return;
            }

            // Audio feedback
            if (audioManager != null)
            {
                if (moveResult.MergesCount > 0)
                {
                    int maxVal = moveResult.Motions.Where(m => m.IsMerge).Select(m => m.PromotedValue).DefaultIfEmpty(4).Max();
                    audioManager.PlayMerge(maxVal);
                }

                if (moveResult.ObstacleDamages.Count > 0)
                {
                    bool shattered = moveResult.ObstacleDamages.Any(o => o.Shattered);
                    audioManager.PlayIceDamage(shattered);
                }

                if (moveResult.ComboCount > 1)
                {
                    audioManager.PlayCombo(moveResult.ComboCount);
                }
            }

            // Visual animation sequencing
            if (boardView != null)
            {
                boardView.AnimateMoveSequence(moveResult, () =>
                {
                    if (GameLogic != null)
                    {
                        boardView.SyncPositionsWithMatrix(GameLogic.Matrix.GetAllOccupied());
                    }

                    if (swipeInput != null && GameLogic.State == GameState.Playing)
                    {
                        swipeInput.IsInputBlocked = false;
                    }
                });
            }
            else
            {
                if (swipeInput != null) swipeInput.IsInputBlocked = false;
            }
        }

        private void HandleUndoClicked()
        {
            if (GameLogic == null || !GameLogic.History.CanUndo) return;

            if (swipeInput != null) swipeInput.IsInputBlocked = true;
            if (audioManager != null) audioManager.PlayButtonClick();

            bool undone = GameLogic.TryUndo(out var undoResult);

            if (undone && boardView != null)
            {
                boardView.AnimateUndoSequence(undoResult, () =>
                {
                    if (GameLogic != null)
                    {
                        boardView.SyncPositionsWithMatrix(GameLogic.Matrix.GetAllOccupied());
                    }

                    if (swipeInput != null && GameLogic.State == GameState.Playing)
                    {
                        swipeInput.IsInputBlocked = false;
                    }
                });
            }
            else
            {
                if (swipeInput != null) swipeInput.IsInputBlocked = false;
            }
        }

        private void HandleBombClicked()
        {
            if (bombCharges <= 0 || GameLogic == null || GameLogic.State != GameState.Playing) return;

            // Target first obstacle or highest tile
            var occupied = GameLogic.Matrix.GetAllOccupied();
            var target = occupied.FirstOrDefault(t => t.Tile.Type == TileType.Ice).Tile
                         ?? occupied.FirstOrDefault(t => t.Tile.Type == TileType.Stone).Tile
                         ?? occupied.OrderByDescending(t => t.Tile.Value).FirstOrDefault().Tile;

            if (target != null)
            {
                var targetPos = occupied.First(t => t.Tile.Id == target.Id).Pos;
                if (GameLogic.TryUseBomb(targetPos, out _))
                {
                    bombCharges--;
                    if (audioManager != null) audioManager.PlayPowerUp();
                    if (boardView != null)
                    {
                        boardView.RenderInitialTiles(GameLogic.Matrix.GetAllOccupied());
                    }
                }
            }
        }

        private void HandleShuffleClicked()
        {
            if (shuffleCharges <= 0 || GameLogic == null || GameLogic.State != GameState.Playing) return;

            if (GameLogic.TryUseShuffle())
            {
                shuffleCharges--;
                if (audioManager != null) audioManager.PlayPowerUp();
                if (boardView != null)
                {
                    boardView.RenderInitialTiles(GameLogic.Matrix.GetAllOccupied());
                }
            }
        }

        private void HandleMetricsChanged(GameMetrics metrics)
        {
            if (uiManager != null)
            {
                uiManager.UpdateMetrics(metrics);
            }
        }

        private void HandleComboSurge(int comboCount, float multiplier)
        {
            if (uiManager != null)
            {
                uiManager.TriggerComboSurge(comboCount, multiplier);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (swipeInput != null && state != GameState.Playing)
            {
                swipeInput.IsInputBlocked = true;
            }

            if (audioManager != null)
            {
                if (state == GameState.Won) audioManager.PlayWin();
                else if (state == GameState.GameOver) audioManager.PlayGameOver();
            }
        }
    }
}
