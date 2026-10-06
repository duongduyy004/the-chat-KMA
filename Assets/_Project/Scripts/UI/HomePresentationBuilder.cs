using System;
using System.Linq;
using System.Collections.Generic;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class HomePresentationBuilder
    {
        static Sprite disc;
        static readonly Dictionary<(float, float, int, int, bool), Sprite> SlantSprites = new Dictionary<(float, float, int, int, bool), Sprite>();

        public static void Build(MainMenuScreen screen)
        {
            if (screen == null || screen.transform.Find("HomeMenuLayout") != null) return;
            var veil = Rect(screen.transform, "HomeMenuVeil");
            veil.anchorMin = Vector2.zero;
            veil.anchorMax = new Vector2(.55f, 1f);
            veil.offsetMin = veil.offsetMax = Vector2.zero;
            var veilImage = veil.gameObject.AddComponent<Image>();
            veilImage.sprite = GradientSprite();
            veilImage.raycastTarget = false;
            veil.SetAsFirstSibling();

            var layout = Rect(screen.transform, "HomeMenuLayout");
            layout.anchorMin = layout.anchorMax = new Vector2(.05f, .46f);
            layout.pivot = new Vector2(0f, .5f);
            layout.sizeDelta = new Vector2(HomeMenuStyle.PanelWidth, HomeMenuStyle.PanelHeight);
            layout.gameObject.AddComponent<HomeMenuResponsive>();
            var oldLogo = screen.transform.Find("HomeLogo");
            if (oldLogo != null) oldLogo.gameObject.SetActive(false);
            var oldTitle = screen.transform.Find("HomeTitle");
            if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            Badge(layout);
            Title(layout);
            Action(screen, layout, "NEW GAMEButton", "CHƠI MỚI", 0, HomeMenuButton.Kind.NewGame);
            Action(screen, layout, "CONTINUEButton", "TIẾP TỤC", 1, HomeMenuButton.Kind.Continue);
            Action(screen, layout, "SETTINGSButton", "CÀI ĐẶT", 2, HomeMenuButton.Kind.Secondary);
            Action(screen, layout, "QUITButton", "THOÁT", 3, HomeMenuButton.Kind.Exit);
        }

        static void Badge(RectTransform parent)
        {
            var root = CreateBadge(parent, "SportBadge");
            Place(root, new Vector2(85f, 226f), new Vector2(132f, 132f));
        }

        // Splash and menu share the same badge sprites, rim and shine component.
        public static RectTransform CreateBadge(RectTransform parent, string name)
        {
            var root = Rect(parent, name);
            root.sizeDelta = Vector2.one * HomeMenuStyle.BadgeSize;
            Disc(root, "BadgeShadow", new Vector2(3f, -4f), 132f, new Color(0f, .04f, .1f, .35f));
            Disc(root, "WhiteRim", Vector2.zero, 132f, HomeMenuStyle.White);
            Disc(root, "NavyFace", Vector2.zero, 124f, HomeMenuStyle.Navy);
            Disc(root, "Track", new Vector2(0f, -19f), 78f, HomeMenuStyle.Red);
            var star = Rect(root, "Star");
            Place(star, new Vector2(0f, 31f), new Vector2(42f, 42f));
            var starImage = star.gameObject.AddComponent<Image>();
            starImage.sprite = HomeMenuIcons.Get(HomeMenuIcons.Shape.Star);
            starImage.color = HomeMenuStyle.Gold;
            starImage.raycastTarget = false;
            for (int i = 0; i < 3; i++)
            {
                var lane = Rect(root, "Lane" + i);
                Place(lane, new Vector2(-16f + i * 16f, -22f), new Vector2(3f, 44f));
                lane.localRotation = Quaternion.Euler(0f, 0f, -32f);
                var laneImage = lane.gameObject.AddComponent<Image>();
                laneImage.color = HomeMenuStyle.White;
                laneImage.raycastTarget = false;
            }
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 4; i++)
                {
                    var leaf = Rect(root, "Laurel" + side + i);
                    Place(leaf, new Vector2(side * (47f + i * 3f), -23f + i * 17f),
                        new Vector2(5f, 22f));
                    leaf.localRotation = Quaternion.Euler(0f, 0f, side * (32f - i * 10f));
                    var leafImage = leaf.gameObject.AddComponent<Image>();
                    leafImage.color = HomeMenuStyle.Grass;
                    leafImage.raycastTarget = false;
                }
            root.gameObject.AddComponent<HomeBadgeShine>();
            return root;
        }

        static void Title(RectTransform parent)
        {
            var top = Label(parent, "TitleTop", "THỂ CHẤT", HomeMenuStyle.TitleTopSize, HomeMenuStyle.Gold);
            Place(top.rectTransform, new Vector2(336f, 292f), new Vector2(340f, 66f));
            StyleTitle(top);
            var kma = Label(parent, "TitleKMA", "KMA", HomeMenuStyle.TitleKmaSize, HomeMenuStyle.Gold);
            Place(kma.rectTransform, new Vector2(320f, 207f), new Vector2(380f, 115f));
            StyleTitle(kma);
            for (int i = 0; i < 3; i++)
            {
                var line = Rect(parent, "TrackLine" + i);
                Place(line, new Vector2(222f + i * 20f, 132f - i * 7f), new Vector2(195f - i * 26f, 3f));
                line.localRotation = Quaternion.Euler(0f, 0f, UITheme.Shared.Menu.titleAngle);
                line.gameObject.AddComponent<Image>().color = HomeMenuStyle.Gold;
            }
        }

        static void StyleTitle(TMP_Text text)
        {
            VietTypography.Apply(text, VietFontRole.Title);
            text.fontStyle = FontStyles.Italic;
            text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, UITheme.Shared.Menu.titleAngle);
        }

        static void Action(Component screen, RectTransform parent, string name,
            string caption, int index, HomeMenuButton.Kind kind)
        {
            var button = screen.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, name, StringComparison.Ordinal));
            if (button == null) return;
            StyleButton(button, parent, caption, kind, new Vector2(0f, .5f),
                new Vector2(235f, 13f - index * HomeMenuStyle.ButtonStep),
                new Vector2(HomeMenuStyle.ButtonWidth, HomeMenuStyle.ButtonHeight));
        }

        // Restyles a scene-authored Button as a slanted menu button; shared by Home and Game Over.
        public static HomeMenuButton StyleButton(Button button, RectTransform parent, string caption,
            HomeMenuButton.Kind kind, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)button.transform;
            rect.SetParent(parent, false);
            Place(rect, position, size);
            rect.anchorMin = rect.anchorMax = anchor;
            var border = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            border.sprite = SlantSprite(true);
            border.type = Image.Type.Simple;
            border.color = HomeMenuStyle.White;
            var oldOutline = button.GetComponent<Outline>();
            if (oldOutline != null) oldOutline.enabled = false;
            var fill = Rect(rect, "Fill").gameObject.AddComponent<Image>();
            float stroke = UITheme.Shared.BorderWidth * .5f;
            Stretch(fill.rectTransform, Vector2.one * stroke, -Vector2.one * stroke);
            fill.sprite = SlantSprite(false);
            fill.raycastTarget = false;
            var label = UiKit.EnsureTmpLabel(rect, 22, HomeMenuStyle.White);
            label.text = VietText.Fix(caption);
            label.fontStyle = FontStyles.Normal;
            label.alignment = TextAlignmentOptions.Left;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18;
            label.fontSizeMax = label.fontSize;
            Stretch(label.rectTransform, new Vector2(55f, 0f), new Vector2(-30f, 0f));
            label.transform.SetAsLastSibling();
            var iconRect = Rect(rect, "Icon");
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
            iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.sizeDelta = new Vector2(23f, 23f);
            iconRect.anchoredPosition = new Vector2(35f, 0f);
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = HomeMenuIcons.Get(kind == HomeMenuButton.Kind.NewGame ? HomeMenuIcons.Shape.Play :
                kind == HomeMenuButton.Kind.Continue ? HomeMenuIcons.Shape.Continue :
                kind == HomeMenuButton.Kind.Exit ? HomeMenuIcons.Shape.Power : HomeMenuIcons.Shape.Gear);
            iconImage.color = HomeMenuStyle.White;
            iconImage.raycastTarget = false;
            var arrowRect = Rect(rect, "HoverArrow");
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1f, .5f);
            arrowRect.sizeDelta = new Vector2(15f, 23f);
            arrowRect.anchoredPosition = new Vector2(-22f, 0f);
            var arrow = arrowRect.gameObject.AddComponent<Image>();
            arrow.sprite = HomeMenuIcons.Get(HomeMenuIcons.Shape.Chevron);
            arrow.color = HomeMenuStyle.Gold;
            arrow.raycastTarget = false;
            arrow.gameObject.SetActive(false);
            button.transition = Selectable.Transition.None;
            var visual = button.gameObject.AddComponent<HomeMenuButton>();
            visual.Initialize(kind, border, fill, arrow);
            return visual;
        }

        static TMP_Text Label(Transform parent, string name, string value, float size, Color color)
        {
            var text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            UiKit.StyleLabel(text, size, color);
            text.text = VietText.Fix(value);
            VietTypography.Apply(text);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        static void Disc(Transform parent, string name, Vector2 position, float size, Color color)
        {
            var rect = Rect(parent, name);
            Place(rect, position, new Vector2(size, size));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = disc ??= DiscSprite();
            image.color = color;
            image.raycastTarget = false;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        static Sprite GradientSprite()
        {
            var texture = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < 128; x++)
                texture.SetPixel(x, 0, new Color(HomeMenuStyle.Navy.r, HomeMenuStyle.Navy.g,
                    HomeMenuStyle.Navy.b, .8f * Mathf.SmoothStep(1f, 0f, x / 127f)));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 1), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect);
        }

        static Sprite DiscSprite()
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f,
                        Mathf.Clamp01((63.5f - Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f))) * 2f)));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f));
        }

        // Rasterize at the actual target dimensions so short loading tracks keep the
        // same slant angle and a uniform stroke instead of stretching a button texture.
        public static Sprite SlantSprite(bool outline, int width = 320, int height = 56, float? borderWidth = null)
        {
            float angle = UITheme.Shared.Menu.buttonSlantAngle;
            float stroke = Mathf.Clamp(borderWidth ?? UITheme.Shared.BorderWidth * .5f, 0f, height * .5f - 1f);
            var key = (angle, stroke, width, height, outline);
            if (SlantSprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            float lean = Mathf.Tan(angle * Mathf.Deg2Rad) * height;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float left = lean - y * lean / height;
                    float right = width - y * lean / height;
                    float outer = Mathf.Clamp01(x - left + 1f) * Mathf.Clamp01(right - x);
                    float inner = y >= stroke && y <= height - 1f - stroke && x >= left + stroke && x <= right - stroke ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, outer * (outline ? 1f - inner : 1f)));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f));
            SlantSprites[key] = sprite;
            return sprite;
        }
    }
}
