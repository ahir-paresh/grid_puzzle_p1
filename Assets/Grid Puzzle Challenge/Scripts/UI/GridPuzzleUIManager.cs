using System;
using System.Collections;
using GridPuzzle.Core;
using Solo.MOST_IN_ONE;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GridPuzzle.UI
{
    /// <summary>
    /// Presentation layer coordinator for Grid Puzzle Challenge.
    /// Exposes prominent system metrics (score, step budget, combo multiplier, objective progress),
    /// explicit player feedback, responsive modals for Win / Game Over states,
    /// and control bindings for Undo and Tactical Power-Ups.
    /// </summary>
    public class GridPuzzleUIManager : MonoBehaviour
    {
        [Header("Top Metric HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI stepsRemainingText;
        [SerializeField] private Image stepsRemainingBadge;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI comboBannerText;

        [Header("Action Controls")]
        [SerializeField] private Button undoButton;
        [SerializeField] private TextMeshProUGUI undoBadgeText;
        [SerializeField] private Button bombButton;
        [SerializeField] private TextMeshProUGUI bombCountText;
        [SerializeField] private Button shuffleButton;
        [SerializeField] private TextMeshProUGUI shuffleCountText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button infoButton;

        [Header("Modals - Game Over")]
        [SerializeField] private GameObject gameOverModal;
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverBestText;
        [SerializeField] private Button gameOverRetryButton;
        [SerializeField] private Button gameOverUndoButton;

        [Header("Modals - Victory")]
        [SerializeField] private GameObject winModal;
        [SerializeField] private TextMeshProUGUI winScoreText;
        [SerializeField] private TextMeshProUGUI winMovesBonusText;
        [SerializeField] private TextMeshProUGUI winStarsText;
        [SerializeField] private Button winNextButton;

        [Header("Modals - Instructions")]
        [SerializeField] private GameObject infoModal;
        [SerializeField] private Button infoCloseButton;

        // Events dispatched to Game Controller
        public event Action OnUndoClicked;
        public event Action OnBombClicked;
        public event Action OnShuffleClicked;
        public event Action OnRestartClicked;

        private Coroutine _comboBannerCoroutine;
        private Coroutine _stepPulseCoroutine;
        private int _displayedScore = 0;
        private Outline _bombOutline;
        private Outline _shuffleOutline;
        private Color _bombOriginalColor = new Color(0.22f, 0.26f, 0.35f, 1f);
        private Color _shuffleOriginalColor = new Color(0.22f, 0.26f, 0.35f, 1f);

        private void Awake()
        {
            if (bombButton != null && bombButton.TryGetComponent<Image>(out var bImg)) _bombOriginalColor = bImg.color;
            if (shuffleButton != null && shuffleButton.TryGetComponent<Image>(out var sImg)) _shuffleOriginalColor = sImg.color;

            if (shuffleButton != null)
            {
                var txt = shuffleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null && txt.text.Contains("SHUFFLE"))
                {
                    txt.text = "⇄ SWAP";
                }
            }

            BindButtons();
            HideAllModals();
        }

        public void SetBombSelected(bool isSelected)
        {
            if (bombButton == null) return;
            var img = bombButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(0.92f, 0.35f, 0.2f, 1f) : _bombOriginalColor;
            }

            if (_bombOutline == null)
            {
                _bombOutline = bombButton.GetComponent<Outline>();
                if (_bombOutline == null && isSelected)
                {
                    _bombOutline = bombButton.gameObject.AddComponent<Outline>();
                }
            }

            if (_bombOutline != null)
            {
                _bombOutline.enabled = isSelected;
                _bombOutline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                _bombOutline.effectDistance = new Vector2(3f, -3f);
            }

            bombButton.transform.localScale = isSelected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        }

        public void SetSwapSelected(bool isSelected)
        {
            if (shuffleButton == null) return;
            var img = shuffleButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(0.25f, 0.65f, 0.90f, 1f) : _shuffleOriginalColor;
            }

            if (_shuffleOutline == null)
            {
                _shuffleOutline = shuffleButton.GetComponent<Outline>();
                if (_shuffleOutline == null && isSelected)
                {
                    _shuffleOutline = shuffleButton.gameObject.AddComponent<Outline>();
                }
            }

            if (_shuffleOutline != null)
            {
                _shuffleOutline.enabled = isSelected;
                _shuffleOutline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                _shuffleOutline.effectDistance = new Vector2(3f, -3f);
            }

            shuffleButton.transform.localScale = isSelected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        }

        public void UpdatePowerUpCounts(int bombs, int swaps)
        {
            if (bombCountText != null) bombCountText.text = bombs.ToString();
            if (shuffleCountText != null) shuffleCountText.text = swaps.ToString();

            if (bombButton != null) bombButton.interactable = bombs > 0;
            if (shuffleButton != null) shuffleButton.interactable = swaps > 0;
        }

        private void BindButtons()
        {
            if (undoButton != null) undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            if (bombButton != null) bombButton.onClick.AddListener(() => OnBombClicked?.Invoke());
            if (shuffleButton != null) shuffleButton.onClick.AddListener(() => OnShuffleClicked?.Invoke());
            if (restartButton != null) restartButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                OnRestartClicked?.Invoke();
            });

            if (gameOverRetryButton != null) gameOverRetryButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                OnRestartClicked?.Invoke();
            });
            if (gameOverUndoButton != null) gameOverUndoButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                OnUndoClicked?.Invoke();
            });
            if (winNextButton != null) winNextButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                OnRestartClicked?.Invoke();
            });

            if (infoButton != null) infoButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                ShowInfoModal(true);
            });
            if (infoCloseButton != null) infoCloseButton.onClick.AddListener(() =>
            {
                TriggerButtonHaptic();
                ShowInfoModal(false);
            });
        }

        private void TriggerButtonHaptic()
        {
            MOST_HapticFeedback.GenerateWithCooldown(MOST_HapticFeedback.HapticTypes.Selection, 0.05f);
        }

        public void UpdateMetrics(GameMetrics metrics)
        {
            // Score animation
            if (scoreText != null)
            {
                scoreText.text = metrics.Score.ToString("N0");
            }

            if (bestScoreText != null)
            {
                bestScoreText.text = metrics.BestScore.ToString("N0");
            }

            // Steps budget presentation
            if (stepsRemainingText != null)
            {
                stepsRemainingText.text = metrics.StepsRemaining.ToString();

                // Dynamic color feedback for steps
                if (metrics.StepsRemaining <= 5)
                {
                    stepsRemainingText.color = new Color(1f, 0.25f, 0.2f, 1f); // Red
                    if (stepsRemainingBadge != null) stepsRemainingBadge.color = new Color(0.8f, 0.15f, 0.15f, 0.4f);
                }
                else if (metrics.StepsRemaining <= 10)
                {
                    stepsRemainingText.color = new Color(1f, 0.75f, 0.2f, 1f); // Amber
                    if (stepsRemainingBadge != null) stepsRemainingBadge.color = new Color(0.8f, 0.5f, 0.1f, 0.3f);
                }
                else
                {
                    stepsRemainingText.color = Color.white;
                    if (stepsRemainingBadge != null) stepsRemainingBadge.color = new Color(0.2f, 0.25f, 0.35f, 0.4f);
                }
            }

            // Objective feedback
            if (objectiveText != null)
            {
                string iceText = metrics.IceObstaclesRemaining > 0
                    ? $"❄ {metrics.IceObstaclesRemaining} Left"
                    : "✓ Ice Cleared!";
                objectiveText.text = $"{iceText}  |  Goal: {metrics.TargetScore}";
            }

            // Undo button interactable state
            if (undoButton != null)
            {
                undoButton.interactable = metrics.CanUndo && metrics.State != GameState.Won;
            }

            if (undoBadgeText != null)
            {
                undoBadgeText.text = metrics.UndoCount.ToString();
            }

            // Game over / Win state modals
            if (metrics.State == GameState.GameOver)
            {
                ShowGameOverModal(metrics);
            }
            else if (metrics.State == GameState.Won)
            {
                ShowWinModal(metrics);
            }
            else if (metrics.State == GameState.Playing)
            {
                HideAllModals();
            }
        }

        public void TriggerComboSurge(int comboCount, float multiplier)
        {
            if (comboBannerText == null || comboCount <= 1) return;

            if (_comboBannerCoroutine != null) StopCoroutine(_comboBannerCoroutine);
            _comboBannerCoroutine = StartCoroutine(ComboSurgeCoroutine(comboCount, multiplier));
        }

        private IEnumerator ComboSurgeCoroutine(int comboCount, float multiplier)
        {
            comboBannerText.gameObject.SetActive(true);
            comboBannerText.text = $"COMBO x{multiplier:F1}! 🔥";
            comboBannerText.transform.localScale = Vector3.one * 1.35f;

            float elapsed = 0f;
            float duration = 1.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                comboBannerText.transform.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
            comboBannerText.gameObject.SetActive(false);
            _comboBannerCoroutine = null;
        }

        public void ShowGameOverModal(GameMetrics metrics)
        {
            if (gameOverModal != null)
            {
                gameOverModal.SetActive(true);
                if (gameOverScoreText != null) gameOverScoreText.text = $"Score: {metrics.Score:N0}";
                if (gameOverBestText != null) gameOverBestText.text = $"Best: {metrics.BestScore:N0}";
                if (gameOverUndoButton != null) gameOverUndoButton.interactable = metrics.CanUndo;
            }
        }

        public void ShowWinModal(GameMetrics metrics)
        {
            if (winModal != null)
            {
                winModal.SetActive(true);
                int movesBonus = metrics.StepsRemaining * 100;
                int totalFinalScore = metrics.Score + movesBonus;

                if (winScoreText != null) winScoreText.text = $"Final Score: {totalFinalScore:N0}";
                if (winMovesBonusText != null) winMovesBonusText.text = $"Steps Bonus: +{movesBonus} ({metrics.StepsRemaining} left)";

                int stars = metrics.StepsRemaining >= 15 ? 3 : (metrics.StepsRemaining >= 5 ? 2 : 1);
                if (winStarsText != null) winStarsText.text = new string('★', stars) + new string('☆', 3 - stars);
            }
        }

        public void ShowInfoModal(bool show)
        {
            if (infoModal != null) infoModal.SetActive(show);
        }

        public void HideAllModals()
        {
            if (gameOverModal != null) gameOverModal.SetActive(false);
            if (winModal != null) winModal.SetActive(false);
            if (infoModal != null) infoModal.SetActive(false);
            if (comboBannerText != null) comboBannerText.gameObject.SetActive(false);
        }
    }
}
