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

        private enum ActivePowerUpMode
        {
            None,
            Bomb,
            Swap
        }

        private ActivePowerUpMode _currentPowerUpMode = ActivePowerUpMode.None;
        private GridPosition? _selectedSwapTilePos = null;

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
                boardView.OnCellClicked += HandleCellClicked;
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

            if (boardView != null)
            {
                boardView.OnCellClicked -= HandleCellClicked;
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
            bombCharges = 2;
            shuffleCharges = 2;
            CancelActivePowerUp();

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
                uiManager.UpdatePowerUpCounts(bombCharges, shuffleCharges);
                uiManager.HideAllModals();
            }
        }

        private void HandleSwipeDetected(Direction direction)
        {
            if (GameLogic == null || GameLogic.State != GameState.Playing)
                return;

            CancelActivePowerUp();

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

            CancelActivePowerUp();

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

                    if (uiManager != null)
                    {
                        uiManager.UpdatePowerUpCounts(bombCharges, shuffleCharges);
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

            if (_currentPowerUpMode == ActivePowerUpMode.Bomb)
            {
                CancelActivePowerUp();
                return;
            }

            CancelActivePowerUp();
            _currentPowerUpMode = ActivePowerUpMode.Bomb;

            if (uiManager != null) uiManager.SetBombSelected(true);
            if (swipeInput != null) swipeInput.IsInputBlocked = true;
            if (audioManager != null) audioManager.PlayButtonClick();
        }

        private void HandleShuffleClicked()
        {
            if (shuffleCharges <= 0 || GameLogic == null || GameLogic.State != GameState.Playing) return;

            if (_currentPowerUpMode == ActivePowerUpMode.Swap)
            {
                CancelActivePowerUp();
                return;
            }

            CancelActivePowerUp();
            _currentPowerUpMode = ActivePowerUpMode.Swap;

            if (uiManager != null) uiManager.SetSwapSelected(true);
            if (swipeInput != null) swipeInput.IsInputBlocked = true;
            if (audioManager != null) audioManager.PlayButtonClick();
        }

        private void HandleCellClicked(GridPosition clickedPos)
        {
            if (GameLogic == null || GameLogic.State != GameState.Playing) return;

            if (_currentPowerUpMode == ActivePowerUpMode.Bomb)
            {
                var targetTile = GameLogic.Matrix.GetTile(clickedPos);
                if (targetTile == null) return; // Ignore clicks on empty slots; must click an active tile/obstacle

                if (GameLogic.TryUseBomb(clickedPos, out var blastedTile))
                {
                    bombCharges--;
                    if (audioManager != null) audioManager.PlayPowerUp();
                    if (uiManager != null) uiManager.UpdatePowerUpCounts(bombCharges, shuffleCharges);

                    CancelActivePowerUp();

                    if (boardView != null)
                    {
                        if (swipeInput != null) swipeInput.IsInputBlocked = true;
                        boardView.AnimateTileDestruction(blastedTile, () =>
                        {
                            if (GameLogic != null && boardView != null)
                            {
                                boardView.SyncPositionsWithMatrix(GameLogic.Matrix.GetAllOccupied());
                            }
                            if (swipeInput != null && GameLogic.State == GameState.Playing)
                            {
                                swipeInput.IsInputBlocked = false;
                            }
                        });
                    }
                }
            }
            else if (_currentPowerUpMode == ActivePowerUpMode.Swap)
            {
                if (!_selectedSwapTilePos.HasValue)
                {
                    // Phase 1: Select first tile to swap
                    var tile = GameLogic.Matrix.GetTile(clickedPos);
                    if (tile == null || tile.Type == TileType.Stone) return; // Stone obstacles cannot be moved

                    _selectedSwapTilePos = clickedPos;
                    if (boardView != null) boardView.SetTileSelected(clickedPos, true);
                    if (audioManager != null) audioManager.PlayButtonClick();
                }
                else
                {
                    // Phase 2: Select destination (another tile or empty slot)
                    GridPosition firstPos = _selectedSwapTilePos.Value;

                    if (clickedPos == firstPos)
                    {
                        // Clicking the same tile deselects it
                        if (boardView != null) boardView.SetTileSelected(firstPos, false);
                        _selectedSwapTilePos = null;
                        if (audioManager != null) audioManager.PlayButtonClick();
                        return;
                    }

                    if (GameLogic.TrySwapTiles(firstPos, clickedPos, out var swapResult))
                    {
                        shuffleCharges--;
                        if (boardView != null) boardView.SetTileSelected(firstPos, false);
                        _selectedSwapTilePos = null;

                        if (audioManager != null) audioManager.PlayPowerUp();
                        if (uiManager != null) uiManager.UpdatePowerUpCounts(bombCharges, shuffleCharges);

                        CancelActivePowerUp();

                        if (boardView != null)
                        {
                            if (swipeInput != null) swipeInput.IsInputBlocked = true;
                            boardView.AnimateSwap(firstPos, clickedPos, swapResult, () =>
                            {
                                if (GameLogic != null && boardView != null)
                                {
                                    boardView.SyncPositionsWithMatrix(GameLogic.Matrix.GetAllOccupied());
                                }
                                if (swipeInput != null && GameLogic.State == GameState.Playing)
                                {
                                    swipeInput.IsInputBlocked = false;
                                }
                            });
                        }
                    }
                }
            }
        }

        private void CancelActivePowerUp()
        {
            if (_selectedSwapTilePos.HasValue && boardView != null)
            {
                boardView.SetTileSelected(_selectedSwapTilePos.Value, false);
            }
            _selectedSwapTilePos = null;
            _currentPowerUpMode = ActivePowerUpMode.None;

            if (uiManager != null)
            {
                uiManager.SetBombSelected(false);
                uiManager.SetSwapSelected(false);
            }

            if (swipeInput != null && GameLogic != null && GameLogic.State == GameState.Playing)
            {
                swipeInput.IsInputBlocked = false;
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
