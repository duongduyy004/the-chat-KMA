using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.Core
{
    [Serializable]
    public sealed class Pool<T> where T : Component
    {
        [SerializeField] T prefab;
        [SerializeField, Min(1)] int prewarmCapacity = 8;
        [SerializeField] Transform parent;

        [NonSerialized] Queue<T> available;
        [NonSerialized] HashSet<T> entries;
        [NonSerialized] HashSet<T> released;

        public int AvailableCount => available?.Count ?? 0;
        public int Capacity => entries?.Count ?? 0;

        public Pool()
        {
        }

        public Pool(T prefab, int prewarmCapacity, Transform parent = null)
        {
            this.prefab = prefab;
            this.prewarmCapacity = Mathf.Max(1, prewarmCapacity);
            this.parent = parent;
            Initialize();
        }

        public void Initialize()
        {
            EnsureCollections();
            if (prefab == null)
                return;

            while (entries.Count < Mathf.Max(1, prewarmCapacity))
            {
                T entry = UnityEngine.Object.Instantiate(prefab, parent);
                entry.gameObject.SetActive(false);
                entries.Add(entry);
                released.Add(entry);
                available.Enqueue(entry);
            }
        }

        public void Prewarm() => Initialize();

        public T Get()
        {
            EnsureCollections();
            while (available.Count > 0)
            {
                T entry = available.Dequeue();
                released.Remove(entry);
                if (entry == null)
                    continue;

                entry.gameObject.SetActive(true);
                return entry;
            }

            return null;
        }

        public void Release(T entry)
        {
            EnsureCollections();
            if (entry == null || !entries.Contains(entry) || !released.Add(entry))
                return;

            entry.gameObject.SetActive(false);
            if (parent != null)
                entry.transform.SetParent(parent, false);
            available.Enqueue(entry);
        }

        void EnsureCollections()
        {
            available ??= new Queue<T>();
            entries ??= new HashSet<T>();
            released ??= new HashSet<T>();
        }
    }
}
