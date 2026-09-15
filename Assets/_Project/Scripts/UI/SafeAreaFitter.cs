using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        bool isApplying;

        public readonly struct Offsets
        {
            public readonly float left;
            public readonly float right;
            public readonly float top;
            public readonly float bottom;

            public Offsets(float left, float right, float top, float bottom)
            {
                this.left = left;
                this.right = right;
                this.top = top;
                this.bottom = bottom;
            }
        }

        void OnEnable() => Apply(Screen.safeArea, new Vector2Int(Screen.width, Screen.height));

        void OnRectTransformDimensionsChange() => Apply(Screen.safeArea, new Vector2Int(Screen.width, Screen.height));

        public Offsets CalculateOffsets(Rect safeArea, Vector2 screenSize)
        {
            var left = Mathf.Max(0f, safeArea.xMin);
            var right = Mathf.Max(0f, screenSize.x - safeArea.xMax);
            var bottom = Mathf.Max(0f, safeArea.yMin);
            var top = Mathf.Max(0f, screenSize.y - safeArea.yMax);
            return new Offsets(left, right, top, bottom);
        }

        public void Apply(Rect safeArea, Vector2Int screenSize)
        {
            var rectTransform = transform as RectTransform;
            if (isApplying || rectTransform == null || screenSize.x <= 0 || screenSize.y <= 0)
                return;

            // An offset is an inset only while the anchors are apart on that axis. Where they are
            // coincident, offsetMax - offsetMin *is* sizeDelta, so writing here resizes the rect to
            // the inset instead of shrinking it by the inset — which collapses a Canvas root to 0x0
            // and drags every anchored child onto the origin.
            var stretchesX = !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x);
            var stretchesY = !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
            if (!stretchesX && !stretchesY)
                return;

            var offsets = CalculateOffsets(safeArea, screenSize);
            isApplying = true;
            try
            {
                var currentMin = rectTransform.offsetMin;
                var currentMax = rectTransform.offsetMax;
                rectTransform.offsetMin = new Vector2(
                    stretchesX ? offsets.left : currentMin.x,
                    stretchesY ? offsets.bottom : currentMin.y);
                rectTransform.offsetMax = new Vector2(
                    stretchesX ? -offsets.right : currentMax.x,
                    stretchesY ? -offsets.top : currentMax.y);
            }
            finally
            {
                isApplying = false;
            }
        }
    }
}
