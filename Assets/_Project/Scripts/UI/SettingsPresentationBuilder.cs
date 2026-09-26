using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class SettingsPresentationBuilder
    {
        static readonly Color Navy = new Color32(8, 35, 61, 255);
        static readonly Color Gold = new Color32(255, 202, 58, 255);

        public static void Build(SettingsScreen screen)
        {
            if (screen.transform.Find("SettingsPanel") != null)
                return;
            var root = screen.GetComponent<RectTransform>() ?? screen.gameObject.AddComponent<RectTransform>();
            Stretch(root, Vector2.zero, Vector2.one);
            var backdrop = screen.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, .06f, .1f, .85f);

            var panel = Rect(root, "SettingsPanel", new Vector2(.22f, .14f), new Vector2(.78f, .86f));
            panel.gameObject.AddComponent<Image>().color = Navy;
            Label(panel, "Title", "CÀI ĐẶT", new Vector2(.08f, .82f), new Vector2(.92f, .96f), 42, Gold);
            var music = Volume(panel, "MusicSlider", "ÂM LƯỢNG NHẠC", .58f, out Text musicValue);
            var sfx = Volume(panel, "SfxSlider", "ÂM LƯỢNG HIỆU ỨNG", .36f, out Text sfxValue);

            Label(panel, "VibrationLabel", "RUNG", new Vector2(.09f, .23f), new Vector2(.6f, .33f), 28, Color.white);
            var toggleRect = Rect(panel, "VibrationToggle", new Vector2(.67f, .23f), new Vector2(.91f, .33f));
            var toggleImage = toggleRect.gameObject.AddComponent<Image>();
            toggleImage.color = new Color32(25, 130, 196, 255);
            var toggle = toggleRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = toggleImage;
            var check = Rect(toggleRect, "Checkmark", new Vector2(.08f, .3f), new Vector2(.2f, .7f));
            toggle.graphic = check.gameObject.AddComponent<Image>();
            toggle.graphic.color = Gold;
            var vibrationValue = Label(toggleRect, "Value", "BẬT", new Vector2(.25f, 0f), Vector2.one, 26, Color.white);

            var backRect = Rect(panel, "BackButton", new Vector2(.26f, .075f), new Vector2(.74f, .18f));
            var backImage = backRect.gameObject.AddComponent<Image>();
            backImage.color = new Color32(25, 130, 196, 255);
            var back = backRect.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.onClick.AddListener(screen.Back);
            Label(backRect, "Label", "QUAY LẠI", Vector2.zero, Vector2.one, 28, Color.white);
            Label(panel, "Hint", "Thay đổi được lưu tự động", new Vector2(.08f, .015f), new Vector2(.92f, .065f), 19,
                new Color32(190, 219, 239, 255));
            screen.BindControls(music, sfx, toggle, musicValue, sfxValue, vibrationValue);
        }

        static Slider Volume(Transform parent, string name, string title, float bottom, out Text value)
        {
            var row = Rect(parent, name + "Row", new Vector2(.09f, bottom), new Vector2(.91f, bottom + .2f));
            Label(row, "Title", title, new Vector2(0f, .55f), new Vector2(.78f, 1f), 26, Color.white);
            value = Label(row, "Value", "100%", new Vector2(.78f, .55f), Vector2.one, 26, Gold);
            var rect = Rect(row, name, new Vector2(0f, .08f), new Vector2(1f, .52f));
            var hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = Rect(rect, "Track", new Vector2(0f, .36f), new Vector2(1f, .64f));
            track.gameObject.AddComponent<Image>().color = new Color32(48, 74, 99, 255);
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

        static Text Label(Transform parent, string name, string text, Vector2 min, Vector2 max, int size, Color color)
        {
            var label = Rect(parent, name, min, max).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
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
