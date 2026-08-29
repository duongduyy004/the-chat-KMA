using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] TMP_Text floatingTextPrefab;
        [SerializeField, Min(1)] int prewarmCount = 8;

        readonly Queue<TMP_Text> available = new Queue<TMP_Text>();

        public int AvailableCount => available.Count;

        void Awake() => Prewarm();

        public void Prewarm()
        {
            if (floatingTextPrefab == null)
                return;
            while (available.Count < prewarmCount)
                Release(CreateEntry());
        }

        public TMP_Text Show(string value, Vector2 anchoredPosition, float duration = .7f)
        {
            var entry = available.Count > 0 ? available.Dequeue() : CreateEntry();
            if (entry == null)
                return null;

            entry.text = value;
            entry.rectTransform.anchoredPosition = anchoredPosition;
            entry.gameObject.SetActive(true);
            StartCoroutine(ReleaseAfter(entry, duration));
            return entry;
        }

        TMP_Text CreateEntry()
        {
            if (floatingTextPrefab == null)
                return null;
            var entry = Instantiate(floatingTextPrefab, transform);
            entry.gameObject.SetActive(false);
            return entry;
        }

        IEnumerator ReleaseAfter(TMP_Text entry, float duration)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));
            Release(entry);
        }

        public void Release(TMP_Text entry)
        {
            if (entry == null || available.Contains(entry))
                return;
            entry.gameObject.SetActive(false);
            available.Enqueue(entry);
        }
    }
}
