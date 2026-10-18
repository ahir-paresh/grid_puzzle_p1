using System;
using System.Collections;
using GridPuzzle.Config;
using GridPuzzle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridPuzzle.View
{
    /// <summary>
    /// Visual representation of an active puzzle tile on the board.
    /// Manages smooth sliding interpolation, scale pop-ins, merge bumps,
    /// obstacle visual styling (ice, stone), interactive targeting selection, and procedural typography.
    /// </summary>
    public class TileView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Components")]
        [SerializeField] private Image backgroundGraphic;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image obstacleOverlay;
        [SerializeField] private TextMeshProUGUI obstacleBadgeText;

        public int TileId { get; private set; }
        public int CurrentValue { get; private set; }
        public TileType CurrentType { get; private set; }
        public GridPosition CurrentPosition { get; set; }

        public event Action<TileView> OnTileClicked;

        private Coroutine _activeAnimationCoroutine;
        private GridPuzzleConfig _config;
        private Vector3 _targetLocalPosition;
        private Outline _selectionOutline;

        public void Initialize(int id, int value, TileType type, int durability, GridPuzzleConfig config)
        {
            TileId = id;
            CurrentValue = value;
            CurrentType = type;
            _config = config;
            _targetLocalPosition = transform.localPosition;

            SetSelected(false);
            UpdateVisualState(value, type, durability);
            transform.localScale = Vector3.one;
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionOutline == null)
            {
                _selectionOutline = GetComponent<Outline>();
                if (_selectionOutline == null && isSelected)
                {
                    _selectionOutline = gameObject.AddComponent<Outline>();
                }
            }

            if (_selectionOutline != null)
            {
                _selectionOutline.enabled = isSelected;
                _selectionOutline.effectColor = new Color(1f, 0.85f, 0.15f, 1f); // Vibrant gold
                _selectionOutline.effectDistance = new Vector2(4f, -4f);
            }

            transform.localScale = isSelected ? new Vector3(1.10f, 1.10f, 1f) : Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnTileClicked?.Invoke(this);
        }

        public void SnapToPosition(Vector3 position)
        {
            StopCurrentAnimation();
            _targetLocalPosition = position;
            transform.localPosition = position;
            transform.localScale = Vector3.one;
        }

        public void UpdateVisualState(int value, TileType type, int durability)
        {
            CurrentValue = value;
            CurrentType = type;

            if (backgroundGraphic == null) backgroundGraphic = GetComponent<Image>();
            if (valueText == null) valueText = GetComponentInChildren<TextMeshProUGUI>();

            if (type == TileType.Stone)
            {
                if (backgroundGraphic != null)
                    backgroundGraphic.color = _config != null ? _config.stoneColor : Color.gray;

                if (valueText != null)
                {
                    valueText.text = "■";
                    valueText.color = Color.white;
                }

                if (obstacleOverlay != null) obstacleOverlay.gameObject.SetActive(false);
                if (obstacleBadgeText != null) obstacleBadgeText.gameObject.SetActive(false);
            }
            else if (type == TileType.Ice)
            {
                if (backgroundGraphic != null)
                {
                    backgroundGraphic.color = durability > 1
                        ? (_config != null ? _config.iceColorIntact : Color.cyan)
                        : (_config != null ? _config.iceColorCracked : Color.white);
                }

                if (valueText != null)
                {
                    valueText.text = value.ToString();
                    valueText.color = Color.white;
                }

                if (obstacleBadgeText != null)
                {
                    obstacleBadgeText.gameObject.SetActive(true);
                    obstacleBadgeText.text = $"❄ {durability}";
                }
            }
            else // Normal
            {
                if (backgroundGraphic != null && _config != null)
                {
                    backgroundGraphic.color = _config.GetTileColor(value);
                }

                if (valueText != null)
                {
                    valueText.text = value.ToString();
                    if (_config != null) valueText.color = _config.GetTextColor(value);
                }

                if (obstacleOverlay != null) obstacleOverlay.gameObject.SetActive(false);
                if (obstacleBadgeText != null) obstacleBadgeText.gameObject.SetActive(false);
            }
        }

        public void AnimateSlideTo(Vector3 targetPosition, float duration, Action onComplete = null)
        {
            StopCurrentAnimation();
            _targetLocalPosition = targetPosition;
            _activeAnimationCoroutine = StartCoroutine(SlideCoroutine(targetPosition, duration, onComplete));
        }

        public void AnimateSpawnPop(float duration, Action onComplete = null)
        {
            StopCurrentAnimation();
            transform.localPosition = _targetLocalPosition;
            _activeAnimationCoroutine = StartCoroutine(SpawnPopCoroutine(duration, onComplete));
        }

        public void AnimateMergeBump(float duration, Action onComplete = null)
        {
            StopCurrentAnimation();
            transform.localPosition = _targetLocalPosition;
            _activeAnimationCoroutine = StartCoroutine(MergeBumpCoroutine(duration, onComplete));
        }

        public void AnimateShatter(float duration, Action onComplete = null)
        {
            StopCurrentAnimation();
            _activeAnimationCoroutine = StartCoroutine(ShatterCoroutine(duration, onComplete));
        }

        private IEnumerator SlideCoroutine(Vector3 targetPosition, float duration, Action onComplete)
        {
            Vector3 startPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth cubic ease-out
                float easedT = 1f - Mathf.Pow(1f - t, 3);
                transform.localPosition = Vector3.Lerp(startPos, targetPosition, easedT);
                yield return null;
            }

            transform.localPosition = targetPosition;
            _targetLocalPosition = targetPosition;
            _activeAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator SpawnPopCoroutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            transform.localScale = Vector3.zero;
            transform.localPosition = _targetLocalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot bounce curve: 0 -> 1.15 -> 1.0
                float scale = Mathf.Sin(t * Mathf.PI * 0.7f) * 1.15f;
                if (t >= 0.7f)
                {
                    scale = Mathf.Lerp(1.15f, 1.0f, (t - 0.7f) / 0.3f);
                }
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one;
            transform.localPosition = _targetLocalPosition;
            _activeAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator MergeBumpCoroutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            transform.localPosition = _targetLocalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Pulse 1.0 -> 1.25 -> 1.0
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one;
            transform.localPosition = _targetLocalPosition;
            _activeAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator ShatterCoroutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 basePos = _targetLocalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Shake jitter relative to fixed basePos (prevents cumulative drift)
                float jitter = (1f - t) * 6f;
                transform.localPosition = basePos + new Vector3(
                    UnityEngine.Random.Range(-jitter, jitter),
                    UnityEngine.Random.Range(-jitter, jitter),
                    0f);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            transform.localPosition = basePos;
            transform.localScale = Vector3.zero;
            _activeAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        private void StopCurrentAnimation()
        {
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                _activeAnimationCoroutine = null;
            }

            transform.localPosition = _targetLocalPosition;
            transform.localScale = Vector3.one;
        }
    }
}
