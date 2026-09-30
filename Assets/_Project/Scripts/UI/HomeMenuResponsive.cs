using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class HomeMenuResponsive : MonoBehaviour
    {
        RectTransform rect;
        RectTransform parent;

        void Awake()
        {
            rect = (RectTransform)transform;
            parent = (RectTransform)transform.parent;
            Resize();
        }

        void OnRectTransformDimensionsChange() => Resize();
        // Canvas dimensions can settle after Awake, especially in the Game view.
        void LateUpdate() => Resize();

        void Resize()
        {
            if (rect == null || parent == null) return;
            if (parent.rect.width <= 0f || parent.rect.height <= 0f) return;
            float scale = Mathf.Min(HomeMenuStyle.MenuScale, parent.rect.height / 650f,
                parent.rect.width * .45f / HomeMenuStyle.PanelWidth);
            Vector3 desiredScale = Vector3.one * scale;
            if (rect.localScale != desiredScale) rect.localScale = desiredScale;
            rect.anchorMin = rect.anchorMax = new Vector2(.045f, .46f);
        }
    }
}
