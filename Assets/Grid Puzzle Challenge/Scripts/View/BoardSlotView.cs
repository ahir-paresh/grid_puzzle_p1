using System;
using GridPuzzle.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridPuzzle.View
{
    /// <summary>
    /// Component attached to grid cell slot backgrounds to detect clicks on empty cells.
    /// Supports tactical power-up targeting (e.g. Swapping a tile to an empty slot).
    /// </summary>
    public class BoardSlotView : MonoBehaviour, IPointerClickHandler
    {
        public GridPosition Position { get; private set; }
        private Action<GridPosition> _onClick;

        public void Initialize(GridPosition pos, Action<GridPosition> onClick)
        {
            Position = pos;
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick?.Invoke(Position);
        }
    }
}
