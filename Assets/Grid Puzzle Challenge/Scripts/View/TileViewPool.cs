using System.Collections.Generic;
using UnityEngine;

namespace GridPuzzle.View
{
    /// <summary>
    /// Zero-allocation object pool for TileView instances to eliminate runtime garbage collection spikes.
    /// </summary>
    public class TileViewPool : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private int initialCapacity = 25;

        private readonly Queue<TileView> _pool = new Queue<TileView>();

        public void Initialize(TileView prefab, int capacity)
        {
            tilePrefab = prefab;
            initialCapacity = capacity;

            for (int i = 0; i < initialCapacity; i++)
            {
                var instance = CreateNewInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        public TileView Spawn(Transform parent)
        {
            TileView view;
            if (_pool.Count > 0)
            {
                view = _pool.Dequeue();
            }
            else
            {
                view = CreateNewInstance();
            }

            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        public void Despawn(TileView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            view.transform.SetParent(transform, false);
            _pool.Enqueue(view);
        }

        private TileView CreateNewInstance()
        {
            TileView instance;
            if (tilePrefab != null)
            {
                instance = Instantiate(tilePrefab, transform);
            }
            else
            {
                // Fallback procedural creation
                var go = new GameObject("TileView_Instance", typeof(RectTransform));
                instance = go.AddComponent<TileView>();
            }
            return instance;
        }
    }
}
