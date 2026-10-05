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
        static readonly string[] StageIcons = { "StageIcon_Learn", "StageIcon_Practice", "StageIcon_Exam" };

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
            Place(emblem.rectTransform, new Vector2(.025f, .90f), new Vector2(52f, 52f), new Vector2(0f, .5f));
            SubjectId[] subjects = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess };
            foreach (SubjectId subject in subjects)
            {
                Image sport = Shape(emblem.transform, subject + "Glyph", Theme.Surface);
                sport.sprite = MapPresentationBuilder.SportIconSprite(subject);
                sport.type = Image.Type.Simple;
                sport.preserveAspect = true;
                Anchor(sport.rectTransform, Vector2.one * .2f, Vector2.one * .8f);
                sport.gameObject.SetActive(subject == SubjectId.Sprint);
            }

            TMP_Text heading = Label(panel, "CourseTitle", Style.headingSize, Theme.TextPrimary, FontStyles.Bold,
                VietFontRole.Hud);
            Anchor(heading.rectTransform, new Vector2(.025f, .83f), new Vector2(.30f, .97f));
            heading.rectTransform.offsetMin = new Vector2(70f, 0f);
            TMP_Text progress = Label(panel, "CourseProgress", Style.bodySize, Theme.MapHint);
            Anchor(progress.rectTransform, new Vector2(.31f, .83f), new Vector2(.66f, .97f));

            Button continueButton = ActionButton(panel, "ContinueCheckpoint", "TIẾP TỤC BÀI HỌC  ›");
            Anchor((RectTransform)continueButton.transform, new Vector2(.69f, .84f), new Vector2(.975f, .96f));

            for (int index = 0; index < 2; index++) CreateConnector(panel, index);
            for (int index = 0; index < 3; index++) CreateCard(panel, index);

            TMP_Text hint = Label(panel, "JourneyHint", Style.captionSize - 2f, Theme.MapHint);
            hint.alignment = TextAlignmentOptions.Center;
            Anchor(hint.rectTransform, new Vector2(.025f, Style.cardTop + .02f), new Vector2(.975f, .82f));
            return panel;
        }

        static void CreateCard(Transform panel, int index)
        {
            Image face = Shape(panel, $"Lesson{index + 1}", Theme.Card);
            RectTransform rect = face.rectTransform;
            float left = Style.cardLeft + index * (Style.cardWidth + Style.cardGap);
            Anchor(rect, new Vector2(left, Style.cardBottom), new Vector2(left + Style.cardWidth, Style.cardTop));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
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

            float pad = Style.cardInset;
            float gap = Style.cardSpacing;
            float icon = Style.iconSize;
            float textLeft = pad + icon + gap;
            float headerBottom = pad + icon;
            float actionTop = pad + Style.cardActionHeight;

            // Header: icon on the left, step number above the title to its right, state at top right.
            Image disc = Shape(rect, "StageIcon", Theme.Accent, true);
            Inset(disc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad, -headerBottom), new Vector2(pad + icon, -pad));
            Image glow = Shape(disc.transform, "Glow", Theme.Accent, true);
            glow.sprite = UiKitAssets.Load().Ring;
            Anchor(glow.rectTransform, Vector2.one * -.10f, Vector2.one * 1.10f);
            Image glyph = Shape(disc.transform, "Glyph", Theme.Surface);
            glyph.sprite = Resources.Load<Sprite>("Icons/" + StageIcons[index]);
            glyph.type = Image.Type.Simple;
            glyph.preserveAspect = true;
            Anchor(glyph.rectTransform, Vector2.one * .18f, Vector2.one * .82f);

            Image badge = Shape(rect, "StateBadge", Theme.Accent);
            Inset(badge.rectTransform, new Vector2(.58f, 1f), Vector2.one,
                new Vector2(0f, -pad - Style.cardBadgeHeight), new Vector2(-pad, -pad));
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

            TMP_Text number = Label(rect, "StepNumber", Style.captionSize, Theme.MutedForeground, FontStyles.Bold);
            number.text = $"0{index + 1}";
            Inset(number.rectTransform, new Vector2(0f, 1f), new Vector2(.56f, 1f),
                new Vector2(textLeft, -pad - Style.cardStepHeight), new Vector2(0f, -pad));

            TMP_Text title = Label(rect, "StageTitle", Style.stageSize, Theme.Surface, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            Inset(title.rectTransform, new Vector2(0f, 1f), Vector2.one,
                new Vector2(textLeft, -headerBottom), new Vector2(-pad, -pad - Style.cardStepHeight));

            // Body: the objective fills the space between header and action, centred both ways.
            TMP_Text objective = Label(rect, "Objective", Style.objectiveSize, Theme.Surface,
                FontStyles.Normal, VietFontRole.Body);
            objective.alignment = TextAlignmentOptions.Center;
            objective.enableWordWrapping = true;
            objective.lineSpacing = 0f;
            objective.enableAutoSizing = true;
            objective.fontSizeMax = Style.objectiveSize;
            objective.fontSizeMin = Style.captionSize - 4f;
            Inset(objective.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(pad, actionTop + gap), new Vector2(-pad, -headerBottom - gap));

            Image action = Shape(rect, "ActionSurface", Theme.Muted);
            Inset(action.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(pad, pad), new Vector2(-pad, actionTop));
            TMP_Text status = Label(rect, "Status", Style.captionSize, Theme.Surface, FontStyles.Bold);
            status.alignment = TextAlignmentOptions.Center;
            Inset(status.rectTransform, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(pad, pad), new Vector2(-pad, actionTop));
        }

        static void CreateConnector(RectTransform panel, int index)
        {
            float left = Style.cardLeft + Style.cardWidth + index * (Style.cardWidth + Style.cardGap);
            Image track = Shape(panel, $"LessonConnector{index + 1}", Theme.MapLockedBorder);
            float middle = (Style.cardBottom + Style.cardTop) * .5f;
            Anchor(track.rectTransform, new Vector2(left, middle - .006f), new Vector2(left + Style.cardGap, middle + .006f));
            Image arrow = Shape(panel, $"LessonArrow{index + 1}", Theme.MapLockedBorder, true);
            Place(arrow.rectTransform, new Vector2(left + Style.cardGap * .5f, middle), new Vector2(38f, 38f));
            TMP_Text mark = Label(arrow.transform, "Label", Style.bodySize, Theme.Surface, FontStyles.Bold);
            mark.text = "›";
            mark.alignment = TextAlignmentOptions.Center;
            Anchor(mark.rectTransform, Vector2.zero, Vector2.one);
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
            FontStyles style = FontStyles.Normal, VietFontRole? role = null)
        {
            RectTransform rect = Rect(parent, name);
            TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            if (role.HasValue) VietTypography.Apply(label, role.Value);
            else VietTypography.Apply(label);
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

        static void Inset(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
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
