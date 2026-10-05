using System.Collections.Generic;
using KMA.Gameplay;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class MapPresentationBuilder
    {
        static Sprite heartSprite;
        static Sprite roundedRectSprite;
        static Sprite lockSprite;

        readonly struct Entry
        {
            public readonly SubjectId Subject;
            public readonly string Label;
            public readonly Color Color;
            public readonly bool Available;

            public Entry(SubjectId subject, string label, Color color, bool available)
            {
                Subject = subject; Label = label; Color = color; Available = available;
            }
        }

        static readonly Entry[] Entries =
        {
            new Entry(SubjectId.Sprint, "Chạy nước rút", UITheme.Shared.LessonJourney.sprint, true),
            new Entry(SubjectId.Volleyball, "Bóng chuyền", UITheme.Shared.LessonJourney.volleyball, true),
            new Entry(SubjectId.Football, "Bóng đá", UITheme.Shared.LessonJourney.football, true),
        };

        public static void Build(MapScreen screen, GameSession session, Sprite sharedBackground = null)
        {
            if (screen == null) return;
            var existing = screen.transform.Find("S5MapPresentation");
            if (existing != null)
            {
                var existingNodes = existing.GetComponentsInChildren<MapNodeView>(true);
                var existingHearts = existing.GetComponentInChildren<HeartBar>(true);
                ApplySharedBackground(existing, sharedBackground);
                Transform oldFutureRow = existing.Find("Content/FutureRow");
                if (oldFutureRow != null) oldFutureRow.gameObject.SetActive(false);
                var lessonList = existing.GetComponentInChildren<JourneyLessonList>(true) ??
                    JourneyLessonList.Create(existing.Find("Content"));
                EnsureCourseSummary(screen, existing.Find("Content"));
                screen.BindPresentation(existingNodes, existingHearts, session ?? new GameSession(), lessonList);
                screen.BindBudgetLabel(existing.Find("Content/Header/LivesPanel/LivesLabelContainer/LivesLabel")
                    ?.GetComponent<TMP_Text>());
                screen.RefreshJourney(session ?? new GameSession());
                ConfigureJourneyPath(existing.Find("Content/SelectionGrid"), existingNodes);
                var back = existing.Find("Content/Header/BackButton")?.GetComponent<Button>();
                if (back != null)
                {
                    back.onClick.RemoveAllListeners();
                    back.onClick.AddListener(() => KMA.Gameplay.Core.SceneRouter.Instance?.RouteToMenu());
                }
                foreach (var node in existingNodes)
                {
                    if (node.IsComingSoon) continue;
                    var button = node.GetComponent<Button>();
                    if (button == null) continue;
                    button.onClick.RemoveAllListeners();
                    var subject = node.SubjectId;
                    button.onClick.AddListener(() => screen.SelectSubject(subject));
                }
                return;
            }
            foreach (Button button in screen.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
            UITheme theme = screen.Theme;
            Color border = theme.Border;
            Color background = Color.Lerp(theme.Background,
                UITheme.Shared.MapBackgroundTint, .46f);

            RectTransform root = Rect(screen.transform, "S5MapPresentation");
            Stretch(root, Vector2.zero, Vector2.zero);
            Image backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = sharedBackground != null ? Color.white : background;
            backdrop.sprite = sharedBackground;
            backdrop.preserveAspect = false;
            if (sharedBackground != null) AddBackdropShade(root);
            RectTransform content = Rect(root, "Content");
            Stretch(content, new Vector2(64, 40), new Vector2(-64, -40));

            HeartBar hearts = Header(content, session, border);
            PinToTop((RectTransform)hearts.transform.parent.parent, 84f);
            RectTransform grid = Rect(content, "SelectionGrid");
            Anchor(grid, new Vector2(0f, .37f), new Vector2(1f, .84f));
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.spacing = new Vector2(16, 16); gridLayout.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ResponsiveGridLayout>().Refresh();
            LayoutElement gridElement = grid.gameObject.AddComponent<LayoutElement>();
            gridElement.flexibleWidth = 1; gridElement.preferredHeight = 392;
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            var nodes = new List<MapNodeView>();
            for (int i = 0; i < Entries.Length; i++)
                nodes.Add(MapStopBuilder.Build(grid, screen, Entries[i].Subject, Entries[i].Label,
                    Entries[i].Color, i + 1));
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            JourneyLessonList lessons = JourneyLessonList.Create(content);
            EnsureCourseSummary(screen, content);
            screen.BindPresentation(nodes.ToArray(), hearts, session ?? new GameSession(), lessons);
            screen.BindBudgetLabel(content.Find("Header/LivesPanel/LivesLabelContainer/LivesLabel")
                ?.GetComponent<TMP_Text>());
            screen.RefreshJourney(session ?? new GameSession());
            ConfigureJourneyPath(grid, nodes.ToArray());
        }

        static void ApplySharedBackground(Transform root, Sprite sharedBackground)
        {
            if (root == null || sharedBackground == null) return;
            Image backdrop = root.GetComponent<Image>();
            if (backdrop == null) backdrop = root.gameObject.AddComponent<Image>();
            backdrop.sprite = sharedBackground;
            backdrop.color = Color.white;
            backdrop.preserveAspect = false;
            AddBackdropShade(root);
        }

        static void AddBackdropShade(Transform root)
        {
            if (root.Find("BackgroundShade") != null) return;
            RectTransform shade = Rect(root, "BackgroundShade");
            Stretch(shade, Vector2.zero, Vector2.zero);
            Image image = shade.gameObject.AddComponent<Image>();
            image.color = new Color(HomeMenuStyle.Navy.r, HomeMenuStyle.Navy.g, HomeMenuStyle.Navy.b, .42f);
            image.raycastTarget = false;
            shade.SetAsFirstSibling();
        }

        static void ConfigureJourneyPath(Transform selectionGrid, MapNodeView[] nodes)
        {
            if (selectionGrid == null) return;
            Anchor((RectTransform)selectionGrid, UITheme.Shared.LessonJourney.mapAnchorMin,
                UITheme.Shared.LessonJourney.mapAnchorMax);
            GridLayoutGroup grid = selectionGrid.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.enabled = false;
            ResponsiveGridLayout responsive = selectionGrid.GetComponent<ResponsiveGridLayout>();
            if (responsive != null) responsive.enabled = false;
            MapJourneyPathLayout path = selectionGrid.GetComponent<MapJourneyPathLayout>();
            if (path == null) path = selectionGrid.gameObject.AddComponent<MapJourneyPathLayout>();
            path.Configure(nodes);
        }

        static JourneyCourseSummary EnsureCourseSummary(MapScreen screen, Transform parent)
        {
            JourneyCourseSummary summary = screen.GetComponentInChildren<JourneyCourseSummary>(true);
            if (summary != null) return summary;
            var panel = new GameObject("JourneyCourseSummary", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = UITheme.Shared.LessonJourney.summaryAnchorMin;
            rect.anchorMax = UITheme.Shared.LessonJourney.summaryAnchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = MinigameUiTheme.WithAlpha(UITheme.Shared.Surface, .98f);
            panel.GetComponent<Image>().raycastTarget = false;
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = UITheme.Shared.Accent;
            outline.effectDistance = new Vector2(1f, -1f);
            var labelObject = new GameObject("SummaryText", typeof(RectTransform));
            labelObject.transform.SetParent(panel.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 2f);
            labelRect.offsetMax = new Vector2(-16f, -2f);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            UiKit.StyleLabel(label, UITheme.Shared.LessonJourney.captionSize, UITheme.Shared.TextPrimary);
            label.fontSize = UITheme.Shared.LessonJourney.captionSize;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            JourneyCourseSummary view = panel.AddComponent<JourneyCourseSummary>();
            view.Configure(label);
            view.Hide();
            return view;
        }

        static HeartBar Header(Transform parent, GameSession session, Color border)
        {
            RectTransform header = Rect(parent, "Header");
            Image headerSurface = header.gameObject.AddComponent<Image>();
            headerSurface.color = MinigameUiTheme.WithAlpha(UITheme.Shared.Surface, 178f / 255f);
            headerSurface.raycastTarget = false;
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 16, 8, 10);
            layout.spacing = 16; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = false;
            LayoutElement headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.minHeight = 84;
            headerElement.preferredHeight = 84;
            headerElement.flexibleHeight = 0;
            Button back = HeaderButton(header, "BackButton", "‹", border);
            back.onClick.AddListener(() => KMA.Gameplay.Core.SceneRouter.Instance?.RouteToMenu());
            RectTransform heading = Rect(header, "Heading");
            heading.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            VerticalLayoutGroup headingLayout = heading.gameObject.AddComponent<VerticalLayoutGroup>();
            headingLayout.spacing = 0;
            headingLayout.childControlWidth = true;
            headingLayout.childControlHeight = true;
            headingLayout.childForceExpandHeight = false;
            TMP_Text title = LayoutLabel(heading, "Title", "CHỌN MÔN THI", 40,
                Color.white, TextAnchor.MiddleLeft);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            RectTransform livesPanel = Rect(header, "LivesPanel");
            Image livesSurface = livesPanel.gameObject.AddComponent<Image>();
            livesSurface.color = UITheme.Shared.MapLivesSurface;
            UseRoundedSurface(livesSurface);
            Outline livesOutline = livesPanel.gameObject.AddComponent<Outline>();
            livesOutline.effectColor = MinigameUiTheme.WithAlpha(UITheme.Shared.Accent, 210f / 255f);
            livesOutline.effectDistance = new Vector2(UITheme.Shared.BorderWidth * .5f, -UITheme.Shared.BorderWidth * .5f);
            HorizontalLayoutGroup livesLayout = livesPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            // The top padding leaves room for the life regen countdown above the hearts.
            livesLayout.padding = new RectOffset(14, 14, 30, 4);
            livesLayout.spacing = 12;
            livesLayout.childAlignment = TextAnchor.MiddleCenter;
            livesLayout.childControlWidth = true;
            livesLayout.childControlHeight = true;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            LayoutElement livesElement = livesPanel.gameObject.AddComponent<LayoutElement>();
            livesElement.preferredWidth = 470;
            livesElement.preferredHeight = 66;
            RectTransform bar = Rect(livesPanel, "HeartBar");
            HorizontalLayoutGroup heartLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            heartLayout.spacing = 6; heartLayout.childAlignment = TextAnchor.MiddleCenter;
            heartLayout.childControlWidth = true; heartLayout.childControlHeight = true;
            heartLayout.childForceExpandWidth = false; heartLayout.childForceExpandHeight = false;
            bar.gameObject.AddComponent<LayoutElement>().preferredWidth = 184;
            HeartBar hearts = bar.gameObject.AddComponent<HeartBar>();
            var slots = new Image[5];
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform slot = Rect(bar, "Heart" + (i + 1)); slots[i] = slot.gameObject.AddComponent<Image>();
                slots[i].sprite = HeartSprite();
                slots[i].preserveAspect = true;
                slot.gameObject.AddComponent<Outline>().effectColor = border;
                LayoutElement element = slot.gameObject.AddComponent<LayoutElement>(); element.preferredWidth = 32; element.preferredHeight = 30;
            }
            hearts.SetSlots(slots);
            int currentLives = session == null ? GameSession.MaxLives : session.Lives;
            hearts.SetHearts(currentLives);
            TMP_Text lives = LayoutLabel(livesPanel, "LivesLabel", $"Lượt thi: {currentLives}/{GameSession.MaxLives}", 24,
                Color.white, TextAnchor.MiddleRight);
            lives.enableWordWrapping = false;
            LayoutElement livesLabelLayout = lives.transform.parent.gameObject.AddComponent<LayoutElement>();
            livesLabelLayout.preferredWidth = 200;
            livesLabelLayout.preferredHeight = 44;
            RectTransform divider = Rect(header, "Divider");
            LayoutElement dividerLayout = divider.gameObject.AddComponent<LayoutElement>();
            dividerLayout.ignoreLayout = true;
            divider.anchorMin = Vector2.zero;
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(.5f, 0f);
            divider.anchoredPosition = Vector2.zero;
            divider.sizeDelta = new Vector2(0f, 4f);
            divider.gameObject.AddComponent<Image>().color = new Color32(255, 255, 255, 36);
            return hearts;
        }

        static Button HeaderButton(Transform parent, string name, string label, Color border)
        {
            RectTransform root = Rect(parent, name);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = UITheme.Shared.TextPrimary;
            UseRoundedSurface(image);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(UITheme.Shared.BorderWidth * .5f, -UITheme.Shared.BorderWidth * .5f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 60f;
            element.preferredHeight = 60f;
            TextTmp(root, "Label", label, 40, UITheme.Shared.Surface, TextAnchor.MiddleCenter);
            return button;
        }

        static void FutureRow(Transform parent, Color muted, Color foreground, Color border)
        {
            RectTransform row = Rect(parent, "FutureRow");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            LayoutElement rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 38;
            rowElement.flexibleHeight = 0;

            TMP_Text upcoming = LayoutLabel(row, "UpcomingLabel", "SẮP RA MẮT", 22,
                UITheme.Shared.MapHint, TextAnchor.MiddleRight);
            LayoutElement upcomingLayout = upcoming.transform.parent.gameObject.AddComponent<LayoutElement>();
            upcomingLayout.preferredWidth = 240;
            upcomingLayout.preferredHeight = 46;
            FutureChip(row, "PushUpsChip", "Hít đất", muted, foreground, border);
        }

        static void FutureChip(Transform parent, string name, string label, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, name);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = muted;
            UseRoundedSurface(image);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            image.raycastTarget = false;
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 168;
            element.preferredHeight = 46;
            TextTmp(root, "Label", label, 22, foreground, TextAnchor.MiddleCenter);
        }

        internal static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = UITheme.Shared.MapButtonHighlight;
            colors.pressedColor = UITheme.Shared.Accent;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, .58f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = UITheme.Shared.Motion.buttonFade;
            return colors;
        }

        // Material Symbols glyphs (Apache 2.0), imported from Resources/Icons; see Art/Icons/Source~.
        internal static Sprite SportIconSprite(SubjectId subject) =>
            Resources.Load<Sprite>("Icons/SportIcon_" + subject);

        internal static void UseRoundedSurface(Image image)
        {
            float radius = UITheme.Shared.CornerRadius * (2f / 3f);
            image.sprite = radius > 0f ? RoundedRectSprite() : null;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = radius > 0f ? 16f / radius : 1f;
        }

        static Sprite RoundedRectSprite()
        {
            if (roundedRectSprite != null)
                return roundedRectSprite;

            const int size = 64;
            const int radius = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedRect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int nearestX = Mathf.Clamp(x, radius, size - radius - 1);
                int nearestY = Mathf.Clamp(y, radius, size - radius - 1);
                int dx = x - nearestX;
                int dy = y - nearestY;
                bool inside = dx * dx + dy * dy <= radius * radius;
                pixels[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            roundedRectSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedRectSprite.name = "RuntimeRoundedRect";
            roundedRectSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedRectSprite;
        }

        internal static Sprite LockSprite()
        {
            if (lockSprite != null)
                return lockSprite;

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeLockIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            DrawRing(pixels, size, 48, 59, 23, 8);
            for (int y = 15; y <= 57; y++)
            for (int x = 19; x <= 77; x++)
                SetIconPixel(pixels, size, x, y);
            for (int y = 29; y <= 50; y++)
            for (int x = 31; x <= 65; x++)
                pixels[y * size + x] = new Color32(255, 255, 255, 0);
            DrawCircle(pixels, size, 48, 37, 6);
            DrawLine(pixels, size, 48, 35, 48, 24, 5);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            lockSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            lockSprite.name = "RuntimeLockIcon";
            lockSprite.hideFlags = HideFlags.HideAndDontSave;
            return lockSprite;
        }

        internal static void DrawCircle(Color32[] pixels, int size, int centerX, int centerY, int radius)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
                if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radiusSquared)
                    SetIconPixel(pixels, size, x, y);
        }

        static void DrawRing(Color32[] pixels, int size, int centerX, int centerY, int radius, int thickness)
        {
            int outer = radius * radius;
            int innerRadius = radius - thickness;
            int inner = innerRadius * innerRadius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int distance = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                if (distance <= outer && distance >= inner)
                    SetIconPixel(pixels, size, x, y);
            }
        }

        internal static void DrawLine(Color32[] pixels, int size, int startX, int startY, int endX, int endY, int thickness)
        {
            int steps = Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY));
            if (steps == 0)
            {
                DrawCircle(pixels, size, startX, startY, Mathf.Max(1, thickness / 2));
                return;
            }
            for (int index = 0; index <= steps; index++)
            {
                float t = (float)index / steps;
                DrawCircle(pixels, size,
                    Mathf.RoundToInt(Mathf.Lerp(startX, endX, t)),
                    Mathf.RoundToInt(Mathf.Lerp(startY, endY, t)),
                    Mathf.Max(1, thickness / 2));
            }
        }

        internal static void SetIconPixel(Color32[] pixels, int size, int x, int y)
        {
            if (x < 0 || x >= size || y < 0 || y >= size)
                return;
            pixels[y * size + x] = new Color32(255, 255, 255, 255);
        }

        static Sprite HeartSprite()
        {
            if (heartSprite != null)
                return heartSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeHeartIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                float px = ((x + .5f) / size * 2f - 1f) * 1.22f;
                float py = ((y + .5f) / size * 2f - 1f) * 1.22f;
                py += .12f;
                float square = px * px + py * py - 1f;
                bool inside = square * square * square - px * px * py * py * py <= 0f;
                pixels[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            heartSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            heartSprite.name = "RuntimeHeartIcon";
            heartSprite.hideFlags = HideFlags.HideAndDontSave;
            return heartSprite;
        }

        internal static RectTransform Rect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)); gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        internal static TMP_Text TextTmp(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            RectTransform rect = Rect(parent, name); Stretch(rect, Vector2.zero, Vector2.zero);
            TextAlignmentOptions tmpAlignment = alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            UiKit.StyleLabel(text, size, color);
            text.text = VietText.Fix(value);
            VietTypography.Apply(text);
            text.alignment = tmpAlignment;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            UiKit.FitLabel(text, size);
            return text;
        }

        internal static TMP_Text LayoutLabel(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            return TextTmp(Rect(parent, name + "Container"), name, value, size, color, alignment);
        }

        internal static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
        }

        internal static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void PinToTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }
    }

}
