using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Celebration
{
    /// A few flat paper bits falling over the scene. Deterministic, UI-only, no particle system.
    public sealed class UiConfetti : MonoBehaviour
    {
        [SerializeField] Color[] colors =
        {
            new Color32(0xFF, 0xC9, 0x28, 0xFF), new Color32(0xE8, 0x5A, 0x48, 0xFF),
            new Color32(0x4F, 0xB3, 0xF0, 0xFF), new Color32(0x62, 0xB5, 0x4A, 0xFF)
        };
        [SerializeField] int count = 48;
        readonly List<(RectTransform rect, float speed, float spin, float sway)> bits =
            new List<(RectTransform, float, float, float)>();

        public bool Playing { get; private set; }

        public void Play()
        {
            if (Playing) return;
            Playing = true;
            var random = new System.Random(20261006);
            var area = (RectTransform)transform;
            for (int i = 0; i < count; i++)
            {
                var rect = (RectTransform)new GameObject("Bit" + i, typeof(RectTransform), typeof(Image)).transform;
                rect.SetParent(transform, false);
                rect.sizeDelta = new Vector2(14f + random.Next(10), 22f + random.Next(12));
                rect.anchorMin = rect.anchorMax = new Vector2((float)random.NextDouble(), 1f);
                rect.anchoredPosition = new Vector2(0f, 40f + random.Next((int)Mathf.Max(1f, area.rect.height)));
                var image = rect.GetComponent<Image>();
                image.color = colors[i % colors.Length];
                image.raycastTarget = false;
                bits.Add((rect, 160f + random.Next(140), random.Next(-180, 180), (float)random.NextDouble() * 6f));
            }
        }

        void Update()
        {
            if (!Playing) return;
            float height = ((RectTransform)transform).rect.height;
            foreach ((RectTransform rect, float speed, float spin, float sway) in bits)
            {
                Vector2 p = rect.anchoredPosition;
                p.y -= speed * Time.deltaTime;
                p.x = Mathf.Sin(Time.time * 2f + sway) * 24f;
                if (p.y < -height - 40f) p.y = 40f;
                rect.anchoredPosition = p;
                rect.Rotate(0f, 0f, spin * Time.deltaTime);
            }
        }
    }
}
