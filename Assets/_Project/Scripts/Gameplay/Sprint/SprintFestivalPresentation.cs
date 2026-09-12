using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public static class SprintFestivalPresentation
    {
        static readonly Color Navy = new Color32(8, 35, 61, 242);
        static readonly Color Coral = new Color32(255, 89, 94, 235);
        static readonly Color Gold = new Color32(255, 202, 58, 235);
        static readonly Color Cream = new Color32(255, 249, 231, 255);

        public static void Build()
        {
            if (GameObject.Find("SprintFestivalChrome") != null)
                return;

            Canvas canvas = GameObject.Find("S2_HUD_Minigame")?.GetComponent<Canvas>()
                ?? Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            TMP_FontAsset font = canvas.GetComponentInChildren<TMP_Text>(true)?.font
                ?? Object.FindFirstObjectByType<TMP_Text>()?.font;

            RectTransform root = Rect(canvas.transform, "SprintFestivalChrome");
            Stretch(root);
            root.gameObject.AddComponent<KMA.Gameplay.UI.SafeAreaFitter>();
            root.SetAsFirstSibling();
            EnsurePause(root, font);

            RectTransform topBar = Rect(root, "TopBar");
            topBar.anchorMin = new Vector2(.31f, .91f);
            topBar.anchorMax = new Vector2(.69f, .985f);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;
            Image topImage = topBar.gameObject.AddComponent<Image>();
            topImage.color = Navy;
            topImage.raycastTarget = false;
            Outline topOutline = topBar.gameObject.AddComponent<Outline>();
            topOutline.effectColor = new Color32(3, 18, 33, 255);
            topOutline.effectDistance = new Vector2(3f, -3f);
            TMP_Text mode = Text(topBar, "ModeLabel", "CHẠY NƯỚC RÚT · 100 M", font, 28f,
                Cream, TextAlignmentOptions.Center);
            Stretch(mode.rectTransform, new Vector2(18f, 7f), new Vector2(-18f, -7f));

            RectTransform prompts = Rect(root, "TouchPrompts");
            prompts.anchorMin = new Vector2(0f, .02f);
            prompts.anchorMax = new Vector2(1f, .16f);
            prompts.offsetMin = Vector2.zero;
            prompts.offsetMax = Vector2.zero;
            TMP_Text left = Prompt(prompts, "LeftPrompt", "CHẠM TRÁI", font, Coral,
                new Vector2(.035f, .08f), new Vector2(.455f, .92f));
            TMP_Text right = Prompt(prompts, "RightPrompt", "CHẠM PHẢI", font, Gold,
                new Vector2(.545f, .08f), new Vector2(.965f, .92f));
            left.alignment = TextAlignmentOptions.Center;
            right.alignment = TextAlignmentOptions.Center;

            StyleTapArea("LeftTap", new Color(1f, .35f, .37f, .10f));
            StyleTapArea("RightTap", new Color(1f, .79f, .23f, .10f));
        }

        static void EnsurePause(Transform parent, TMP_FontAsset font)
        {
            if (Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>() != null)
                return;
            GameObject root = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(142f, 58f);
            Image image = root.GetComponent<Image>();
            image.color = Navy;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(3f, -3f);
            root.AddComponent<KMA.Gameplay.UI.PausePanel>();
            TMP_Text label = Text(root.transform, "Label", "TẠM DỪNG", font, 18f,
                Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, new Vector2(8f, 5f), new Vector2(-8f, -5f));
        }

        static TMP_Text Prompt(Transform parent, string name, string value, TMP_FontAsset font,
            Color color, Vector2 min, Vector2 max)
        {
            RectTransform root = Rect(parent, name);
            root.anchorMin = min;
            root.anchorMax = max;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.gameObject.SetActive(false);
            TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = 25f;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.raycastTarget = false;
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .85f);
            outline.effectDistance = new Vector2(2f, -2f);
            root.gameObject.SetActive(true);
            return text;
        }

        static void StyleTapArea(string name, Color color)
        {
            GameObject target = GameObject.Find(name);
            if (target == null)
                return;
            Image image = target.GetComponent<Image>();
            if (image != null)
                image.color = color;
        }

        static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform root = Rect(parent, name);
            root.gameObject.SetActive(false);
            TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            root.gameObject.SetActive(true);
            return text;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
