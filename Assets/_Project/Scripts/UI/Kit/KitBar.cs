using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// A rounded progress bar. The fill is a sliced child that grows to the value, so both of its
    /// ends stay round at any width, and the corner radius follows half the bar's height.
    public sealed class KitBar : MonoBehaviour
    {
        [SerializeField] Image track;
        [SerializeField] Image fill;
        [SerializeField] Image pip;
        [SerializeField] TMP_Text label;

        public float Value { get; private set; }
        public Image Track => track;
        public Image Fill => fill;
        public Image Pip => pip;
        public TMP_Text Label => label;

        public void Configure(Image trackImage, Image fillImage, Image pipImage, TMP_Text labelText)
        {
            track = trackImage;
            fill = fillImage;
            pip = pipImage;
            label = labelText;
            ApplyRadius();
        }

        public void SetValue(float value)
        {
            Value = Mathf.Clamp01(value);
            if (fill != null)
            {
                fill.rectTransform.anchorMax = new Vector2(Value, 1f);
                fill.enabled = Value > .001f;
            }
            if (pip != null)
            {
                RectTransform rect = pip.rectTransform;
                rect.anchorMin = new Vector2(Value, rect.anchorMin.y);
                rect.anchorMax = new Vector2(Value, rect.anchorMax.y);
            }
        }

        public void SetFillColor(Color color)
        {
            if (fill != null)
                fill.color = color;
        }

        void OnRectTransformDimensionsChange() => ApplyRadius();

        void ApplyRadius()
        {
            if (track == null)
                return;
            float height = track.rectTransform.rect.height;
            if (height <= 0f)
                return;
            float radius = height * .5f;
            UiKit.SetRadius(track, radius);
            if (fill != null)
                UiKit.SetRadius(fill, radius);
        }
    }
}
