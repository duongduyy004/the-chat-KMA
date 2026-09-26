using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class HomePresentationBuilder
    {
        static readonly Color Navy = new Color32(8, 35, 61, 246);
        static readonly Color Gold = new Color32(255, 202, 58, 255);
        static readonly Color Coral = new Color32(255, 89, 94, 255);
        static readonly Color Blue = new Color32(25, 130, 196, 255);
        static readonly Color Cream = new Color32(255, 249, 231, 255);

        public static void Build(MainMenuScreen screen)
        {
            if (screen == null || screen.transform.Find("FestivalMenuPanel") != null)
                return;

            RectTransform panel = Rect(screen.transform, "FestivalMenuPanel");
            panel.anchorMin = new Vector2(.045f, .055f);
            panel.anchorMax = new Vector2(.43f, .945f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = Navy;
            panelImage.raycastTarget = false;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(3f, -3f);

            VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(34, 34, 24, 28);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            FlexibleSpacer(panel, "TopSpacer");

            Transform logo = screen.transform.Find("HomeLogo");
            if (logo != null)
            {
                logo.SetParent(panel, false);
                SetLayout(logo.gameObject, 150f);
            }

            Transform title = screen.transform.Find("HomeTitle");
            if (title != null)
            {
                title.SetParent(panel, false);
                SetLayout(title.gameObject, 64f);
            }

            Text kicker = Label(panel, "FestivalKicker", "NGÀY HỘI THỂ THAO KMA", 24,
                Gold, TextAnchor.MiddleCenter);
            kicker.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            Text subtitle = Label(panel, "FestivalSubtitle",
                "Bứt tốc · Vượt thử thách · Chinh phục huy chương", 19,
                Cream, TextAnchor.MiddleCenter);
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            RectTransform actionStack = Rect(panel, "ActionStack");
            VerticalLayoutGroup actions = actionStack.gameObject.AddComponent<VerticalLayoutGroup>();
            actions.spacing = 10f;
            actions.childAlignment = TextAnchor.MiddleCenter;
            actions.childControlWidth = true;
            actions.childControlHeight = true;
            actions.childForceExpandWidth = true;
            actions.childForceExpandHeight = false;
            LayoutElement stackLayout = actionStack.gameObject.AddComponent<LayoutElement>();
            stackLayout.flexibleWidth = 1f;
            stackLayout.preferredHeight = 342f;

            StyleAction(FindButton(screen, "CONTINUEButton"), actionStack, Blue, Color.white, 78f, 29);
            StyleAction(FindButton(screen, "NEW GAMEButton"), actionStack, Coral, Navy, 78f, 29);
            StyleAction(FindButton(screen, "SETTINGSButton"), actionStack, new Color32(13, 57, 92, 255),
                Color.white, 78f, 27);
            StyleAction(FindButton(screen, "QUITButton"), actionStack, new Color32(13, 57, 92, 255),
                Color.white, 78f, 27);

            Text footer = Label(panel, "FestivalFooter", "HÀNH TRÌNH RÈN LUYỆN THỂ CHẤT", 15,
                new Color32(190, 219, 239, 255), TextAnchor.MiddleCenter);
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            FlexibleSpacer(panel, "BottomSpacer");
        }

        static void FlexibleSpacer(Transform parent, string name)
        {
            RectTransform spacer = Rect(parent, name);
            LayoutElement element = spacer.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 0f;
            element.preferredHeight = 0f;
            element.flexibleHeight = 1f;
        }

        static void StyleAction(Button button, Transform parent, Color color, Color foreground,
            float height, int fontSize)
        {
            if (button == null)
                return;
            RectTransform rect = button.transform as RectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            LayoutElement element = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = 48f;

            Image image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.color = color;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 237, 176, 255);
            colors.pressedColor = new Color32(228, 184, 54, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.48f, .54f, .60f, .72f);
            colors.fadeDuration = .08f;
            button.colors = colors;

            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(3f, -3f);
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.color = foreground;
                label.alignment = TextAnchor.MiddleCenter;
            }
        }

        static Button FindButton(MainMenuScreen screen, string name) =>
            screen.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, name, StringComparison.Ordinal));

        static void SetLayout(GameObject target, float height)
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            LayoutElement element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.preferredHeight = height;
        }

        static Text Label(Transform parent, string name, string value, int size, Color color,
            TextAnchor alignment)
        {
            RectTransform rect = Rect(parent, name);
            return AddText(rect, value, size, color, alignment);
        }

        static Text Text(Transform parent, string name, string value, int size, Color color,
            TextAnchor alignment)
        {
            RectTransform rect = Rect(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return AddText(rect, value, size, color, alignment);
        }

        static Text AddText(RectTransform rect, string value, int size, Color color, TextAnchor alignment)
        {
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }
    }
}
