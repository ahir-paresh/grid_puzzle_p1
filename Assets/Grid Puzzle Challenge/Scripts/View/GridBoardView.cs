using System;
using System.Collections;
using System.Collections.Generic;
using GridPuzzle.Config;
using GridPuzzle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GridPuzzle.View
{
    /// <summary>
    /// Visual rendering framework for the N x M grid puzzle board.
    /// Manages slot background layouts, dynamic screen scaling, and translates
    /// pure logical transition events into smooth visual animations.
    /// </summary>
    public class GridBoardView : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private RectTransform boardContainer;
        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private RectTransform tilesContainer;

        [Header("Prefabs & Pooling")]
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private TileViewPool tilePool;

        private GridPuzzleConfig _config;
        private int _rows;
        private int _columns;
        private float _cellSize;
        private float _spacing;

        private readonly Dictionary<int, TileView> _activeTiles = new Dictionary<int, TileView>();
        private readonly List<GameObject> _slotInstances = new List<GameObject>();

        public void Initialize(GridPuzzleConfig config, int rows, int columns)
        {
            _config = config;
            _rows = rows;
            _columns = columns;

            if (boardContainer == null) boardContainer = GetComponent<RectTransform>();
            if (slotsContainer == null) slotsContainer = boardContainer;
            if (tilesContainer == null) tilesContainer = boardContainer;

            if (tilePool == null)
            {
                var poolGo = new GameObject("TilePool");
                poolGo.transform.SetParent(transform, false);
                tilePool = poolGo.AddComponent<TileViewPool>();
            }
            tilePool.Initialize(tilePrefab, rows * columns + 10);

            CalculateCellGeometry();
            BuildSlotBackgrounds();
        }

        private void CalculateCellGeometry()
        {
            // Adjust cell size dynamically to fit container dimensions
            float availableWidth = boardContainer.rect.width > 0 ? boardContainer.rect.width : 500f;
            float availableHeight = boardContainer.rect.height > 0 ? boardContainer.rect.height : 500f;

            _spacing = 14f;
            float maxCellW = (availableWidth - (_columns + 1) * _spacing) / _columns;
            float maxCellH = (availableHeight - (_rows + 1) * _spacing) / _rows;

            _cellSize = Mathf.Min(maxCellW, maxCellH);
        }

        public Vector2 GetLocalPosition(GridPosition pos)
        {
            float totalW = _columns * _cellSize + (_columns - 1) * _spacing;
            float totalH = _rows * _cellSize + (_rows - 1) * _spacing;

            float x = -totalW * 0.5f + pos.Col * (_cellSize + _spacing) + _cellSize * 0.5f;
            float y = -totalH * 0.5f + pos.Row * (_cellSize + _spacing) + _cellSize * 0.5f;

            return new Vector2(x, y);
        }

        public void BuildSlotBackgrounds()
        {
            foreach (var slot in _slotInstances)
            {
                if (slot != null) Destroy(slot);
            }
            _slotInstances.Clear();

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    var pos = new GridPosition(r, c);
                    var slotGo = slotPrefab != null
                        ? Instantiate(slotPrefab, slotsContainer)
                        : CreateProceduralSlot(slotsContainer);

                    slotGo.name = $"Slot_{r}_{c}";
                    var rt = slotGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(_cellSize, _cellSize);
                    rt.localPosition = GetLocalPosition(pos);

                    _slotInstances.Add(slotGo);
                }
            }
        }

        private GameObject CreateProceduralSlot(Transform parent)
        {
            var go = new GameObject("Slot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = _config != null ? _config.cellSlotColor : new Color(0.2f, 0.22f, 0.28f, 1f);
            return go;
        }

        public void RenderInitialTiles(IEnumerable<(GridPosition Pos, GridTile Tile)> tiles)
        {
            ClearAllTiles();

            foreach (var (pos, tile) in tiles)
            {
                SpawnTileVisual(pos, tile, playPop: false);
            }
        }

        public TileView SpawnTileVisual(GridPosition pos, GridTile tile, bool playPop = true)
        {
            if (tile == null) return null;

            var view = tilePool.Spawn(tilesContainer);
            var rt = view.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            Vector2 localPos = GetLocalPosition(pos);
            rt.localPosition = localPos;

            view.Initialize(tile.Id, tile.Value, tile.Type, tile.IceDurability, _config);
            view.SnapToPosition(localPos);
            _activeTiles[tile.Id] = view;

            if (playPop)
            {
                view.AnimateSpawnPop(_config != null ? _config.spawnScaleDuration : 0.18f);
            }

            return view;
        }

        public void AnimateMoveSequence(MoveResult moveResult, Action onSequenceComplete)
        {
            StartCoroutine(MoveSequenceCoroutine(moveResult, onSequenceComplete));
        }

        private IEnumerator MoveSequenceCoroutine(MoveResult moveResult, Action onSequenceComplete)
        {
            float slideDuration = _config != null ? _config.slideDuration : 0.14f;
            float mergePopDuration = _config != null ? _config.mergePopDuration : 0.16f;

            // 1. Animate sliding motions simultaneously
            var pendingMerges = new List<TileMotion>();

            foreach (var motion in moveResult.Motions)
            {
                if (_activeTiles.TryGetValue(motion.TileId, out var tileView))
                {
                    Vector2 targetPos = GetLocalPosition(motion.To);
                    tileView.AnimateSlideTo(targetPos, slideDuration);

                    if (motion.IsMerge)
                    {
                        pendingMerges.Add(motion);
                    }
                }
            }

            // Wait for sliding to finish
            yield return new WaitForSeconds(slideDuration);

            // 2. Process merges & promote target tiles
            foreach (var merge in pendingMerges)
            {
                // Despawn sliding tile that got absorbed
                if (_activeTiles.TryGetValue(merge.TileId, out var absorbedView))
                {
                    tilePool.Despawn(absorbedView);
                    _activeTiles.Remove(merge.TileId);
                }

                // Promote surviving tile visual
                if (merge.MergedIntoTileId.HasValue && _activeTiles.TryGetValue(merge.MergedIntoTileId.Value, out var survivorView))
                {
                    survivorView.UpdateVisualState(merge.PromotedValue, TileType.Normal, 0);
                    survivorView.AnimateMergeBump(mergePopDuration);
                }
            }

            // 3. Process Obstacle damages
            bool hasShatter = false;
            foreach (var obs in moveResult.ObstacleDamages)
            {
                if (_activeTiles.TryGetValue(obs.ObstacleId, out var obsView))
                {
                    if (obs.Shattered)
                    {
                        hasShatter = true;
                        Vector2 slotPos = GetLocalPosition(obs.Position);
                        obsView.AnimateShatter(0.2f, () =>
                        {
                            obsView.SnapToPosition(slotPos);
                            obsView.UpdateVisualState(obsView.CurrentValue, TileType.Normal, 0);
                            obsView.AnimateSpawnPop(0.15f);
                        });
                    }
                    else
                    {
                        obsView.UpdateVisualState(obsView.CurrentValue, TileType.Ice, obs.RemainingDurability);
                        obsView.AnimateMergeBump(0.12f);
                    }
                }
            }

            if (hasShatter)
            {
                // Synchronize animation timeline: shatter shake/dissolve (0.20s) + smooth pop reveal (0.15s)
                yield return new WaitForSeconds(0.35f);
            }
            else if (pendingMerges.Count > 0)
            {
                yield return new WaitForSeconds(mergePopDuration);
            }

            // 4. Spawn new tile if present
            if (moveResult.SpawnedTile != null && moveResult.SpawnPosition.HasValue)
            {
                SpawnTileVisual(moveResult.SpawnPosition.Value, moveResult.SpawnedTile, playPop: true);
                yield return new WaitForSeconds(_config != null ? _config.spawnScaleDuration : 0.18f);
            }

            onSequenceComplete?.Invoke();
        }

        public void AnimateUndoSequence(UndoResult undoResult, Action onSequenceComplete)
        {
            StartCoroutine(UndoSequenceCoroutine(undoResult, onSequenceComplete));
        }

        private IEnumerator UndoSequenceCoroutine(UndoResult undoResult, Action onSequenceComplete)
        {
            float duration = _config != null ? _config.slideDuration : 0.14f;

            // 1. Remove despawned tiles (the ones spawned during the move)
            foreach (var tileId in undoResult.DespawnedTileIds)
            {
                if (_activeTiles.TryGetValue(tileId, out var view))
                {
                    tilePool.Despawn(view);
                    _activeTiles.Remove(tileId);
                }
            }

            // 2. Re-create restored absorbed tiles
            foreach (var (pos, tile) in undoResult.RestoredTiles)
            {
                if (!_activeTiles.ContainsKey(tile.Id))
                {
                    SpawnTileVisual(pos, tile, playPop: false);
                }
            }

            // 3. Slide tiles back to original positions
            foreach (var motion in undoResult.ReverseMotions)
            {
                if (_activeTiles.TryGetValue(motion.TileId, out var view))
                {
                    Vector2 targetPos = GetLocalPosition(motion.To);
                    view.UpdateVisualState(motion.PromotedValue, view.CurrentType, 0);
                    view.AnimateSlideTo(targetPos, duration);
                }
            }

            yield return new WaitForSeconds(duration);
            onSequenceComplete?.Invoke();
        }

        public void SyncPositionsWithMatrix(IEnumerable<(GridPosition Pos, GridTile Tile)> matrixTiles)
        {
            if (matrixTiles == null) return;

            foreach (var (pos, tile) in matrixTiles)
            {
                if (_activeTiles.TryGetValue(tile.Id, out var view))
                {
                    Vector2 expectedPos = GetLocalPosition(pos);
                    view.SnapToPosition(expectedPos);
                    view.UpdateVisualState(tile.Value, tile.Type, tile.IceDurability);
                }
            }
        }

        public void ClearAllTiles()
        {
            foreach (var kvp in _activeTiles)
            {
                if (kvp.Value != null)
                {
                    tilePool.Despawn(kvp.Value);
                }
            }
            _activeTiles.Clear();
        }
    }
}
