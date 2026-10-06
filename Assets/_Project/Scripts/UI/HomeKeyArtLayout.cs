using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Keeps the menu's hit areas glued to the key-art illustration: the panel, title and button faces
    /// are painted into that picture, so the layout takes the illustration's exact on-screen rectangle
    /// (which differs from this screen's own rect on devices with a safe-area inset).
    public sealed class HomeKeyArtLayout : MonoBehaviour
    {
        [SerializeField] RectTransform reference;
        readonly Vector3[] corners = new Vector3[4];

        public void Configure(RectTransform art)
        {
            reference = art;
            Align();
        }

        void LateUpdate() => Align();

        void Align()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (reference == null || parent == null) return;
            reference.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]);
            Vector2 max = parent.InverseTransformPoint(corners[2]);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = max - min;
            rect.anchoredPosition = (min + max) * .5f - parent.rect.center;
        }
    }
}
