#if UNITY_EDITOR
using System;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    /// Restyles the shared minigame prefabs with the UI kit in place. Node names and serialized
    /// references stay, so every scene instance (GameOver, minigames) follows.
    public static class MinigamePrefabStyler
    {
        public const string HudPrefab = "Assets/_Project/Prefabs/UI/HUD_Minigame.prefab";
        public const string PhasePrefab = "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab";
        public const string ResultPrefab = "Assets/_Project/Prefabs/UI/ResultPanel.prefab";

        [MenuItem("KMA/UI/Restyle Shared Minigame Prefabs")]
        public static void RestyleAll()
        {
            Restyle(HudPrefab, StyleHud);
            Restyle(PhasePrefab, StylePhaseOverlay);
            Restyle(ResultPrefab, StyleResultPanel);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] Shared minigame prefabs restyled.");
        }

        static void Restyle(string path, Action<GameObject> style)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                style(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void StyleHud(GameObject root)
        {
            Transform safe = root.transform.Find("SafeAreaRoot");
            StyleText(safe, "Timer", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary, true);
            StyleText(safe, "Phase", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            StyleText(safe, "Score", MinigameUiTheme.Headline, MinigameUiTheme.Accent, true);
            StyleText(safe, "Status", MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary);
            StyleFilledBar(safe.Find("Progress"), MinigameUiTheme.Accent);
            StyleFilledBar(safe.Find("Stamina"), MinigameUiTheme.Success);

            Transform hearts = safe.Find("HeartBar");
            foreach (Transform heart in hearts)
            {
                var image = heart.GetComponent<Image>();
                image.sprite = UiKitAssets.Load().Circle;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = MinigameUiTheme.Energy;
            }
        }

        static void StyleFilledBar(Transform bar, Color fillColor)
        {
            float radius = MinigameUiTheme.BarHeight * .5f;
            var track = bar.GetComponent<Image>();
            if (track != null)
            {
                UiKit.SetRadius(track, radius);
                track.color = MinigameUiTheme.Track;
                StripEffects(track.gameObject);
            }

            // MinigameHUD drives fillAmount, so the fill stays a Filled image.
            var fill = bar.Find("Fill").GetComponent<Image>();
            Image.FillMethod method = fill.fillMethod;
            int origin = fill.fillOrigin;
            UiKit.SetRadius(fill, radius);
            fill.type = Image.Type.Filled;
            fill.fillMethod = method;
            fill.fillOrigin = origin;
            fill.color = fillColor;
        }

        static void StylePhaseOverlay(GameObject root)
        {
            Transform t = root.transform;
            Image tutorialCard = t.Find("TutorialRoot").GetComponent<Image>();
            if (tutorialCard != null)
            {
                StripEffects(tutorialCard.gameObject);
                UiKit.StylePanel(tutorialCard);
            }
            StyleText(t, "TutorialRoot/TutorialTitle", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            TMP_Text instruction = StyleText(t, "TutorialRoot/InstructionLabel", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            UiKit.FitLabel(instruction, MinigameUiTheme.Body);
            StyleText(t, "TutorialRoot/StepLabel", MinigameUiTheme.Caption, MinigameUiTheme.Accent);
            StyleButton(t, "TutorialRoot/BackButton", ButtonVariant.Secondary);
            StyleButton(t, "TutorialRoot/NextButton", ButtonVariant.Primary);
            StyleButton(t, "TutorialRoot/SkipButton", ButtonVariant.Secondary);
            StyleButton(t, "TutorialRoot/CloseButton", ButtonVariant.Primary);

            // The countdown is a bare number, like Sprint's.
            RemoveImage(t.Find("CountdownRoot").gameObject);
            UiKit.StyleCountdown(t.Find("CountdownRoot/CountdownLabel").GetComponent<TMP_Text>());

            StyleChipRoot(t.Find("PlayRoot"));
            StyleChipRoot(t.Find("ResolveRoot"));
            StyleText(t, "PlayRoot/PlayLabel", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleText(t, "ResolveRoot/ResolveLabel", MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleText(t, "PhaseLabel", MinigameUiTheme.Headline, MinigameUiTheme.TextPrimary, true);
        }

        static void StyleChipRoot(Transform node)
        {
            var image = node.GetComponent<Image>();
            if (image == null)
                return;
            StripEffects(node.gameObject);
            UiKit.StylePanel(image, MinigameUiTheme.RadiusPanel, MinigameUiTheme.SurfaceSoft);
        }

        static void StyleResultPanel(GameObject root)
        {
            Transform t = root.transform;
            Transform backdropNode = t.Find("Backdrop");
            var backdrop = UiKit.GetOrAdd<Image>(backdropNode);
            StripEffects(backdrop.gameObject);
            backdrop.sprite = null;
            backdrop.type = Image.Type.Simple;
            backdrop.color = MinigameUiTheme.Scrim;
            // Cover the whole canvas whatever its aspect; a fixed 1920x1080 left bare bands on wide screens.
            UiKit.Stretch((RectTransform)root.transform);
            UiKit.Stretch((RectTransform)backdropNode);

            Transform content = t.Find("Content");
            var card = UiKit.GetOrAdd<Image>(content);
            StripEffects(card.gameObject);
            UiKit.StylePanel(card);
            var contentRect = (RectTransform)content;
            UiKit.Place(contentRect, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 720f));

            TMP_Text status = StyleText(content, "StatusLabel", MinigameUiTheme.Title, MinigameUiTheme.Success);
            UiKit.Anchor(status.rectTransform, new Vector2(.06f, .80f), new Vector2(.94f, .95f));
            TMP_Text caption = StyleText(content, "ScoreCaption", MinigameUiTheme.Caption,
                MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .7f));
            UiKit.Anchor(caption.rectTransform, new Vector2(.06f, .70f), new Vector2(.94f, .78f));
            caption.text = VietText.Fix(ResultPanel.ScoreCaptionText);
            TMP_Text score = StyleText(content, "ScoreLabel", MinigameUiTheme.Display, MinigameUiTheme.Accent, true);
            UiKit.Anchor(score.rectTransform, new Vector2(.06f, .46f), new Vector2(.94f, .70f));
            TMP_Text rank = StyleText(content, "RankLabel", MinigameUiTheme.Headline, MinigameUiTheme.TextPrimary);
            UiKit.Anchor(rank.rectTransform, new Vector2(.06f, .36f), new Vector2(.94f, .46f));

            TMP_Text detail = EnsureLabel(content, "DetailLabel", MinigameUiTheme.Body, MinigameUiTheme.TextPrimary, .29f, .36f);
            TMP_Text lives = EnsureLabel(content, "LivesLabel", MinigameUiTheme.Caption, MinigameUiTheme.TextPrimary, .23f, .29f);
            TMP_Text error = EnsureLabel(content, "ErrorLabel", MinigameUiTheme.Caption, MinigameUiTheme.Energy, .17f, .23f);
            detail.gameObject.SetActive(false);
            lives.gameObject.SetActive(false);
            error.text = VietText.Fix(string.Empty);

            var action = content.Find("ActionButton").GetComponent<Button>();
            UiKit.StyleButton(action, ButtonVariant.Primary, "TIẾP TỤC");
            UiKit.Anchor((RectTransform)action.transform, new Vector2(.25f, .035f), new Vector2(.75f, .16f));

            Transform existingRetry = content.Find("RetryButton");
            Button retry = existingRetry != null
                ? existingRetry.GetComponent<Button>()
                : UiKit.Button(content, "RetryButton", "CHƠI LẠI", ButtonVariant.Primary).Button;
            UiKit.StyleButton(retry, ButtonVariant.Primary, "CHƠI LẠI");
            UiKit.Anchor((RectTransform)retry.transform, new Vector2(.06f, .035f), new Vector2(.48f, .16f));
            retry.gameObject.SetActive(false);

            var panel = new SerializedObject(root.GetComponent<ResultPanel>());
            panel.FindProperty("detailLabel").objectReferenceValue = detail;
            panel.FindProperty("livesLabel").objectReferenceValue = lives;
            panel.FindProperty("errorLabel").objectReferenceValue = error;
            panel.FindProperty("retryButton").objectReferenceValue = retry;
            panel.ApplyModifiedPropertiesWithoutUndo();
        }

        static TMP_Text EnsureLabel(Transform parent, string name, float size, Color color, float minY, float maxY)
        {
            Transform existing = parent.Find(name);
            TMP_Text label = existing != null
                ? existing.GetComponent<TMP_Text>()
                : UiKit.Label(parent, name, string.Empty, size, color);
            UiKit.StyleLabel(label, size, color);
            UiKit.Anchor(label.rectTransform, new Vector2(.06f, minY), new Vector2(.94f, maxY));
            return label;
        }

        static TMP_Text StyleText(Transform root, string path, float size, Color color, bool outline = false)
        {
            var label = root.Find(path).GetComponent<TMP_Text>();
            UiKit.StyleLabel(label, size, color, outline);
            return label;
        }

        static void StyleButton(Transform root, string path, ButtonVariant variant) =>
            UiKit.StyleButton(root.Find(path).GetComponent<Button>(), variant);

        static void RemoveImage(GameObject node)
        {
            StripEffects(node);
            var image = node.GetComponent<Image>();
            if (image != null)
                Object.DestroyImmediate(image);
        }

        static void StripEffects(GameObject node)
        {
            foreach (Shadow effect in node.GetComponents<Shadow>())
                Object.DestroyImmediate(effect);
        }
    }
}
#endif
