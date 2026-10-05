using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class SettingsPresentationBuilder
    {
        static Color Navy => UITheme.Shared.Surface;
        static Color Gold => UITheme.Shared.Accent;

        // Same scale as the map screen: header title, labels/controls, hint.
        const int TitleSize = 40;
        const int LabelSize = 28;
        const int HintSize = 20;
        const TextAlignmentOptions Left = TextAlignmentOptions.MidlineLeft;
        const TextAlignmentOptions Right = TextAlignmentOptions.MidlineRight;
        const TextAlignmentOptions Center = TextAlignmentOptions.Center;

        public static void Build(SettingsScreen screen)
        {
            if (screen.transform.Find("SettingsPanel") != null)
                return;
            var root = screen.GetComponent<RectTransform>() ?? screen.gameObject.AddComponent<RectTransform>();
            Stretch(root, Vector2.zero, Vector2.one);
            var backdrop = screen.gameObject.AddComponent<Image>();
            backdrop.color = UITheme.Shared.SettingsBackdrop;

            var panel = Rect(root, "SettingsPanel", new Vector2(.22f, .14f), new Vector2(.78f, .86f));
            panel.gameObject.AddComponent<Image>().color = Navy;
            Label(panel, "Title", "CÀI ĐẶT", new Vector2(.08f, .82f), new Vector2(.92f, .96f), TitleSize, Gold,
                VietFontRole.BodyBold, Center);
            var music = Volume(panel, "MusicSlider", "ÂM LƯỢNG NHẠC", .58f, out TMP_Text musicValue);
            var sfx = Volume(panel, "SfxSlider", "ÂM LƯỢNG HIỆU ỨNG", .36f, out TMP_Text sfxValue);

            Label(panel, "VibrationLabel", "RUNG", new Vector2(.09f, .23f), new Vector2(.6f, .33f), LabelSize, Color.white,
                VietFontRole.Hud, Left);
            var toggleRect = Rect(panel, "VibrationToggle", new Vector2(.67f, .23f), new Vector2(.91f, .33f));
            var toggleImage = toggleRect.gameObject.AddComponent<Image>();
            toggleImage.color = screen.Theme.Background;
            var toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = toggleImage;
            var check = Rect(toggleRect, "Checkmark", new Vector2(.08f, .3f), new Vector2(.2f, .7f));
            toggle.graphic = check.gameObject.AddComponent<Image>();
            toggle.graphic.color = Gold;
            TMP_Text vibrationValue = Label(toggleRect, "Value", "BẬT", new Vector2(.25f, 0f), Vector2.one, LabelSize,
                Color.white, VietFontRole.Hud, Center);

            var backRect = Rect(panel, "BackButton", new Vector2(.26f, .075f), new Vector2(.74f, .18f));
            var backImage = backRect.gameObject.AddComponent<Image>();
            backImage.color = screen.Theme.Background;
            var back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            var colors = back.colors;
            colors.fadeDuration = UITheme.Shared.Motion.buttonFade;
            back.colors = colors;
            back.onClick.AddListener(screen.Back);
            Label(backRect, "Label", "QUAY LẠI", Vector2.zero, Vector2.one, LabelSize, Color.white,
                VietFontRole.ButtonSecondary, Center);
            Label(panel, "Hint", "Thay đổi được lưu tự động", new Vector2(.08f, .015f), new Vector2(.92f, .065f), HintSize,
                UITheme.Shared.SettingsHint, VietFontRole.Body, Center);
            screen.BindControls(music, sfx, toggle, musicValue, sfxValue, vibrationValue);
        }

        static Slider Volume(Transform parent, string name, string title, float bottom, out TMP_Text value)
        {
            var row = Rect(parent, name + "Row", new Vector2(.09f, bottom), new Vector2(.91f, bottom + .2f));
            Label(row, "Title", title, new Vector2(0f, .55f), new Vector2(.78f, 1f), LabelSize, Color.white,
                VietFontRole.Hud, Left);
            value = Label(row, "Value", "100%", new Vector2(.78f, .55f), Vector2.one, LabelSize, Gold,
                VietFontRole.Hud, Right);
            var rect = Rect(row, name, new Vector2(0f, .08f), new Vector2(1f, .52f));
            var hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = Rect(rect, "Track", new Vector2(0f, .36f), new Vector2(1f, .64f));
            track.gameObject.AddComponent<Image>().color = UITheme.Shared.SettingsTrack;
            var fill = Rect(track, "Fill", Vector2.zero, Vector2.one);
            fill.gameObject.AddComponent<Image>().color = Gold;
            var handleArea = Rect(rect, "HandleArea", Vector2.zero, Vector2.one);
            handleArea.offsetMin = new Vector2(16f, 0f);
            handleArea.offsetMax = new Vector2(-16f, 0f);
            var handle = Rect(handleArea, "Handle", new Vector2(0f, .1f), new Vector2(0f, .9f));
            handle.sizeDelta = new Vector2(32f, 0f);
            slider.targetGraphic = handle.gameObject.AddComponent<Image>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        static TMP_Text Label(Transform parent, string name, string text, Vector2 min, Vector2 max, int size,
            Color color, VietFontRole role, TextAlignmentOptions alignment)
        {
            var label = Rect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = VietText.Fix(text);
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            VietTypography.Apply(label, role);
            label.enableWordWrapping = false;
            label.alignment = alignment;
            // Shrink to fit a narrow panel, never grow past the shared scale.
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Min(size, HintSize);
            return label;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect, min, max);
            return rect;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
