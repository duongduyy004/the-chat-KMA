using KMA.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class HomeMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public enum Kind { NewGame, Continue, Secondary, Exit }

        [SerializeField] Kind kind;
        Button button;
        [SerializeField] Image border;
        [SerializeField] Image fill;
        [SerializeField] Image arrow;
        Image icon;
        RectTransform rect;
        Vector2 restingPosition;
        bool hovered;
        bool focused;
        bool pressed;
        [SerializeField] bool primary;
        TMP_Text[] labels;

        public Kind ButtonKind => kind;

        void Awake()
        {
            if (border != null && fill != null)
                Initialize(kind, border, fill, arrow);
        }

        public void Initialize(Kind buttonKind, Image borderImage, Image fillImage, Image arrowImage)
        {
            kind = buttonKind;
            button = GetComponent<Button>();
            border = borderImage;
            fill = fillImage;
            arrow = arrowImage;
            icon = transform.Find("Icon")?.GetComponent<Image>();
            rect = (RectTransform)transform;
            restingPosition = rect.anchoredPosition;
            labels = GetComponentsInChildren<TMP_Text>(true);
            Apply();
        }

        public void SetPrimary(bool value)
        {
            primary = value;
            if (labels != null)
                foreach (var label in labels)
                {
                    label.fontSize = primary ? 23f : 22f;
                    if (label.enableAutoSizing)
                    {
                        label.fontSizeMax = label.fontSize;
                        label.fontSizeMin = Mathf.Min(label.fontSizeMin, 18f);
                    }
                }
            Apply();
        }

        void Update()
        {
            if (button == null) return;
            // Button.interactable can change while this screen is hidden or after loading a save.
            Apply();
        }

        void Apply()
        {
            if (button == null || border == null || fill == null) return;
            bool enabled = button.interactable;
            bool active = enabled && (hovered || focused);
            border.color = !enabled ? UITheme.Shared.Menu.disabledBorder :
                active ? HomeMenuStyle.Gold : HomeMenuStyle.White;
            fill.color = !enabled ? UITheme.Shared.Menu.disabledFill :
                kind == Kind.Exit ? new Color(0f, 0f, 0f, 0f) :
                primary ? HomeMenuStyle.Red : HomeMenuStyle.Glass;
            rect.anchoredPosition = restingPosition + (active ? new Vector2(8f, 0f) : Vector2.zero);
            rect.localScale = Vector3.one * (pressed && enabled ? UITheme.Shared.Menu.pressScale : 1f);
            if (arrow != null) arrow.gameObject.SetActive(active);
            if (icon != null) icon.color = enabled ? HomeMenuStyle.White :
                UITheme.Shared.Menu.disabledText;
            foreach (var label in labels)
                if (label != arrow) label.color = enabled ? HomeMenuStyle.White :
                    UITheme.Shared.Menu.disabledText;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            hovered = true;
            GameAudio.Play(GameSound.Click);
            Apply();
        }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; Apply(); }
        public void OnPointerDown(PointerEventData eventData) { pressed = true; Apply(); }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; Apply(); }
        public void OnSelect(BaseEventData eventData) { focused = true; Apply(); }
        public void OnDeselect(BaseEventData eventData) { focused = false; pressed = false; Apply(); }
    }
}
