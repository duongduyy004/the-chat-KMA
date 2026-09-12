using System.Collections.Generic;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class MapPresentationBuilder
    {
        static Sprite heartSprite;

        readonly struct Entry
        {
            public readonly SubjectId Subject;
            public readonly string Label;
            public readonly string Icon;
            public readonly Color Color;
            public readonly bool Available;

            public Entry(SubjectId subject, string label, string icon, Color color, bool available)
            {
                Subject = subject; Label = label; Icon = icon; Color = color; Available = available;
            }
        }

        static readonly Entry[] Entries =
        {
            new Entry(SubjectId.Sprint, "Chạy nước rút", ">>", new Color32(255, 89, 94, 255), true),
            new Entry(SubjectId.Endurance, "Chạy bền", "∞", new Color32(255, 202, 58, 255), true),
            new Entry(SubjectId.Volleyball, "Bóng chuyền", "◉", new Color32(138, 203, 136, 255), true),
            new Entry(SubjectId.Basketball, "Bóng rổ", "●", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.PingPong, "Bóng bàn", "◎", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.Badminton, "Cầu lông", "⌁", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.Football, "Bóng đá", "⬡", new Color32(226, 232, 240, 255), false),
        };

        public static void Build(MapScreen screen, GameSession session)
        {
            if (screen == null || screen.transform.Find("S5MapPresentation") != null) return;
            screen.SetBossUnlocked(session != null && session.BossUnlocked);
            foreach (Button button in screen.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
            UITheme theme = screen.Theme;
            Color card = theme == null ? Color.white : theme.Card;
            Color muted = theme == null ? new Color32(226, 232, 240, 255) : theme.Muted;
            Color mutedForeground = theme == null ? new Color32(71, 85, 105, 255) : theme.MutedForeground;
            Color border = theme == null ? Color.black : theme.Border;
            Color background = Color.Lerp(theme == null ? new Color32(25, 130, 196, 255) : theme.Background,
                new Color32(10, 48, 82, 255), .46f);

            RectTransform root = Rect(screen.transform, "S5MapPresentation");
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<Image>().color = background;
            RectTransform content = Rect(root, "Content");
            Stretch(content, new Vector2(56, 36), new Vector2(-56, -34));

            HeartBar hearts = Header(content, session, border);
            PinToTop((RectTransform)hearts.transform.parent, 72f);
            RectTransform grid = Rect(content, "SelectionGrid");
            Anchor(grid, new Vector2(0f, .42f), new Vector2(1f, .82f));
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<CenteredLastRowGridLayout>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.spacing = new Vector2(16, 16); gridLayout.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ResponsiveGridLayout>().Refresh();
            LayoutElement gridElement = grid.gameObject.AddComponent<LayoutElement>();
            gridElement.flexibleWidth = 1; gridElement.preferredHeight = 392;
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            var nodes = new List<MapNodeView>();
            foreach (Entry entry in Entries) nodes.Add(Card(grid, screen, entry, card, muted, mutedForeground, border));
            FutureRow(content, muted, mutedForeground, border);
            Anchor((RectTransform)content.Find("FutureRow"), new Vector2(.28f, .34f), new Vector2(.72f, .39f));
            ProgressSection(content, session, border);
            Anchor((RectTransform)content.Find("ProgressSection"), new Vector2(0f, .19f), new Vector2(1f, .30f));
            BossButton(content, screen, card, muted, mutedForeground, border);
            Anchor((RectTransform)content.Find("BossButton"), new Vector2(0f, .07f), new Vector2(1f, .15f));
            screen.BindPresentation(nodes.ToArray(), hearts, session);
            foreach (MapNodeView node in nodes)
                if (!node.IsComingSoon && !node.IsInteractable) node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
        }

        static HeartBar Header(Transform parent, GameSession session, Color border)
        {
            RectTransform header = Rect(parent, "Header");
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = false;
            LayoutElement headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.minHeight = 72;
            headerElement.preferredHeight = 72;
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
            Text title = LayoutLabel(heading, "Title", "CHỌN MÔN THI", 34,
                Color.white, TextAnchor.LowerLeft);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            Text subtitle = LayoutLabel(heading, "Subtitle", "Chọn thử thách tiếp theo", 16,
                new Color32(201, 226, 245, 255), TextAnchor.UpperLeft);
            subtitle.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
            RectTransform bar = Rect(header, "HeartBar");
            HorizontalLayoutGroup heartLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            heartLayout.spacing = 4; heartLayout.childAlignment = TextAnchor.MiddleRight;
            heartLayout.childControlWidth = true; heartLayout.childControlHeight = true;
            heartLayout.childForceExpandWidth = false; heartLayout.childForceExpandHeight = false;
            bar.gameObject.AddComponent<LayoutElement>().preferredWidth = 152;
            HeartBar hearts = bar.gameObject.AddComponent<HeartBar>();
            var slots = new Image[5];
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform slot = Rect(bar, "Heart" + (i + 1)); slots[i] = slot.gameObject.AddComponent<Image>();
                slots[i].sprite = HeartSprite();
                slots[i].preserveAspect = true;
                slot.gameObject.AddComponent<Outline>().effectColor = border;
                LayoutElement element = slot.gameObject.AddComponent<LayoutElement>(); element.preferredWidth = 24; element.preferredHeight = 23;
            }
            hearts.SetSlots(slots);
            int currentLives = session == null ? GameSession.MaxLives : session.Lives;
            hearts.SetHearts(currentLives);
            Text lives = LayoutLabel(header, "LivesLabel", "LƯỢT: " + currentLives + "/" + GameSession.MaxLives, 20,
                Color.white, TextAnchor.MiddleRight);
            lives.transform.parent.gameObject.AddComponent<LayoutElement>().preferredWidth = 82;
            return hearts;
        }

        static Button HeaderButton(Transform parent, string name, string label, Color border)
        {
            RectTransform root = Rect(parent, name);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color32(255, 249, 231, 255);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(2f, -2f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 54f;
            element.preferredHeight = 54f;
            Text(root, "Label", label, 38, new Color32(8, 35, 61, 255), TextAnchor.MiddleCenter);
            return button;
        }

        static MapNodeView Card(Transform parent, MapScreen screen, Entry entry, Color card, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, entry.Subject + "Node");
            Image image = root.gameObject.AddComponent<Image>(); image.color = entry.Available ? card : muted;
            Outline outline = root.gameObject.AddComponent<Outline>(); outline.effectColor = border; outline.effectDistance = new Vector2(2, -2);
            Shadow shadow = root.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .65f); shadow.effectDistance = new Vector2(6, -6);
            Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.colors = ButtonColors();
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12); layout.spacing = 5; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            RectTransform stripe = Rect(root, "HeaderStripe");
            stripe.gameObject.AddComponent<Image>().color = entry.Color;
            stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = 6;
            RectTransform cardHeader = Rect(root, "CardHeader");
            HorizontalLayoutGroup cardHeaderLayout = cardHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardHeaderLayout.spacing = 10; cardHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
            cardHeaderLayout.childControlWidth = true; cardHeaderLayout.childControlHeight = true;
            RectTransform icon = Rect(cardHeader, "SportIcon"); icon.gameObject.AddComponent<Image>().color = entry.Color;
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
            Text(icon, "Glyph", entry.Icon, 22, Color.black, TextAnchor.MiddleCenter);
            Text title = LayoutLabel(cardHeader, "Title", entry.Label, 28, entry.Available ? Color.black : foreground, TextAnchor.MiddleLeft);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            Text status = LayoutLabel(root, "Status", string.Empty, 18, entry.Available ? new Color32(12, 105, 94, 255) : foreground,
                TextAnchor.MiddleLeft);
            status.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            Text detail = LayoutLabel(root, "Detail", string.Empty, 19, foreground, TextAnchor.MiddleLeft);
            detail.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            Text action = Text(root, "ActionHint",
                entry.Available ? "CHẠM ĐỂ THI ĐẤU" : "SẮP RA MẮT", 16,
                entry.Available ? new Color32(8, 35, 61, 255) : foreground, TextAnchor.MiddleRight);
            action.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
            MapNodeView node = root.gameObject.AddComponent<MapNodeView>(); node.Bind(button, title, detail);
            node.BindStatusLabel(status);
            node.Configure(entry.Subject, entry.Label, !entry.Available, null, 5);
            if (entry.Available) button.onClick.AddListener(() => screen.SelectSubject(entry.Subject));
            else node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
            return node;
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

            FutureChip(row, "PushUpsChip", "Hít đất", muted, foreground, border);
            FutureChip(row, "RhythmChip", "Nhịp điệu", muted, foreground, border);
            FutureChip(row, "SwimmingChip", "Bơi lội", muted, foreground, border);
        }

        static void FutureChip(Transform parent, string name, string label, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, name);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = muted;
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            button.interactable = false;
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 168;
            element.preferredHeight = 34;
            Text(root, "Label", label, 16, foreground, TextAnchor.MiddleCenter);
        }

        static void ProgressSection(Transform parent, GameSession session, Color border)
        {
            var completed = 0;
            if (session != null)
            {
                foreach (Entry entry in Entries)
                    if (session.GetRecord(entry.Subject)?.Passed == true) completed++;
            }
            RectTransform section = Rect(parent, "ProgressSection");
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
            LayoutElement sectionElement = section.gameObject.AddComponent<LayoutElement>();
            sectionElement.preferredHeight = 64;
            sectionElement.flexibleHeight = 0;
            Text label = LayoutLabel(section, "ProgressLabel", "TIẾN ĐỘ   " + completed + "/" + Entries.Length, 18,
                Color.white, TextAnchor.MiddleLeft);
            label.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            RectTransform track = Rect(section, "ProgressTrack");
            Image trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = new Color32(255, 255, 255, 70);
            track.gameObject.AddComponent<Outline>().effectColor = border;
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
            RectTransform fill = Rect(track, "Fill");
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2((float)completed / Entries.Length, 1f);
            fill.offsetMin = new Vector2(2f, 2f); fill.offsetMax = new Vector2(-2f, -2f);
            fill.gameObject.AddComponent<Image>().color = new Color32(255, 202, 58, 255);
            Text hint = LayoutLabel(section, "UnlockHint", "Hoàn thành môn để mở thử thách cuối", 13,
                new Color32(201, 226, 245, 255), TextAnchor.MiddleLeft);
            hint.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        static void BossButton(Transform parent, MapScreen screen, Color card, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, "BossButton"); Image image = root.gameObject.AddComponent<Image>();
            image.color = screen.BossUnlocked ? card : muted; root.gameObject.AddComponent<Outline>().effectColor = border;
            Shadow shadow = root.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .65f); shadow.effectDistance = new Vector2(6, -6);
            Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = screen.BossUnlocked;
            button.colors = ButtonColors();
            if (screen.BossUnlocked) button.onClick.AddListener(screen.SelectBoss);
            LayoutElement bossElement = root.gameObject.AddComponent<LayoutElement>();
            bossElement.preferredHeight = 52;
            bossElement.flexibleHeight = 0;
            Text(root, "Label", screen.BossUnlocked ? "THỬ THÁCH CUỐI" : "HOÀN THÀNH CÁC MÔN ĐỂ MỞ", 22,
                screen.BossUnlocked ? Color.black : foreground, TextAnchor.MiddleCenter);
        }

        static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 236, 170, 255);
            colors.pressedColor = new Color32(255, 202, 58, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, .58f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .08f;
            return colors;
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
            texture.Apply(false, true);
            heartSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            heartSprite.name = "RuntimeHeartIcon";
            heartSprite.hideFlags = HideFlags.HideAndDontSave;
            return heartSprite;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)); gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        static Text Text(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            RectTransform rect = Rect(parent, name); Stretch(rect, Vector2.zero, Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static Text LayoutLabel(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            return Text(Rect(parent, name + "Container"), name, value, size, color, alignment);
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
        }

        static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
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

    public sealed class ResponsiveGridLayout : MonoBehaviour
    {
        GridLayoutGroup grid;
        RectTransform rect;

        void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rect = GetComponent<RectTransform>();
        }

        void OnEnable() => Refresh();
        void OnRectTransformDimensionsChange() => Refresh();

        public void Refresh()
        {
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            if (rect == null) rect = GetComponent<RectTransform>();
            if (grid == null || rect == null) return;
            int columns = Mathf.Max(1, grid.constraintCount);
            float width = rect.rect.width;
            if (width <= 0f) width = Mathf.Max(1f, Screen.width - 144f);
            float cellWidth = Mathf.Max(1f, (width - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1)) / columns);
            float cellHeight = Mathf.Clamp(cellWidth * 0.58f, 168f, 188f);
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            LayoutElement element = GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredHeight = cellHeight * 2f + grid.spacing.y;
                element.flexibleHeight = 0f;
            }
        }
    }

    public sealed class CenteredLastRowGridLayout : GridLayoutGroup
    {
        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();
            if (constraint != Constraint.FixedColumnCount || constraintCount < 1)
                return;

            int finalRowCount = rectChildren.Count % constraintCount;
            if (finalRowCount == 0)
                return;

            float rowWidth = finalRowCount * cellSize.x + (finalRowCount - 1) * spacing.x;
            float startX = GetStartOffset(0, rowWidth);
            int firstChild = rectChildren.Count - finalRowCount;
            for (int index = 0; index < finalRowCount; index++)
                SetChildAlongAxis(rectChildren[firstChild + index], 0,
                    startX + index * (cellSize.x + spacing.x), cellSize.x);
        }
    }
}
