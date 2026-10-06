using KMA.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    /// An invisible button laid over a button face painted in the key art: tints on hover and press,
    /// greys out while disabled, and plays the menu click.
    public sealed class HomeKeyArtButton : MonoBehaviour, IPointerEnterHandler
    {
        static readonly Color Idle = new Color(1f, 1f, 1f, 0f);
        static readonly Color Hover = new Color(1f, .86f, .25f, .28f);
        static readonly Color Press = new Color(.04f, .16f, .29f, .22f);

        [SerializeField] Image shade;
        Button button;

        public void Configure(Button target, Image disabledShade)
        {
            button = target;
            shade = disabledShade;
            var colors = button.colors;
            colors.normalColor = Idle;
            colors.highlightedColor = Hover;
            colors.selectedColor = Hover;
            colors.pressedColor = Press;
            colors.disabledColor = Idle;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = button.GetComponent<Image>();
            Refresh();
        }

        void Awake() => button = GetComponent<Button>();

        void Update() => Refresh();

        void Refresh()
        {
            if (button != null && shade != null) shade.enabled = !button.interactable;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && button.interactable) GameAudio.Play(GameSound.Click);
        }
    }
}
