using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    // Geometry and decoration only; JourneyLessonList owns progress and navigation.
    internal static class JourneyLessonPresentation
    {
        static UITheme Theme => UITheme.Shared;
        static UITheme.LessonJourneyStyle Style => Theme.LessonJourney;

        public static RectTransform Create(Transform parent)
        {
            Image surface = Shape(parent, "JourneyLessons", Theme.Surface);
            RectTransform panel = surface.rectTransform;
            Anchor(panel, Style.panelAnchorMin, Style.panelAnchorMax);
            AddBorder(surface, Theme.MapLockedBorder);

            CreateCourtPattern(panel);
            Image accent = Shape(panel, "ChapterAccent", Theme.LessonJourney.sprint);
            Anchor(accent.rectTransform, new Vector2(.025f, .982f), new Vector2(.975f, .989f));

            Image emblem = Shape(panel, "CourseIcon", Theme.LessonJourney.sprint, true);
            Place(emblem.rectTransform, new Vector2(.025f, .885f), new Vector2(76f, 76f), new Vector2(0f, .5f));
            SubjectId[] subjects = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football };
            foreach (SubjectId subject in subjects)
            {
                Image sport = Shape(emblem.transform, subject + "Glyph", Theme.Surface);
                sport.sprite = MapPresentationBuilder.SportIconSprite(subject);
                sport.type = Image.Type.Simple;
                sport.preserveAspect = true;
                Anchor(sport.rectTransform, Vector2.one * .2f, Vector2.one * .8f);
                sport.gameObject.SetActive(subject == SubjectId.Sprint);
            }

            TMP_Text heading = Label(panel, "CourseTitle", Style.headingSize, Theme.TextPrimary, FontStyles.Bold);
            Anchor(heading.rectTransform, new Vector2(.025f, .875f), new Vector2(.66f, .96f));
            heading.rectTransform.offsetMin = new Vector2(96f, 0f);
            TMP_Text progress = Label(panel, "CourseProgress", Style.bodySize, Theme.MapHint);
            Anchor(progress.rectTransform, new Vector2(.025f, .80f), new Vector2(.66f, .875f));
            progress.rectTransform.offsetMin = new Vector2(96f, 0f);

            Button continueButton = ActionButton(panel, "ContinueCheckpoint", "TIẾP TỤC BÀI HỌC  ›");
            Anchor((RectTransform)continueButton.transform, new Vector2(.69f, .835f), new Vector2(.975f, .955f));

            for (int index = 0; index < 2; index++) CreateConnector(panel, index);
            for (int index = 0; index < 3; index++) CreateCard(panel, index);

            TMP_Text hint = Label(panel, "JourneyHint", Style.captionSize, Theme.MapHint);
            hint.alignment = TextAlignmentOptions.Center;
            Anchor(hint.rectTransform, new Vector2(.025f, .018f), new Vector2(.975f, .085f));
            return panel;
        }

        static void CreateCard(Transform panel, int index)
        {
            Image face = Shape(panel, $"Lesson{index + 1}", Theme.Card);
            RectTransform rect = face.rectTransform;
            float left = Style.cardLeft + index * (Style.cardWidth + Style.cardGap);
            Anchor(rect, new Vector2(left, Style.cardBottom), new Vector2(left + Style.cardWidth, Style.cardTop));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 320f;
            face.raycastTarget = true;
            AddBorder(face, Theme.MapLockedBorder);
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.disabledColor = Color.white;
            colors.pressedColor = Color.Lerp(Color.white, Color.black, Theme.Motion.pressLighten);
            colors.fadeDuration = Theme.Motion.buttonFade;
            button.colors = colors;
            rect.gameObject.AddComponent<CanvasGroup>();

            TMP_Text number = Label(rect, "StepNumber", Style.captionSize, Theme.MutedForeground, FontStyles.Bold);
            number.text = $"0{index + 1}";
            Anchor(number.rectTransform, new Vector2(.055f, .86f), new Vector2(.22f, .975f));

            Image badge = Shape(rect, "StateBadge", Theme.Accent);
            Anchor(badge.rectTransform, new Vector2(.46f, .865f), new Vector2(.945f, .965f));
            TMP_Text state = Label(badge.transform, "Label", Style.captionSize, Theme.Surface, FontStyles.Bold);
            state.alignment = TextAlignmentOptions.Center;
            Anchor(state.rectTransform, Vector2.zero, Vector2.one);
            RectTransform completeMark = Rect(badge.transform, "CompletedMark");
            Anchor(completeMark, new Vector2(.04f, .1f), new Vector2(.22f, .9f));
            Image shortStroke = Shape(completeMark, "ShortStroke", Theme.Surface);
            Place(shortStroke.rectTransform, new Vector2(.30f, .40f), new Vector2(10f, 4f));
            shortStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            Image longStroke = Shape(completeMark, "LongStroke", Theme.Surface);
            Place(longStroke.rectTransform, new Vector2(.62f, .54f), new Vector2(18f, 4f));
            longStroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            completeMark.gameObject.SetActive(false);

            Image disc = Shape(rect, "StageIcon", Theme.Accent, true);
            Place(disc.rectTransform, new Vector2(.5f, .73f), Vector2.one * Style.iconSize);
            Image glow = Shape(disc.transform, "Glow", Theme.Accent, true);
            glow.sprite = UiKitAssets.Load().Ring;
            Anchor(glow.rectTransform, Vector2.one * -.10f, Vector2.one * 1.10f);
            RectTransform glyph = Rect(disc.transform, "Glyph");
            Anchor(glyph, Vector2.one * .20f, Vector2.one * .80f);
            CreateStageGlyph(glyph, index);

            TMP_Text title = Label(rect, "StageTitle", Style.stageSize, Theme.Surface, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Center;
            Anchor(title.rectTransform, new Vector2(.05f, .45f), new Vector2(.95f, .59f));

            TMP_Text objective = Label(rect, "Objective", Style.bodySize, Theme.Surface);
            objective.alignment = TextAlignmentOptions.Top;
            objective.enableWordWrapping = true;
            Anchor(objective.rectTransform, new Vector2(.055f, .18f), new Vector2(.945f, .445f));

            Image action = Shape(rect, "ActionSurface", Theme.Muted);
            Anchor(action.rectTransform, new Vector2(.04f, .04f), new Vector2(.96f, .175f));
            TMP_Text status = Label(rect, "Status", Style.captionSize, Theme.Surface, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.Center;
            Anchor(status.rectTransform, new Vector2(.045f, .04f), new Vector2(.955f, .175f));
        }

        static void CreateConnector(RectTransform panel, int index)
        {
            float left = Style.cardLeft + Style.cardWidth + index * (Style.cardWidth + Style.cardGap);
            Image track = Shape(panel, $"LessonConnector{index + 1}", Theme.MapLockedBorder);
            Anchor(track.rectTransform, new Vector2(left, .425f), new Vector2(left + Style.cardGap, .437f));
            Image arrow = Shape(panel, $"LessonArrow{index + 1}", Theme.MapLockedBorder, true);
            Place(arrow.rectTransform, new Vector2(left + Style.cardGap * .5f, .431f), new Vector2(38f, 38f));
            TMP_Text mark = Label(arrow.transform, "Label", Style.bodySize, Theme.Surface, FontStyles.Bold);
            mark.text = "›";
            mark.alignment = TextAlignmentOptions.Center;
            Anchor(mark.rectTransform, Vector2.zero, Vector2.one);
        }

        static void CreateStageGlyph(RectTransform root, int index)
        {
            if (index == 0)
            {
                // Open book, with a spine and two readable page strokes.
                Box(root, "LeftPage", .05f, .12f, .46f, .85f);
                Box(root, "RightPage", .54f, .12f, .95f, .85f);
                Box(root, "Spine", .46f, .02f, .54f, .82f);
                PageStroke(root, "LineLeft1", .14f, .62f, .36f);
                PageStroke(root, "LineLeft2", .14f, .40f, .36f);
                PageStroke(root, "LineRight1", .64f, .62f, .86f);
                PageStroke(root, "LineRight2", .64f, .40f, .86f);
            }
            else if (index == 1)
            {
                Image outer = Shape(root, "OuterTarget", Theme.Surface, true);
                outer.sprite = UiKitAssets.Load().Ring;
                Anchor(outer.rectTransform, Vector2.zero, Vector2.one);
                Image inner = Shape(root, "InnerTarget", Theme.Surface, true);
                inner.sprite = UiKitAssets.Load().Ring;
                Anchor(inner.rectTransform, Vector2.one * .23f, Vector2.one * .77f);
                Image center = Shape(root, "Bullseye", Theme.Surface, true);
                Anchor(center.rectTransform, Vector2.one * .43f, Vector2.one * .57f);
            }
            else
            {
                Image left = Shape(root, "LeftHandle", Theme.Surface, true);
                left.sprite = UiKitAssets.Load().Ring;
                Anchor(left.rectTransform, new Vector2(0f, .44f), new Vector2(.45f, .86f));
                Image right = Shape(root, "RightHandle", Theme.Surface, true);
                right.sprite = UiKitAssets.Load().Ring;
                Anchor(right.rectTransform, new Vector2(.55f, .44f), new Vector2(1f, .86f));
                Box(root, "Cup", .22f, .38f, .78f, .96f);
                Box(root, "Stem", .43f, .13f, .57f, .45f);
                Box(root, "Base", .24f, .03f, .76f, .15f);
            }
        }

        static void PageStroke(Transform root, string name, float xMin, float y, float xMax)
        {
            Image line = Shape(root, name, Theme.Accent);
            line.sprite = null;
            Anchor(line.rectTransform, new Vector2(xMin, y), new Vector2(xMax, y + .045f));
        }

        static void Box(Transform root, string name, float xMin, float yMin, float xMax, float yMax)
        {
            Image image = Shape(root, name, Theme.Surface);
            image.sprite = null;
            Anchor(image.rectTransform, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        }

        static void CreateCourtPattern(Transform panel)
        {
            RectTransform pattern = Rect(panel, "CourtPattern");
            Anchor(pattern, new Vector2(.70f, .08f), new Vector2(.98f, .78f));
            pattern.gameObject.AddComponent<CanvasGroup>().alpha = .06f;
            for (int sport = 0; sport < 3; sport++)
            {
                RectTransform court = Rect(pattern, new[] { "Sprint", "Volleyball", "Football" }[sport]);
                Anchor(court, Vector2.zero, Vector2.one);
                for (int i = 0; i < (sport == 1 ? 6 : 3); i++)
                {
                    float x = .08f + i * (sport == 1 ? .16f : .42f);
                    Image lane = Shape(court, $"Line{i}", Theme.TextPrimary);
                    Anchor(lane.rectTransform, new Vector2(x, .05f), new Vector2(x + .012f, .95f));
                }
                if (sport > 0)
                {
                    for (int i = 0; i < (sport == 1 ? 6 : 3); i++)
                    {
                        float y = .05f + i * (sport == 1 ? .18f : .45f);
                        Image line = Shape(court, $"Cross{i}", Theme.TextPrimary);
                        Anchor(line.rectTransform, new Vector2(.08f, y), new Vector2(.932f, y + .012f));
                    }
                }
                if (sport == 2)
                {
                    Image center = Shape(court, "CenterCircle", Theme.TextPrimary, true);
                    center.sprite = UiKitAssets.Load().Ring;
                    Place(center.rectTransform, Vector2.one * .5f, new Vector2(92f, 92f));
                }
                court.gameObject.SetActive(sport == 0);
            }
        }

        static Button ActionButton(Transform parent, string name, string text)
        {
            Image image = Shape(parent, name, Theme.Accent);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            AddBorder(image, Theme.Menu.goldDark);
            TMP_Text label = Label(image.transform, "Label", Style.bodySize, Theme.Surface, FontStyles.Bold);
            label.text = VietText.Fix(text);
            label.alignment = TextAlignmentOptions.Center;
            Anchor(label.rectTransform, new Vector2(.02f, 0f), new Vector2(.98f, 1f));
            image.gameObject.AddComponent<KitPressFeedback>().Configure(image, image.rectTransform);
            return button;
        }

        static void AddBorder(Image image, Color color)
        {
            Outline border = image.gameObject.AddComponent<Outline>();
            border.effectColor = color;
            border.effectDistance = new Vector2(Style.borderWidth, -Style.borderWidth);
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Theme.ShadowColor;
            shadow.effectDistance = Theme.ShadowOffset;
        }

        static Image Shape(Transform parent, string name, Color color, bool circle = false)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = circle ? UiKitAssets.Load().Circle : UiKitAssets.Load().RoundRect20;
            image.type = circle ? Image.Type.Simple : Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Label(Transform parent, string name, float size, Color color,
            FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Rect(parent, name);
            TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            VietTypography.Apply(label);
            return label;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
