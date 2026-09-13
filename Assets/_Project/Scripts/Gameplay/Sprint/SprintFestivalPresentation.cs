using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public static class SprintFestivalPresentation
    {
        static readonly Color Navy = new Color32(8, 35, 61, 242);
        static readonly Color Coral = new Color32(255, 89, 94, 235);
        static readonly Color Gold = new Color32(255, 202, 58, 235);
        static readonly Color Cream = new Color32(255, 249, 231, 255);

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

            GameObject oldChrome = GameObject.Find("SprintFestivalChrome");
            if (oldChrome != null)
                Object.Destroy(oldChrome);

            DisableSharedMetrics(safeArea);

            RectTransform root = Rect(safeArea, "SprintBroadcastChrome");
            Stretch(root);
            root.SetAsLastSibling();
            EnsurePause(root, font);

            RectTransform scoreboard = Rect(root, "Scoreboard");
            scoreboard.anchorMin = new Vector2(.025f, .72f);
            scoreboard.anchorMax = new Vector2(.31f, .965f);
            scoreboard.offsetMin = Vector2.zero;
            scoreboard.offsetMax = Vector2.zero;
            Image scoreboardImage = scoreboard.gameObject.AddComponent<Image>();
            scoreboardImage.color = Navy;
            scoreboardImage.raycastTarget = false;
            Outline scoreboardOutline = scoreboard.gameObject.AddComponent<Outline>();
            scoreboardOutline.effectColor = new Color32(3, 18, 33, 255);
            scoreboardOutline.effectDistance = new Vector2(3f, -3f);

            TMP_Text distance = Metric(scoreboard, "Distance", font, 30f, Cream,
                new Vector2(.08f, .64f), new Vector2(.92f, .94f));
            TMP_Text rank = Metric(scoreboard, "Rank", font, 30f, Gold,
                new Vector2(.08f, .35f), new Vector2(.92f, .64f));
            TMP_Text combo = Metric(scoreboard, "Combo", font, 24f, Coral,
                new Vector2(.08f, .13f), new Vector2(.92f, .36f));
            distance.text = "0 / 100 m";
            rank.text = "1st";
            combo.text = "COMBO ×0";

            RectTransform progressTrack = Rect(scoreboard, "ProgressTrack");
            progressTrack.anchorMin = new Vector2(.08f, .05f);
            progressTrack.anchorMax = new Vector2(.92f, .11f);
            progressTrack.offsetMin = Vector2.zero;
            progressTrack.offsetMax = Vector2.zero;
            Image trackImage = progressTrack.gameObject.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, .22f);
            trackImage.raycastTarget = false;
            Image progressFill = Rect(progressTrack, "ProgressFill").gameObject.AddComponent<Image>();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount = 0f;
            progressFill.color = Gold;
            progressFill.raycastTarget = false;

            TMP_Text mode = Text(root, "ModeLabel", "CHẠY NƯỚC RÚT · 100 M", font, 28f,
                Cream, TextAlignmentOptions.Center);
            mode.rectTransform.anchorMin = new Vector2(.33f, .92f);
            mode.rectTransform.anchorMax = new Vector2(.67f, .985f);
            mode.rectTransform.offsetMin = Vector2.zero;
            mode.rectTransform.offsetMax = Vector2.zero;

            RectTransform prompts = Rect(root, "TouchPrompts");
            prompts.anchorMin = new Vector2(0f, .02f);
            prompts.anchorMax = new Vector2(1f, .16f);
            prompts.offsetMin = Vector2.zero;
            prompts.offsetMax = Vector2.zero;
            TMP_Text left = Prompt(prompts, "LeftPrompt", "CHẠM TRÁI", font, Coral,
                new Vector2(.035f, .08f), new Vector2(.455f, .92f));
            TMP_Text right = Prompt(prompts, "RightPrompt", "CHẠM PHẢI", font, Gold,
                new Vector2(.545f, .08f), new Vector2(.965f, .92f));
            left.alignment = TextAlignmentOptions.Center;
            right.alignment = TextAlignmentOptions.Center;

            StyleTapArea("LeftTap", new Color(1f, .35f, .37f, .10f));
            StyleTapArea("RightTap", new Color(1f, .79f, .23f, .10f));
            EnsurePlayerMarker(root);
        }

        static void DisableSharedMetrics(Transform safeArea)
        {
            string[] obsolete = { "Time", "Phase", "Score", "Status", "Progress", "Stamina", "SprintMetrics" };
            for (int i = 0; i < obsolete.Length; i++)
            {
                Transform metric = safeArea.Find(obsolete[i]);
                if (metric != null)
                    metric.gameObject.SetActive(false);
            }
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

        static void EnsurePlayerMarker(Transform parent)
        {
            if (parent.Find("PlayerMarker") != null)
                return;

            Transform player = GameObject.Find("Player")?.transform;
            if (player == null)
                return;

            RectTransform marker = Rect(parent, "PlayerMarker");
            marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(.5f, .5f);
            marker.sizeDelta = new Vector2(92f, 48f);
            Image plate = marker.gameObject.AddComponent<Image>();
            plate.color = new Color(0f, 1f, 1f, .16f);
            plate.raycastTarget = false;
            Outline outline = marker.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.cyan;
            outline.effectDistance = new Vector2(2f, -2f);
            marker.gameObject.AddComponent<SprintPlayerMarkerFollower>().Bind(player);
            TMP_Text label = Text(marker, "Label", "YOU", parent.GetComponentInChildren<TMP_Text>(true)?.font,
                20f, Color.cyan, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, new Vector2(5f, 3f), new Vector2(-5f, -3f));
        }

        static void EnsurePause(Transform parent, TMP_FontAsset font)
        {
            if (Object.FindFirstObjectByType<KMA.Gameplay.UI.PausePanel>() != null)
                return;
            GameObject root = new GameObject("PausePanel", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-28f, -28f);
            rect.sizeDelta = new Vector2(142f, 58f);
            Image image = root.GetComponent<Image>();
            image.color = Navy;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(3f, -3f);
            root.AddComponent<KMA.Gameplay.UI.PausePanel>();
            TMP_Text label = Text(root.transform, "Label", "TẠM DỪNG", font, 18f,
                Cream, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, new Vector2(8f, 5f), new Vector2(-8f, -5f));
        }

        static TMP_Text Prompt(Transform parent, string name, string value, TMP_FontAsset font,
            Color color, Vector2 min, Vector2 max)
        {
            RectTransform root = Rect(parent, name);
            root.anchorMin = min;
            root.anchorMax = max;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.gameObject.SetActive(false);
            TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = 25f;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.raycastTarget = false;
            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .85f);
            outline.effectDistance = new Vector2(2f, -2f);
            root.gameObject.SetActive(true);
            return text;
        }

        static void StyleTapArea(string name, Color color)
        {
            GameObject target = GameObject.Find(name);
            if (target == null)
                return;
            Image image = target.GetComponent<Image>();
            if (image != null)
                image.color = color;
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

    public sealed class SprintPlayerMarkerFollower : MonoBehaviour
    {
        Transform target;

        public Transform Target => target;

        public void Bind(Transform playerRoot) => target = playerRoot;

        void LateUpdate()
        {
            if (target == null || transform.parent is not RectTransform parent)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera ?? Camera.main
                : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, target.position + Vector3.up * .7f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 local))
                ((RectTransform)transform).anchoredPosition = local;
        }
    }
}
