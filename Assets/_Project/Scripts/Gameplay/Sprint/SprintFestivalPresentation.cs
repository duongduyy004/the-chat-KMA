using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public static class SprintFestivalPresentation
    {
        public static void Build()
        {
            if (GameObject.Find("SprintBroadcastChrome") != null)
                return;

            Canvas canvas = GameObject.Find("S2_HUD_Minigame")?.GetComponent<Canvas>()
                ?? Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;
            TMP_FontAsset font = canvas.GetComponentInChildren<TMP_Text>(true)?.font
                ?? Object.FindFirstObjectByType<TMP_Text>()?.font;

            Transform safeArea = canvas.transform.Find("SafeAreaRoot");
            if (safeArea == null)
                return;
            PrepareSafeArea(safeArea);

            GameObject oldChrome = GameObject.Find("SprintFestivalChrome");
            if (oldChrome != null)
                Object.Destroy(oldChrome);

            DisableSharedMetrics(safeArea);

            RectTransform root = Rect(safeArea, "SprintBroadcastChrome");
            Stretch(root);
            root.SetAsLastSibling();

            RectTransform safeRect = (RectTransform)safeArea;
            Rect safe = SafeRect(safeRect);

            var chromeLayout = root.gameObject.AddComponent<SprintChromeLayout>();
            chromeLayout.Bind(safeRect);

            // Progress rail
            Image railTrack = Panel(root, "ProgressRail", SprintUiTheme.WithAlpha(Color.white, .22f),
                Mathf.RoundToInt(safe.height * .015f));
            ApplyRect(railTrack.rectTransform, safe, SprintUiLayout.ProgressRailRect(safe));
            chromeLayout.Register(railTrack.rectTransform, SprintUiLayout.ProgressRailRect);
            Image railFill = Panel(railTrack.transform, "RailFill", SprintUiTheme.Accent,
                Mathf.RoundToInt(safe.height * .015f));
            Stretch(railFill.rectTransform);
            railFill.type = Image.Type.Filled;
            railFill.fillMethod = Image.FillMethod.Horizontal;
            railFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            railFill.fillAmount = 0f;
            Image pip = Panel(railTrack.transform, "PlayerPip", SprintUiTheme.Player, 4);
            pip.rectTransform.anchorMin = new Vector2(0f, -.35f);
            pip.rectTransform.anchorMax = new Vector2(0f, 1.35f);
            pip.rectTransform.pivot = new Vector2(.5f, .5f);
            pip.rectTransform.sizeDelta = new Vector2(safe.height * .014f, 0f);
            pip.rectTransform.anchoredPosition = Vector2.zero;

            // Scoreboard
            Image scoreboard = Panel(root, "Scoreboard", SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .92f),
                Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
            ApplyRect(scoreboard.rectTransform, safe, SprintUiLayout.ScoreboardRect(safe));
            chromeLayout.Register(scoreboard.rectTransform, SprintUiLayout.ScoreboardRect);
            AddShadow(scoreboard);

            TMP_Text distance = Metric(scoreboard.transform, "Distance", font, SprintUiTheme.Title,
                SprintUiTheme.TextPrimary, new Vector2(.05f, .46f), new Vector2(.62f, .92f));
            distance.alignment = TextAlignmentOptions.Left;
            distance.text = "0 / 100 m";

            Image rankBadge = Panel(scoreboard.transform, "RankBadge",
                SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .22f), Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
            rankBadge.rectTransform.anchorMin = new Vector2(.66f, .46f);
            rankBadge.rectTransform.anchorMax = new Vector2(.95f, .92f);
            rankBadge.rectTransform.offsetMin = Vector2.zero;
            rankBadge.rectTransform.offsetMax = Vector2.zero;
            TMP_Text rank = Text(rankBadge.transform, "RankLabel", "1st", font, SprintUiTheme.Headline,
                SprintUiTheme.Accent, TextAlignmentOptions.Center);
            Stretch(rank.rectTransform);

            TMP_Text combo = Metric(scoreboard.transform, "Combo", font, SprintUiTheme.Body,
                SprintUiTheme.Energy, new Vector2(.05f, .10f), new Vector2(.62f, .42f));
            combo.alignment = TextAlignmentOptions.Left;
            combo.text = "COMBO ×0";

            // Mode chip
            TMP_Text mode = Text(root, "ModeLabel", "CHẠY NƯỚC RÚT · 100M", font, SprintUiTheme.Caption,
                SprintUiTheme.WithAlpha(SprintUiTheme.TextPrimary, .6f), TextAlignmentOptions.Center);
            ApplyRect(mode.rectTransform, safe, SprintUiLayout.ModeChipRect(safe));
            chromeLayout.Register(mode.rectTransform, SprintUiLayout.ModeChipRect);

            EnsurePause(root, font, safe, chromeLayout);

            EnsureStartPresentation(root, font, safe);

            EnsureControls(root, safe);
            EnsurePlayerIdentity(root);
            EnsureFinishLine(root);
            EnsureResultPresentation();
        }

        /// Positions a RectTransform from an absolute rect expressed in the safe area's own space.
        static void ApplyRect(RectTransform rect, Rect safe, Rect target)
        {
            rect.anchorMin = new Vector2(
                Mathf.InverseLerp(safe.xMin, safe.xMax, target.xMin),
                Mathf.InverseLerp(safe.yMin, safe.yMax, target.yMin));
            rect.anchorMax = new Vector2(
                Mathf.InverseLerp(safe.xMin, safe.xMax, target.xMax),
                Mathf.InverseLerp(safe.yMin, safe.yMax, target.yMax));
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// Reachable from SprintChromeLayout, which lives outside this static class.
        internal static void ApplyRectPublic(RectTransform rect, Rect safe, Rect target) =>
            ApplyRect(rect, safe, target);

        static Rect SafeRect(RectTransform safeArea) =>
            new Rect(0f, 0f, safeArea.rect.width, safeArea.rect.height);

        static Image Panel(Transform parent, string name, Color color, int radius)
        {
            RectTransform root = Rect(parent, name);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = SprintUiShapes.RoundedRect(radius);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void AddShadow(Component target)
        {
            var shadow = target.gameObject.AddComponent<Shadow>();
            shadow.effectColor = SprintUiTheme.ShadowColor;
            shadow.effectDistance = SprintUiTheme.ShadowOffset;
        }

        static void EnsureFinishLine(RectTransform root)
        {
            RectTransform finish = Rect(root, "FinishLine");
            finish.anchorMin = new Vector2(.84f, 0f);
            finish.anchorMax = new Vector2(.9f, 1f);
            finish.offsetMin = Vector2.zero;
            finish.offsetMax = Vector2.zero;

            const int squareCount = 10;
            for (int i = 0; i < squareCount; i++)
            {
                RectTransform square = Rect(finish, $"Square{i}");
                square.anchorMin = new Vector2(0f, (float)i / squareCount);
                square.anchorMax = new Vector2(1f, (float)(i + 1) / squareCount);
                square.offsetMin = Vector2.zero;
                square.offsetMax = Vector2.zero;
                Image image = square.gameObject.AddComponent<Image>();
                image.color = i % 2 == 0 ? Color.black : Color.white;
                image.raycastTarget = false;
            }

            var presenter = root.GetComponent<SprintFinishLinePresenter>()
                ?? root.gameObject.AddComponent<SprintFinishLinePresenter>();
            presenter.Configure(Object.FindFirstObjectByType<SprintController>(), finish.gameObject);
        }

        static void EnsureResultPresentation()
        {
            KMA.Gameplay.UI.ResultPanel panel =
                Object.FindFirstObjectByType<KMA.Gameplay.UI.ResultPanel>(FindObjectsInactive.Include);
            if (panel == null)
                return;

            var presenter = panel.GetComponent<SprintResultPresentation>()
                ?? panel.gameObject.AddComponent<SprintResultPresentation>();
            presenter.Bind(panel, Object.FindFirstObjectByType<SprintController>());
        }

        static void EnsureStartPresentation(RectTransform parent, TMP_FontAsset font, Rect safe)
        {
            RectTransform root = Rect(parent, "StartPresentation");
            Stretch(root);
            root.SetAsLastSibling();

            TMP_Text countdown = Text(root, "CountdownLabel", string.Empty, font, SprintUiTheme.Display,
                SprintUiTheme.Accent, TextAlignmentOptions.Center);
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(countdown.rectTransform, safe, SprintUiLayout.CountdownRect(safe));
            countdown.outlineWidth = .2f;
            countdown.outlineColor = new Color32(3, 18, 33, 255);

            RectTransform instructionRoot = Rect(root, "InstructionPlate");
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(instructionRoot, safe, SprintUiLayout.InstructionRect(safe));
            Image plate = instructionRoot.gameObject.AddComponent<Image>();
            plate.sprite = SprintUiShapes.RoundedRect(Mathf.RoundToInt(SprintUiTheme.RadiusPanel));
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = 1f;
            plate.color = SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .82f);
            plate.raycastTarget = false;
            AddShadow(plate);

            TMP_Text instruction = Text(instructionRoot, "InstructionLabel",
                SprintStartPresentation.InstructionCopy, font, SprintUiTheme.BodyLarge,
                SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
            Stretch(instruction.rectTransform, new Vector2(SprintUiTheme.SpaceMd, SprintUiTheme.SpaceXs),
                new Vector2(-SprintUiTheme.SpaceMd, -SprintUiTheme.SpaceXs));

            SprintStartPresentation presenter = Object.FindFirstObjectByType<SprintStartPresentation>();
            if (presenter == null)
                presenter = root.gameObject.AddComponent<SprintStartPresentation>();
            presenter.Configure(countdown.gameObject, countdown, instructionRoot.gameObject, instruction);
            presenter.Bind(Object.FindFirstObjectByType<SprintController>());

            var chromeLayout = parent.GetComponent<SprintChromeLayout>();
            if (chromeLayout != null)
            {
                chromeLayout.Register(countdown.rectTransform, SprintUiLayout.CountdownRect);
                chromeLayout.Register(instructionRoot, SprintUiLayout.InstructionRect);
            }
        }

        static void PrepareSafeArea(Transform safeArea)
        {
            safeArea.gameObject.SetActive(true);
            var nestedFitter = safeArea.GetComponent<KMA.Gameplay.UI.SafeAreaFitter>();
            if (nestedFitter != null)
                nestedFitter.enabled = false;

            if (safeArea is RectTransform rectTransform)
            {
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }
        }

        static void EnsureControls(RectTransform root, Rect safe)
        {
            KMA.Input.ScreenTapArea leftTap = FindTapArea("LeftTap");
            KMA.Input.ScreenTapArea rightTap = FindTapArea("RightTap");
            if (leftTap == null || rightTap == null)
                return;

            ControlVisual left = BuildControl(leftTap, safe, true);
            ControlVisual right = BuildControl(rightTap, safe, false);

            var presenter = root.GetComponent<SprintControlPresenter>()
                ?? root.gameObject.AddComponent<SprintControlPresenter>();
            presenter.Configure(Object.FindFirstObjectByType<SprintController>(),
                left.Visual, right.Visual, left.Background, right.Background, left.Border, right.Border);
            presenter.BindPressFeedback(leftTap, rightTap);

            var chromeLayout = root.GetComponent<SprintChromeLayout>();
            if (chromeLayout != null)
            {
                chromeLayout.Register(leftTap.GetComponent<RectTransform>(), safeRect => SprintUiLayout.ControlRect(safeRect, true));
                chromeLayout.Register(rightTap.GetComponent<RectTransform>(), safeRect => SprintUiLayout.ControlRect(safeRect, false));
            }
        }

        readonly struct ControlVisual
        {
            public readonly RectTransform Visual;
            public readonly Image Background;
            public readonly Image Border;

            public ControlVisual(RectTransform visual, Image background, Image border)
            {
                Visual = visual;
                Background = background;
                Border = border;
            }
        }

        static ControlVisual BuildControl(KMA.Input.ScreenTapArea tapArea, Rect safe, bool left)
        {
            var tapRect = tapArea.GetComponent<RectTransform>();
            // A degenerate safe rect (batchmode's headless canvas, or the first frame before
            // Canvas layout runs) would collapse InverseLerp to a zero-size anchor pin. Skip and
            // keep the authored tap-area rect; SprintChromeLayout re-applies once safe is valid.
            if (safe.width > 0f && safe.height > 0f)
                ApplyRect(tapRect, safe, SprintUiLayout.ControlRect(safe, left));

            Image tapImage = tapArea.GetComponent<Image>() ?? tapArea.gameObject.AddComponent<Image>();
            tapImage.color = new Color(1f, 1f, 1f, 0f);
            tapImage.raycastTarget = true;

            Transform existing = tapRect.Find("Visual");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            RectTransform visual = Rect(tapRect, "Visual");
            Stretch(visual);

            int radius = Mathf.RoundToInt(SprintUiTheme.RadiusControl);
            Image border = Panel(visual, "Border", SprintUiTheme.WithAlpha(SprintUiTheme.Accent, .25f), radius);
            Stretch(border.rectTransform);
            AddShadow(border);

            Image background = Panel(visual, "Background",
                SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .42f), radius);
            float inset = SprintUiTheme.BorderWidth;
            Stretch(background.rectTransform, new Vector2(inset, inset), new Vector2(-inset, -inset));

            TMP_FontAsset font = Object.FindFirstObjectByType<TMP_Text>()?.font;
            TMP_Text arrow = Text(visual, "Arrow", left ? "←" : "→", font,
                SprintUiTheme.BodyLarge * 1.6f, SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
            arrow.rectTransform.anchorMin = new Vector2(.1f, .44f);
            arrow.rectTransform.anchorMax = new Vector2(.9f, .88f);
            arrow.rectTransform.offsetMin = Vector2.zero;
            arrow.rectTransform.offsetMax = Vector2.zero;

            TMP_Text label = Text(visual, "Label", left ? "TRÁI" : "PHẢI", font,
                SprintUiTheme.BodyLarge, SprintUiTheme.TextPrimary, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = new Vector2(.1f, .12f);
            label.rectTransform.anchorMax = new Vector2(.9f, .46f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            return new ControlVisual(visual, background, border);
        }

        static void DisableSharedMetrics(Transform safeArea)
        {
            Transform canvasRoot = safeArea.parent != null ? safeArea.parent : safeArea;
            string[] obsolete =
            {
                "Timer", "Phase", "Score", "Status", "Progress", "Stamina", "HeartBar", "SprintMetrics"
            };

            for (int i = 0; i < obsolete.Length; i++)
            {
                Transform found = FindDescendant(canvasRoot, obsolete[i]);
                if (found != null)
                    found.gameObject.SetActive(false);
            }
        }

        static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static TMP_Text Metric(Transform parent, string name, TMP_FontAsset font, float fontSize, Color color,
            Vector2 min, Vector2 max)
        {
            TMP_Text text = Text(parent, name, string.Empty, font, fontSize, color, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = min;
            text.rectTransform.anchorMax = max;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return text;
        }

        static void EnsurePlayerIdentity(Transform chrome)
        {
            Transform chromeMarker = chrome.Find("PlayerMarker");
            if (chromeMarker != null)
                Object.Destroy(chromeMarker.gameObject);

            Transform player = GameObject.Find("Player")?.transform;
            if (player == null)
                return;

            Transform presentation = player.GetComponentInChildren<RunnerVisualPresenter>(true)?.transform ?? player;
            Transform marker = presentation.Find("PlayerMarker");
            if (marker == null)
            {
                marker = presentation.Find("PlayerLabel");
                if (marker == null)
                {
                    marker = new GameObject("PlayerMarker").transform;
                    marker.SetParent(presentation, false);
                    marker.localPosition = new Vector3(0f, 1.65f, 0f);
                }
                else
                {
                    marker.name = "PlayerMarker";
                }
            }

            TextMesh label = marker.GetComponent<TextMesh>() ?? marker.gameObject.AddComponent<TextMesh>();
            label.text = "PLAYER";
            label.fontSize = 48;
            label.characterSize = .065f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.cyan;
            MeshRenderer labelRenderer = marker.GetComponent<MeshRenderer>();
            if (labelRenderer != null)
                labelRenderer.sortingOrder = 20;

            SpriteRenderer playerVisual = presentation.GetComponentInChildren<SpriteRenderer>(true);
            if (playerVisual != null)
            {
                var identity = presentation.GetComponent<SprintPlayerIdentityOutline>()
                    ?? presentation.gameObject.AddComponent<SprintPlayerIdentityOutline>();
                identity.Bind(playerVisual, Color.cyan);
            }
        }

        static void EnsurePause(RectTransform parent, TMP_FontAsset font, Rect safe, SprintChromeLayout chromeLayout)
        {
            if (Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>() != null)
                return;

            Image button = Panel(parent, "PausePanel",
                SprintUiTheme.WithAlpha(SprintUiTheme.Surface, .92f), Mathf.RoundToInt(SprintUiTheme.RadiusPause));
            button.raycastTarget = true;
            if (safe.width > 0f && safe.height > 0f)
            {
                ApplyRect(button.rectTransform, safe, SprintUiLayout.PauseRect(safe));
            }
            else
            {
                // A degenerate first-frame safe rect (batchmode's headless canvas) collapses the
                // usual spread-anchor math to (0,0); pin to the top-right corner instead.
                // SprintChromeLayout re-applies the real anchors once safe becomes valid.
                button.rectTransform.anchorMin = Vector2.one;
                button.rectTransform.anchorMax = Vector2.one;
                button.rectTransform.pivot = Vector2.one;
                button.rectTransform.sizeDelta = new Vector2(96f, 96f);
                button.rectTransform.anchoredPosition = new Vector2(-38f, -65f);
            }
            chromeLayout.Register(button.rectTransform, SprintUiLayout.PauseRect);
            AddShadow(button);
            button.gameObject.AddComponent<Button>();
            button.gameObject.AddComponent<KMA.Gameplay.UI.PausePanel>();

            PauseBar(button.transform, "BarLeft", .28f, .44f);
            PauseBar(button.transform, "BarRight", .56f, .72f);
        }

        static void PauseBar(Transform parent, string name, float minX, float maxX)
        {
            Image bar = Panel(parent, name, SprintUiTheme.TextPrimary, 2);
            bar.rectTransform.anchorMin = new Vector2(minX, .26f);
            bar.rectTransform.anchorMax = new Vector2(maxX, .74f);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = Vector2.zero;
        }

        static KMA.Input.ScreenTapArea FindTapArea(string name)
        {
            GameObject target = GameObject.Find(name);
            return target == null ? null : target.GetComponent<KMA.Input.ScreenTapArea>();
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

    public sealed class SprintPlayerIdentityOutline : MonoBehaviour
    {
        [SerializeField] SpriteRenderer source;
        [SerializeField] SpriteRenderer outline;
        [SerializeField] Color outlineColor = Color.cyan;

        public SpriteRenderer Source => source;
        public SpriteRenderer Outline => outline;
        public Color OutlineColor => outlineColor;

        public void Bind(SpriteRenderer sourceRenderer, Color color)
        {
            source = sourceRenderer;
            outlineColor = color;
            EnsureOutlineRenderer();
            Refresh();
        }

        void LateUpdate()
        {
            Refresh();
        }

        void EnsureOutlineRenderer()
        {
            if (source == null)
                return;

            if (outline == null)
            {
                Transform existing = source.transform.Find("PlayerIdentityOutline");
                if (existing != null)
                    outline = existing.GetComponent<SpriteRenderer>();
            }

            if (outline == null)
            {
                var outlineObject = new GameObject("PlayerIdentityOutline");
                outlineObject.transform.SetParent(source.transform, false);
                outlineObject.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
                outline = outlineObject.AddComponent<SpriteRenderer>();
            }

            outline.sharedMaterial = source.sharedMaterial;
            outline.sortingLayerID = source.sortingLayerID;
            outline.sortingOrder = source.sortingOrder - 1;
        }

        void Refresh()
        {
            if (source == null)
                return;

            EnsureOutlineRenderer();
            if (outline == null)
                return;

            outline.sprite = source.sprite;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;
            outline.enabled = source.enabled;
            outline.color = outlineColor;
        }
    }
}
