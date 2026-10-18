using System;
using GridPuzzle.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridPuzzle.Input
{
    /// <summary>
    /// Robust gesture detection and pointer vector tracking pipeline.
    /// Tracks pointer start, delta vector, velocity, angular thresholds, and deadzones
    /// to output discrete directional commands: Up, Down, Left, and Right.
    /// Supports mobile touch gestures, mouse drags, and keyboard fallback (Arrows / WASD).
    /// </summary>
    public class SwipeInputDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Swipe Thresholds")]
        [Tooltip("Minimum screen pixel displacement to register as a swipe.")]
        [SerializeField] private float minSwipeDistance = 45f;

        [Tooltip("Maximum duration in seconds for a flick swipe.")]
        [SerializeField] private float maxSwipeDuration = 0.8f;

        [Tooltip("Dominance ratio between primary and secondary axes to prevent ambiguous diagonal swipes.")]
        [Range(1.1f, 2.0f)]
        [SerializeField] private float axisDominanceRatio = 1.25f;

        [Header("State")]
        [SerializeField] private bool isInputBlocked = false;

        public bool IsInputBlocked
        {
            get => isInputBlocked;
            set => isInputBlocked = value;
        }

        // Pointer tracking state
        private Vector2 _pointerDownPosition;
        private float _pointerDownTime;
        private bool _isTrackingPointer = false;
        private bool _swipeDispatched = false;

        // Discrete directional command event
        public event Action<Direction> OnSwipeDetected;

        private void Update()
        {
            if (isInputBlocked) return;

            HandleKeyboardInput();
            HandleTouchFallback();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isInputBlocked) return;

            _pointerDownPosition = eventData.position;
            _pointerDownTime = Time.unscaledTime;
            _isTrackingPointer = true;
            _swipeDispatched = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isInputBlocked || !_isTrackingPointer || _swipeDispatched) return;

            Vector2 currentPos = eventData.position;
            EvaluateSwipeVector(currentPos);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isInputBlocked || !_isTrackingPointer) return;

            if (!_swipeDispatched)
            {
                EvaluateSwipeVector(eventData.position);
            }

            _isTrackingPointer = false;
            _swipeDispatched = false;
        }

        private void EvaluateSwipeVector(Vector2 currentPosition)
        {
            Vector2 delta = currentPosition - _pointerDownPosition;
            float distance = delta.magnitude;

            if (distance < minSwipeDistance)
                return;

            float duration = Time.unscaledTime - _pointerDownTime;
            if (duration > maxSwipeDuration && distance < minSwipeDistance * 1.5f)
            {
                // Swipe too sluggish / dragged indefinitely
                return;
            }

            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            // Enforce axis dominance to prevent diagonal ambiguity
            if (absX > absY * axisDominanceRatio)
            {
                // Horizontal Swipe
                Direction dir = delta.x > 0 ? Direction.Right : Direction.Left;
                DispatchSwipe(dir);
            }
            else if (absY > absX * axisDominanceRatio)
            {
                // Vertical Swipe
                Direction dir = delta.y > 0 ? Direction.Up : Direction.Down;
                DispatchSwipe(dir);
            }
        }

        private void DispatchSwipe(Direction direction)
        {
            _swipeDispatched = true;
            _isTrackingPointer = false;
            OnSwipeDetected?.Invoke(direction);
        }

        private void HandleKeyboardInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D))
            {
                DispatchSwipe(Direction.Right);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                DispatchSwipe(Direction.Left);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                DispatchSwipe(Direction.Up);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                DispatchSwipe(Direction.Down);
            }
        }

        private void HandleTouchFallback()
        {
            // Direct touch fallback if EventSystem is not hitting a raycast target
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    _pointerDownPosition = touch.position;
                    _pointerDownTime = Time.unscaledTime;
                    _isTrackingPointer = true;
                    _swipeDispatched = false;
                }
                else if (touch.phase == TouchPhase.Moved && _isTrackingPointer && !_swipeDispatched)
                {
                    EvaluateSwipeVector(touch.position);
                }
                else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && _isTrackingPointer)
                {
                    if (!_swipeDispatched)
                    {
                        EvaluateSwipeVector(touch.position);
                    }
                    _isTrackingPointer = false;
                }
            }
        }

        /// <summary>
        /// External method to trigger directional commands (e.g., from on-screen accessibility UI buttons).
        /// </summary>
        public void SendDirectionalCommand(Direction direction)
        {
            if (isInputBlocked) return;
            DispatchSwipe(direction);
        }
    }
}
