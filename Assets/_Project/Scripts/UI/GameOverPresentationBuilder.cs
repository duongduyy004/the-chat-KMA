using System;
using System.Linq;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    // Centered Game Over layout in the Home menu style: badge, slanted title, run stats, three actions.
    public static class GameOverPresentationBuilder
    {
        const string LayoutName = "GameOverLayout";
        static readonly Vector2 Center = new Vector2(.5f, .5f);

        public static void Build(GameOverScreen screen, GameSession session)
        {
            if (screen == null) return;
            if (screen.transform.Find(LayoutName) != null)
            {
                var current = GameOverStats.From(session);
                SetStat(screen, "StatPassed", $"{current.SubjectsPassed}/{current.SubjectsTotal}");
                SetStat(screen, "StatFailed", current.FailedVisits.ToString());
                SetStat(screen, "StatScore", current.TotalScore.ToString());
                return;
            }
            var stats = GameOverStats.From(session);

            var veil = Rect(screen.transform, "GameOverVeil");
            Stretch(veil);
            var veilImage = veil.gameObject.AddComponent<Image>();
            // Opaque: nothing from a previous scene's HUD may show through behind the result.
            veilImage.color = HomeMenuStyle.Navy;
            veil.SetAsFirstSibling();

            var layout = Rect(screen.transform, LayoutName);
            layout.anchorMin = layout.anchorMax = Center;
            layout.sizeDelta = new Vector2(900f, 1000f);
            layout.anchoredPosition = Vector2.zero;
            layout.gameObject.AddComponent<CanvasGroup>();
            layout.gameObject.AddComponent<GameOverReveal>();

            var badge = HomePresentationBuilder.CreateBadge(layout, "SportBadge");
            At(badge, new Vector2(0f, 400f), badge.sizeDelta);

            var top = Title(layout, "TitleTop", "HẾT LƯỢT", 48f, HomeMenuStyle.Gold, new Vector2(0f, 300f), new Vector2(520f, 66f));
            var main = Title(layout, "TitleMain", "GAME OVER", 94f, HomeMenuStyle.Red, new Vector2(0f, 205f), new Vector2(760f, 115f));
            TrackLines(layout, 125f);
            var subtitle = UiKit.Label(layout, "Subtitle",
                $"Bạn đã dùng hết {GameSession.MaxLives} lượt. Thử lại nhé!", 28f, HomeMenuStyle.White);
            Body(subtitle, new Vector2(0f, 65f), new Vector2(820f, 48f));

            Stat(layout, "StatPassed", $"{stats.SubjectsPassed}/{stats.SubjectsTotal}", "MÔN ĐÃ QUA", -290f);
            Stat(layout, "StatFailed", stats.FailedVisits.ToString(), "LƯỢT THẤT BẠI", 0f);
            Stat(layout, "StatScore", stats.TotalScore.ToString(), "TỔNG ĐIỂM", 290f);

            Action(screen, layout, "RETRYButton", "CHƠI LẠI", -215f, HomeMenuButton.Kind.Continue, true);
            Action(screen, layout, "NEW GAMEButton", "CHƠI MỚI", -295f, HomeMenuButton.Kind.NewGame, false);
            Action(screen, layout, "MAIN MENUButton", "MENU CHÍNH", -375f, HomeMenuButton.Kind.Secondary, false);
        }

        static void SetStat(GameOverScreen screen, string name, string value)
        {
            var label = screen.transform.Find(LayoutName + "/" + name + "/Value")?.GetComponent<TMP_Text>();
            if (label != null) label.text = VietText.Fix(value);
        }

        static TMP_Text Title(RectTransform parent, string name, string text, float size, Color color, Vector2 position, Vector2 box)
        {
            var label = UiKit.Label(parent, name, text, size, color);
            VietTypography.Apply(label, VietFontRole.Title);
            label.fontStyle = FontStyles.Italic;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            At(label.rectTransform, position, box);
            label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, UITheme.Shared.Menu.titleAngle);
            return label;
        }

        static void Body(TMP_Text label, Vector2 position, Vector2 box)
        {
            label.fontStyle = FontStyles.Normal;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            At(label.rectTransform, position, box);
        }

        static void TrackLines(RectTransform parent, float y)
        {
            for (int i = 0; i < 3; i++)
            {
                var line = Rect(parent, "TrackLine" + i);
                At(line, new Vector2(0f, y - i * 9f), new Vector2(340f - i * 60f, 3f));
                line.localRotation = Quaternion.Euler(0f, 0f, UITheme.Shared.Menu.titleAngle);
                var image = line.gameObject.AddComponent<Image>();
                image.color = HomeMenuStyle.Gold;
                image.raycastTarget = false;
            }
        }

        static void Stat(RectTransform parent, string name, string value, string caption, float x)
        {
            const int width = 260, height = 120;
            var card = Rect(parent, name);
            At(card, new Vector2(x, -70f), new Vector2(width, height));
            var border = card.gameObject.AddComponent<Image>();
            border.sprite = HomePresentationBuilder.SlantSprite(true, width, height);
            border.color = HomeMenuStyle.White;
            border.raycastTarget = false;
            var fill = Rect(card, "Fill").gameObject.AddComponent<Image>();
            Stretch(fill.rectTransform);
            float stroke = UITheme.Shared.BorderWidth * .5f;
            fill.rectTransform.offsetMin = Vector2.one * stroke;
            fill.rectTransform.offsetMax = -Vector2.one * stroke;
            fill.sprite = HomePresentationBuilder.SlantSprite(false, width, height);
            fill.color = HomeMenuStyle.Glass;
            fill.raycastTarget = false;

            var number = UiKit.Label(card, "Value", value, 54f, HomeMenuStyle.Gold);
            number.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.FitLabel(number, 54f);
            Anchor(number.rectTransform, new Vector2(.05f, .42f), new Vector2(.95f, .95f));
            var text = UiKit.Label(card, "Caption", caption, 28f, HomeMenuStyle.White);
            text.fontStyle = FontStyles.Normal;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.FitLabel(text, 28f);
            Anchor(text.rectTransform, new Vector2(.05f, .05f), new Vector2(.95f, .45f));
        }

        static void Action(GameOverScreen screen, RectTransform parent, string name, string caption,
            float y, HomeMenuButton.Kind kind, bool primary)
        {
            var button = screen.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (button == null) return;
            var visual = HomePresentationBuilder.StyleButton(button, parent, caption, kind, Center,
                new Vector2(0f, y), new Vector2(380f, 64f));
            visual.SetPrimary(primary);
            // SetPrimary shrinks labels to the Home size; keep the shared minimum font size.
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.fontSize = MinigameUiTheme.MinimumFontSize;
                if (!label.enableAutoSizing) continue;
                label.fontSizeMin = label.fontSizeMax = MinigameUiTheme.MinimumFontSize;
            }
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void At(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Center;
            rect.pivot = Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect) => Anchor(rect, Vector2.zero, Vector2.one);
    }
}
