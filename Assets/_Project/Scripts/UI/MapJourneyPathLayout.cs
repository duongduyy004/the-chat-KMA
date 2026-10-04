using System.Collections.Generic;
using System.Linq;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    [DisallowMultipleComponent]
    public sealed class MapJourneyPathLayout : MonoBehaviour
    {
        readonly List<MapNodeView> nodes = new List<MapNodeView>(3);
        readonly List<Image> pathTracks = new List<Image>(2);
        readonly List<Image> pathProgress = new List<Image>(2);
        RectTransform root;
        Vector2 lastSize;

        public void Configure(IEnumerable<MapNodeView> mapNodes)
        {
            root = (RectTransform)transform;
            nodes.Clear();
            if (mapNodes != null)
                nodes.AddRange(mapNodes.Where(node => node != null && !node.IsComingSoon)
                    .OrderBy(node => CourseOrder(node.SubjectId)));

            while (pathTracks.Count < Mathf.Max(0, nodes.Count - 1))
                CreatePathSegment(pathTracks.Count);

            LayoutPath(true);
        }

        void LateUpdate()
        {
            if (root == null) root = (RectTransform)transform;
            if (root == null || nodes.Count == 0) return;
            if (root.rect.size != lastSize)
                LayoutPath(false);
            UpdateProgressColors();
        }

        void LayoutPath(bool force)
        {
            if (root == null || root.rect.width <= 1f || root.rect.height <= 1f) return;
            Vector2 size = root.rect.size;
            if (!force && size == lastSize) return;
            lastSize = size;

            float cardWidth = Mathf.Min(430f, size.x * .29f);
            float cardHeight = Mathf.Clamp(size.y * .47f, 196f, 258f);
            float[] x = { .17f, .5f, .83f };
            // Keep the three courses on one baseline so the route reads as a
            // continuous sequence, like the chapter preview.
            const float centerY = .54f;
            int count = Mathf.Min(nodes.Count, 3);
            for (int i = 0; i < count; i++)
            {
                RectTransform card = nodes[i].GetComponent<RectTransform>();
                if (card == null) continue;
                Vector2 anchor = new Vector2(x[i], centerY);
                card.anchorMin = card.anchorMax = anchor;
                card.pivot = new Vector2(.5f, .5f);
                card.anchoredPosition = Vector2.zero;
                card.sizeDelta = new Vector2(cardWidth, cardHeight);
            }

            for (int i = 0; i < Mathf.Min(pathTracks.Count, count - 1); i++)
                PositionSegment(i, nodes[i].GetComponent<RectTransform>(),
                    nodes[i + 1].GetComponent<RectTransform>());
        }

        void CreatePathSegment(int index)
        {
            // Insert the colored route above its dark outline, while keeping
            // both behind the course cards.
            var progress = CreateImage($"PathProgress{index + 1}", HomeMenuStyle.White, 10f);
            var track = CreateImage($"PathTrack{index + 1}", HomeMenuStyle.Navy, 18f);
            pathTracks.Add(track);
            pathProgress.Add(progress);
        }

        Image CreateImage(string objectName, Color color, float thickness)
        {
            Transform existing = transform.Find(objectName);
            var go = existing != null ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null) go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(1f, thickness);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        void PositionSegment(int index, RectTransform from, RectTransform to)
        {
            if (from == null || to == null) return;
            Vector3 start = from.TransformPoint(new Vector3(from.rect.width * .5f, 0f));
            Vector3 end = to.TransformPoint(new Vector3(-to.rect.width * .5f, 0f));
            Vector3 localStart = root.InverseTransformPoint(start);
            Vector3 localEnd = root.InverseTransformPoint(end);
            Vector2 delta = localEnd - localStart;
            Vector2 midpoint = (localStart + localEnd) * .5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            RectTransform track = pathTracks[index].rectTransform;
            track.anchoredPosition = midpoint;
            track.sizeDelta = new Vector2(length, 18f);
            track.localRotation = Quaternion.Euler(0f, 0f, angle);

            RectTransform progress = pathProgress[index].rectTransform;
            progress.anchoredPosition = midpoint;
            progress.sizeDelta = new Vector2(length, 10f);
            progress.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void UpdateProgressColors()
        {
            for (int i = 0; i < pathProgress.Count && i + 1 < nodes.Count; i++)
            {
                bool passed = nodes[i].Stars > 0;
                pathProgress[i].color = passed ? HomeMenuStyle.Gold : HomeMenuStyle.White;
            }
        }

        static int CourseOrder(SubjectId id) => id switch
        {
            SubjectId.Sprint => 0,
            SubjectId.Volleyball => 1,
            SubjectId.Football => 2,
            _ => int.MaxValue
        };
    }
}
