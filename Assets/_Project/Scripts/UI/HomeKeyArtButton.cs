using KMA.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
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
