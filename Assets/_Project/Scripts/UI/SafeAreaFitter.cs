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

            var offsets = CalculateOffsets(safeArea, screenSize);
            isApplying = true;
            try
            {
                rectTransform.offsetMin = new Vector2(offsets.left, offsets.bottom);
                rectTransform.offsetMax = new Vector2(-offsets.right, -offsets.top);
            }
            finally
            {
                isApplying = false;
            }
        }
    }
}
