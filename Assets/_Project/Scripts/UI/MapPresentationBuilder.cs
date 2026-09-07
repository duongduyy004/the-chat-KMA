using System.Collections.Generic;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class MapPresentationBuilder
    {
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
            new Entry(SubjectId.Sprint, "Chạy nước rút", new Color32(255, 89, 94, 255), true),
            new Entry(SubjectId.Endurance, "Chạy bền", new Color32(255, 202, 58, 255), true),
            new Entry(SubjectId.Volleyball, "Bóng chuyền", new Color32(138, 203, 136, 255), true),
            new Entry(SubjectId.Basketball, "Bóng rổ", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.PingPong, "Bóng bàn", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.Badminton, "Cầu lông", new Color32(226, 232, 240, 255), false),
            new Entry(SubjectId.Football, "Bóng đá", new Color32(226, 232, 240, 255), false),
        };

        public static void Build(MapScreen screen, GameSession session)
        {
            if (screen == null || screen.transform.Find("S5MapPresentation") != null) return;
            foreach (Button button in screen.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
            UITheme theme = screen.Theme;
            Color card = theme == null ? Color.white : theme.Card;
            Color muted = theme == null ? new Color32(226, 232, 240, 255) : theme.Muted;
            Color mutedForeground = theme == null ? new Color32(71, 85, 105, 255) : theme.MutedForeground;
            Color border = theme == null ? Color.black : theme.Border;
            Color background = theme == null ? new Color32(25, 130, 196, 255) : theme.Background;

            RectTransform root = Rect(screen.transform, "S5MapPresentation");
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<Image>().color = background;
            RectTransform content = Rect(root, "Content");
            Stretch(content, new Vector2(72, 48), new Vector2(-72, -48));
            VerticalLayoutGroup vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 20; vertical.childControlWidth = true; vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true; vertical.childForceExpandHeight = false;

            HeartBar hearts = Header(content, session, border);
            RectTransform grid = Rect(content, "SelectionGrid");
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.spacing = new Vector2(16, 16); gridLayout.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ResponsiveGridLayout>().Refresh();
            LayoutElement gridElement = grid.gameObject.AddComponent<LayoutElement>();
            gridElement.flexibleWidth = 1; gridElement.preferredHeight = 280;
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            var nodes = new List<MapNodeView>();
            foreach (Entry entry in Entries) nodes.Add(Card(grid, screen, entry, card, muted, mutedForeground, border));
            FutureRow(content, muted, mutedForeground, border);
            BossButton(content, screen, card, muted, mutedForeground, border);
            screen.BindPresentation(nodes.ToArray(), hearts, session);
            foreach (MapNodeView node in nodes)
                if (!node.IsComingSoon && !node.IsInteractable) node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
        }

        static HeartBar Header(Transform parent, GameSession session, Color border)
        {
            RectTransform header = Rect(parent, "Header");
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = false;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            Text title = LayoutLabel(header, "Title", "CHỌN MÔN THI", 32, Color.black, TextAnchor.MiddleLeft);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            RectTransform bar = Rect(header, "HeartBar");
            HorizontalLayoutGroup heartLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            heartLayout.spacing = 6; heartLayout.childAlignment = TextAnchor.MiddleRight;
            heartLayout.childControlWidth = true; heartLayout.childControlHeight = true;
            bar.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;
            HeartBar hearts = bar.gameObject.AddComponent<HeartBar>();
            var slots = new Image[5];
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform slot = Rect(bar, "Heart" + (i + 1)); slots[i] = slot.gameObject.AddComponent<Image>();
                slot.gameObject.AddComponent<Outline>().effectColor = border;
                LayoutElement element = slot.gameObject.AddComponent<LayoutElement>(); element.preferredWidth = 22; element.preferredHeight = 22;
            }
            hearts.SetSlots(slots);
            Text lives = LayoutLabel(header, "LivesLabel", "LƯỢT: " + (session == null ? 5 : session.Lives) + "/5", 18, Color.black, TextAnchor.MiddleRight);
            lives.transform.parent.gameObject.AddComponent<LayoutElement>().preferredWidth = 100;
            return hearts;
        }

        static MapNodeView Card(Transform parent, MapScreen screen, Entry entry, Color card, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, entry.Subject + "Node");
            Image image = root.gameObject.AddComponent<Image>(); image.color = entry.Available ? card : muted;
            Outline outline = root.gameObject.AddComponent<Outline>(); outline.effectColor = border; outline.effectDistance = new Vector2(2, -2);
            Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10); layout.spacing = 4; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            RectTransform stripe = Rect(root, "Stripe"); stripe.gameObject.AddComponent<Image>().color = entry.Color;
            stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;
            Text title = LayoutLabel(root, "Title", entry.Label, 20, entry.Available ? Color.black : foreground, TextAnchor.MiddleCenter);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            Text detail = LayoutLabel(root, "Detail", string.Empty, 14, foreground, TextAnchor.MiddleCenter);
            detail.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            MapNodeView node = root.gameObject.AddComponent<MapNodeView>(); node.Bind(button, title, detail);
            node.Configure(entry.Subject, entry.Label, !entry.Available, null, 5);
            if (entry.Available) button.onClick.AddListener(() => screen.SelectSubject(entry.Subject));
            else node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
            return node;
        }

        static void FutureRow(Transform parent, Color muted, Color foreground, Color border)
        {
            RectTransform row = Rect(parent, "FutureRow");
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
            foreach (string label in new[] { "Hít đất", "Nhịp điệu", "Bơi lội" })
            {
                RectTransform chip = Rect(row, label + "Chip"); Image image = chip.gameObject.AddComponent<Image>(); image.color = muted;
                chip.gameObject.AddComponent<Outline>().effectColor = border;
                Button button = chip.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = false;
                Text(chip, "Label", label + " — ĐANG PHÁT TRIỂN", 14, foreground, TextAnchor.MiddleCenter);
            }
        }

        static void BossButton(Transform parent, MapScreen screen, Color card, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, "BossButton"); Image image = root.gameObject.AddComponent<Image>();
            image.color = screen.BossUnlocked ? card : muted; root.gameObject.AddComponent<Outline>().effectColor = border;
            Button button = root.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = screen.BossUnlocked;
            if (screen.BossUnlocked) button.onClick.AddListener(screen.SelectBoss);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
            Text(root, "Label", screen.BossUnlocked ? "THỬ THÁCH CUỐI" : "HOÀN THÀNH CÁC MÔN ĐỂ MỞ", 18,
                screen.BossUnlocked ? Color.black : foreground, TextAnchor.MiddleCenter);
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
            float cellHeight = Mathf.Clamp(cellWidth * 0.6f, 96f, 160f);
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            LayoutElement element = GetComponent<LayoutElement>();
            if (element != null) element.preferredHeight = cellHeight * 2f + grid.spacing.y;
        }
    }
}
