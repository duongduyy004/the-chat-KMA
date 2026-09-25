using System.Collections.Generic;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class MapPresentationBuilder
    {
        static Sprite heartSprite;
        static Sprite roundedRectSprite;
        static Sprite lockSprite;
        static readonly Dictionary<SubjectId, Sprite> SportSprites = new Dictionary<SubjectId, Sprite>();

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
            new Entry(SubjectId.Sprint, "Chạy nước rút", new Color32(49, 162, 222, 255), true),
            new Entry(SubjectId.Volleyball, "Bóng chuyền", new Color32(245, 158, 46, 255), true),
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
            Color background = Color.Lerp(theme == null ? new Color32(25, 130, 196, 255) : theme.Background,
                new Color32(10, 48, 82, 255), .46f);

            RectTransform root = Rect(screen.transform, "S5MapPresentation");
            Stretch(root, Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<Image>().color = background;
            RectTransform content = Rect(root, "Content");
            Stretch(content, new Vector2(64, 40), new Vector2(-64, -40));

            HeartBar hearts = Header(content, session, border);
            PinToTop((RectTransform)hearts.transform.parent.parent, 116f);
            RectTransform grid = Rect(content, "SelectionGrid");
            Anchor(grid, new Vector2(0f, .33f), new Vector2(1f, .84f));
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<CenteredLastRowGridLayout>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.spacing = new Vector2(16, 16); gridLayout.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ResponsiveGridLayout>().Refresh();
            LayoutElement gridElement = grid.gameObject.AddComponent<LayoutElement>();
            gridElement.flexibleWidth = 1; gridElement.preferredHeight = 392;
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            var nodes = new List<MapNodeView>();
            foreach (Entry entry in Entries) nodes.Add(Card(grid, screen, entry, card, muted, mutedForeground, border));
            grid.GetComponent<ResponsiveGridLayout>().Refresh();
            FutureRow(content, muted, mutedForeground, border);
            Anchor((RectTransform)content.Find("FutureRow"), new Vector2(.16f, .23f), new Vector2(.84f, .29f));
            screen.BindPresentation(nodes.ToArray(), hearts, session);
            foreach (MapNodeView node in nodes)
                if (!node.IsComingSoon && !node.IsInteractable) node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
        }

        static HeartBar Header(Transform parent, GameSession session, Color border)
        {
            RectTransform header = Rect(parent, "Header");
            Image headerSurface = header.gameObject.AddComponent<Image>();
            headerSurface.color = new Color32(8, 35, 61, 178);
            headerSurface.raycastTarget = false;
            HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 16, 10, 12);
            layout.spacing = 18; layout.childAlignment = TextAnchor.MiddleCenter; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = false;
            LayoutElement headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.minHeight = 116;
            headerElement.preferredHeight = 116;
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
            Text title = LayoutLabel(heading, "Title", "CHỌN MÔN THI", 56,
                Color.white, TextAnchor.LowerLeft);
            title.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 62;
            Text subtitle = LayoutLabel(heading, "Subtitle", "Chọn một môn để bắt đầu", 26,
                new Color32(201, 226, 245, 255), TextAnchor.UpperLeft);
            subtitle.transform.parent.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
            RectTransform livesPanel = Rect(header, "LivesPanel");
            Image livesSurface = livesPanel.gameObject.AddComponent<Image>();
            livesSurface.color = new Color32(13, 57, 92, 238);
            UseRoundedSurface(livesSurface);
            Outline livesOutline = livesPanel.gameObject.AddComponent<Outline>();
            livesOutline.effectColor = new Color32(255, 202, 58, 210);
            livesOutline.effectDistance = new Vector2(2f, -2f);
            HorizontalLayoutGroup livesLayout = livesPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            livesLayout.padding = new RectOffset(16, 16, 10, 10);
            livesLayout.spacing = 12;
            livesLayout.childAlignment = TextAnchor.MiddleCenter;
            livesLayout.childControlWidth = true;
            livesLayout.childControlHeight = true;
            livesLayout.childForceExpandWidth = false;
            livesLayout.childForceExpandHeight = false;
            LayoutElement livesElement = livesPanel.gameObject.AddComponent<LayoutElement>();
            livesElement.preferredWidth = 430;
            livesElement.preferredHeight = 80;
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
            Text lives = LayoutLabel(livesPanel, "LivesLabel", "LƯỢT: " + currentLives + "/" + GameSession.MaxLives, 24,
                Color.white, TextAnchor.MiddleRight);
            lives.horizontalOverflow = HorizontalWrapMode.Overflow;
            LayoutElement livesLabelLayout = lives.transform.parent.gameObject.AddComponent<LayoutElement>();
            livesLabelLayout.preferredWidth = 160;
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
            image.color = new Color32(255, 249, 231, 255);
            UseRoundedSurface(image);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(2f, -2f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 84f;
            element.preferredHeight = 84f;
            Text(root, "Label", label, 56, new Color32(8, 35, 61, 255), TextAnchor.MiddleCenter);
            return button;
        }

        static MapNodeView Card(Transform parent, MapScreen screen, Entry entry, Color card, Color muted, Color foreground, Color border)
        {
            RectTransform root = Rect(parent, entry.Subject + "Node");
            Image image = root.gameObject.AddComponent<Image>(); image.color = entry.Available ? card : muted;
            UseRoundedSurface(image);
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = entry.Available ? new Color32(255, 202, 58, 255) : new Color32(117, 138, 156, 255);
            outline.effectDistance = new Vector2(3, -3);
            Shadow shadow = root.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .4f); shadow.effectDistance = new Vector2(7, -7);
            Button button = null;
            if (entry.Available)
            {
                button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.colors = ButtonColors();
                root.gameObject.AddComponent<BrutalButton>();
            }
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10); layout.spacing = 4; layout.childControlWidth = true;
            layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            RectTransform stripe = Rect(root, "HeaderStripe");
            stripe.gameObject.AddComponent<Image>().color = entry.Color;
            stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = 4;
            RectTransform cardHeader = Rect(root, "CardHeader");
            HorizontalLayoutGroup cardHeaderLayout = cardHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
            cardHeaderLayout.spacing = 16; cardHeaderLayout.childAlignment = TextAnchor.MiddleLeft;
            cardHeaderLayout.childControlWidth = true; cardHeaderLayout.childControlHeight = true;
            cardHeaderLayout.childForceExpandWidth = false;
            cardHeaderLayout.childForceExpandHeight = false;
            cardHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;
            RectTransform icon = Rect(cardHeader, "SportIcon");
            Image iconPlate = icon.gameObject.AddComponent<Image>();
            iconPlate.color = entry.Color;
            UseRoundedSurface(iconPlate);
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 60;
            iconLayout.preferredHeight = 60;
            RectTransform glyph = Rect(icon, "IconGlyph");
            Stretch(glyph, new Vector2(12, 12), new Vector2(-12, -12));
            Image glyphImage = glyph.gameObject.AddComponent<Image>();
            glyphImage.sprite = SportIconSprite(entry.Subject);
            glyphImage.color = new Color32(8, 35, 61, 255);
            glyphImage.preserveAspect = true;
            glyphImage.raycastTarget = false;
            Text title = LayoutLabel(cardHeader, "Title", entry.Label, 36, entry.Available ? new Color32(8, 35, 61, 255) : foreground, TextAnchor.MiddleLeft);
            LayoutElement titleLayout = title.transform.parent.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 70;
            titleLayout.flexibleWidth = 1;
            Text status = LayoutLabel(root, "Status", string.Empty, 26, entry.Available ? new Color32(12, 105, 94, 255) : foreground,
                TextAnchor.MiddleLeft);
            RectTransform statusContainer = status.transform.parent as RectTransform;
            LayoutElement statusLayout = statusContainer.gameObject.AddComponent<LayoutElement>();
            statusLayout.ignoreLayout = true;
            statusContainer.anchorMin = Vector2.zero;
            statusContainer.anchorMax = new Vector2(entry.Available ? .70f : 1f, 0f);
            statusContainer.offsetMin = new Vector2(20f, 12f);
            statusContainer.offsetMax = new Vector2(entry.Available ? -4f : -20f, 44f);
            Text detail = LayoutLabel(root, "Detail", string.Empty, 26, foreground, TextAnchor.MiddleLeft);
            RectTransform detailContainer = detail.transform.parent as RectTransform;
            LayoutElement detailLayout = detailContainer.gameObject.AddComponent<LayoutElement>();
            detailLayout.ignoreLayout = true;
            detailContainer.anchorMin = Vector2.zero;
            detailContainer.anchorMax = new Vector2(.74f, 0f);
            detailContainer.offsetMin = new Vector2(20f, 46f);
            detailContainer.offsetMax = new Vector2(-4f, 76f);
            Text action = null;
            if (entry.Available)
            {
                action = Text(root, "ActionHint", "THI →", 26,
                    new Color32(163, 104, 0, 255), TextAnchor.MiddleRight);
                LayoutElement actionLayout = action.gameObject.AddComponent<LayoutElement>();
                actionLayout.ignoreLayout = true;
                RectTransform actionRect = action.rectTransform;
                actionRect.anchorMin = new Vector2(.68f, 0f);
                actionRect.anchorMax = new Vector2(1f, 0f);
                actionRect.offsetMin = new Vector2(4f, 12f);
                actionRect.offsetMax = new Vector2(-20f, 44f);
            }
            MapNodeView node = root.gameObject.AddComponent<MapNodeView>();
            node.Bind(button, title, detail, detailContainer.gameObject);
            node.BindPresentation(status, action, image, iconPlate, outline, entry.Color);
            node.Configure(entry.Subject, entry.Label, !entry.Available, null, 5);
            if (entry.Available) button.onClick.AddListener(() => screen.SelectSubject(entry.Subject));
            else
            {
                node.SetAvailability(false, "ĐANG PHÁT TRIỂN");
                AddCornerLockIcon(root, new Color32(38, 60, 77, 255));
            }
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

            Text upcoming = LayoutLabel(row, "UpcomingLabel", "SẮP RA MẮT", 22,
                new Color32(201, 226, 245, 255), TextAnchor.MiddleRight);
            LayoutElement upcomingLayout = upcoming.transform.parent.gameObject.AddComponent<LayoutElement>();
            upcomingLayout.preferredWidth = 150;
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
            Text(root, "Label", label, 22, foreground, TextAnchor.MiddleCenter);
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

        static Sprite SportIconSprite(SubjectId subject)
        {
            if (SportSprites.TryGetValue(subject, out Sprite cached) && cached != null)
                return cached;
            SportSprites.Remove(subject);

            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SportIcon_" + subject,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            switch (subject)
            {
                case SubjectId.Sprint:
                    DrawLine(pixels, size, 62, 80, 36, 52, 8);
                    DrawLine(pixels, size, 36, 52, 61, 51, 8);
                    DrawLine(pixels, size, 61, 51, 31, 15, 8);
                    DrawLine(pixels, size, 24, 34, 45, 34, 6);
                    break;
                case SubjectId.Football:
                    DrawRing(pixels, size, 48, 48, 34, 6);
                    DrawCircle(pixels, size, 48, 48, 11);
                    DrawLine(pixels, size, 48, 37, 48, 14, 5);
                    DrawLine(pixels, size, 39, 54, 19, 66, 5);
                    DrawLine(pixels, size, 57, 54, 77, 66, 5);
                    DrawLine(pixels, size, 42, 41, 25, 25, 5);
                    DrawLine(pixels, size, 54, 41, 70, 25, 5);
                    break;
                case SubjectId.Volleyball:
                    DrawRing(pixels, size, 48, 48, 34, 6);
                    DrawLine(pixels, size, 48, 48, 48, 82, 5);
                    DrawLine(pixels, size, 48, 48, 19, 31, 5);
                    DrawLine(pixels, size, 48, 48, 77, 31, 5);
                    break;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.name = "SportIcon_" + subject;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SportSprites[subject] = sprite;
            return sprite;
        }

        static void UseRoundedSurface(Image image)
        {
            image.sprite = RoundedRectSprite();
            image.type = Image.Type.Sliced;
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
            texture.Apply(false, true);
            roundedRectSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            roundedRectSprite.name = "RuntimeRoundedRect";
            roundedRectSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedRectSprite;
        }

        static void AddCornerLockIcon(Transform parent, Color color)
        {
            RectTransform icon = Rect(parent, "LockIcon");
            LayoutElement layout = icon.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            icon.anchorMin = Vector2.one;
            icon.anchorMax = Vector2.one;
            icon.pivot = Vector2.one;
            icon.anchoredPosition = new Vector2(-14f, -14f);
            icon.sizeDelta = new Vector2(42f, 42f);
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = LockSprite();
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static Sprite LockSprite()
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
            texture.Apply(false, true);
            lockSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            lockSprite.name = "RuntimeLockIcon";
            lockSprite.hideFlags = HideFlags.HideAndDontSave;
            return lockSprite;
        }

        static void DrawCircle(Color32[] pixels, int size, int centerX, int centerY, int radius)
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

        static void DrawLine(Color32[] pixels, int size, int startX, int startY, int endX, int endY, int thickness)
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

        static void SetIconPixel(Color32[] pixels, int size, int x, int y)
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
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
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
            float cellHeight = Mathf.Clamp(cellWidth * 0.56f, 220f, 264f);
            if (rect.rect.height > 0f)
                cellHeight = Mathf.Min(cellHeight, Mathf.Max(1f, (rect.rect.height - grid.spacing.y) / 2f));
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            int titleSize = cellWidth < 350f ? 30 : cellWidth < 420f ? 32 : 36;
            foreach (MapNodeView node in GetComponentsInChildren<MapNodeView>(true))
            {
                Transform title = node.transform.Find("CardHeader/TitleContainer/Title");
                if (title != null)
                    title.GetComponent<Text>().fontSize = titleSize;
            }
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
