using System;
using System.Collections;
using KMA.Gameplay;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    /// The shared result screen of every minigame. Tiếp tục raises the preview route; Chơi lại
    /// (only when a retry is configured) raises ResultPanelActions.Retry.
    public sealed class ResultPanel : MonoBehaviour, IRetryResultPreviewPanel
    {
        const float ScrimDuration = .12f;
        const float ModalDuration = .18f;
        const float TitleDuration = .14f;
        const float ScoreDuration = .35f;
        const float RankDuration = .16f;

        [SerializeField] GameObject contentRoot;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text detailLabel;
        [SerializeField] TMP_Text livesLabel;
        [SerializeField] TMP_Text errorLabel;
        [SerializeField] Button actionButton;
        [SerializeField] Button retryButton;

        string successTitle = "CHIẾN THẮNG";
        string failureTitle = "THẤT BẠI";
        int remainingLives;
        bool retryConfigured;
        bool retryAvailable;
        bool actionPending;
        bool listenersBound;
        string finalScoreText = string.Empty;

        public event Action<string> ActionRequested;

        public MinigameResult CurrentResult { get; private set; }
        public string PreviewRoute { get; private set; } = string.Empty;
        public bool HasContinued { get; private set; }
        public bool RetryAvailable => retryAvailable;
        public bool IsActionPending => actionPending;
        public bool IsVisible => contentRoot ? contentRoot.activeInHierarchy : gameObject.activeInHierarchy;
        public bool ContinueInteractable => actionButton && actionButton.interactable;
        public bool SupportsRetry => retryButton && detailLabel && livesLabel;

        /// Wires a panel built in code; prefab instances are wired by MinigamePrefabStyler.
        public void Configure(GameObject content, TMP_Text status, TMP_Text detail, TMP_Text score, TMP_Text rank,
            TMP_Text lives, Button continueAction, Button retryAction, TMP_Text error = null)
        {
            contentRoot = content;
            statusLabel = status;
            detailLabel = detail;
            scoreLabel = score;
            rankLabel = rank;
            livesLabel = lives;
            actionButton = continueAction;
            retryButton = retryAction;
            errorLabel = error;
            BindButtons();
            RefreshButtons();
        }

        public bool ValidateReferences() => contentRoot && statusLabel && scoreLabel && rankLabel && actionButton;

        public void SetTitles(string success, string failure)
        {
            successTitle = success ?? successTitle;
            failureTitle = failure ?? failureTitle;
        }

        public void SetDetail(string text)
        {
            if (detailLabel == null)
                return;
            detailLabel.text = text ?? string.Empty;
            detailLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void ConfigureRetry(int livesAfterFailure)
        {
            retryConfigured = true;
            remainingLives = Mathf.Max(0, livesAfterFailure);
            retryAvailable = remainingLives > 0;
            RefreshButtons();
        }

        public void Show(MinigameResult result, string previewRoute)
        {
            CurrentResult = result ?? throw new ArgumentNullException(nameof(result));
            PreviewRoute = previewRoute ?? string.Empty;
            HasContinued = false;
            actionPending = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null)
                contentRoot.SetActive(true);
            if (errorLabel != null)
                errorLabel.text = string.Empty;
            if (statusLabel != null)
            {
                statusLabel.text = result.Pass ? successTitle : failureTitle;
                statusLabel.color = result.Pass ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            finalScoreText = Mathf.RoundToInt(result.Score).ToString();
            if (scoreLabel != null)
                scoreLabel.text = finalScoreText;
            if (rankLabel != null)
                rankLabel.text = $"XẾP HẠNG {result.Rank}";
            if (livesLabel != null)
            {
                livesLabel.text = $"CÒN {remainingLives} MẠNG";
                livesLabel.gameObject.SetActive(retryConfigured);
            }
            SetButtonLabel(actionButton, "TIẾP TỤC");
            SetButtonLabel(retryButton, "CHƠI LẠI");

            retryAvailable = !result.Pass && remainingLives > 0;
            RefreshButtons();
            Reveal(result.Score);
        }

        public void Continue()
        {
            if (CurrentResult == null || HasContinued || actionPending)
                return;
            HasContinued = true;
            ActionRequested?.Invoke(PreviewRoute);
        }

        public void Retry()
        {
            if (CurrentResult == null || HasContinued || actionPending || !retryAvailable)
                return;
            HasContinued = true;
            ActionRequested?.Invoke(ResultPanelActions.Retry);
        }

        public void SetActionPending(bool pending, string error)
        {
            actionPending = pending;
            if (errorLabel != null)
                errorLabel.text = error ?? string.Empty;
            if (!pending)
                HasContinued = false;
            RefreshButtons();
        }

        void Awake() => BindButtons();
        void OnEnable() => BindButtons();

        void OnDisable()
        {
            if (!listenersBound)
                return;
            if (actionButton != null) actionButton.onClick.RemoveListener(Continue);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            listenersBound = false;
        }

        void Update()
        {
            if (IsVisible && CurrentResult != null && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
                Continue();
        }

        void BindButtons()
        {
            if (listenersBound)
                return;
            if (actionButton != null) actionButton.onClick.AddListener(Continue);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            listenersBound = actionButton != null || retryButton != null;
        }

        void RefreshButtons()
        {
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(retryAvailable);
                retryButton.interactable = !actionPending;
            }
            if (actionButton == null)
                return;
            actionButton.interactable = !actionPending;

            bool paired = retryAvailable && retryButton != null;
            var action = (RectTransform)actionButton.transform;
            action.anchorMin = new Vector2(paired ? .52f : .25f, action.anchorMin.y);
            action.anchorMax = new Vector2(paired ? .94f : .75f, action.anchorMax.y);
            if (actionButton.GetComponent<KitPressFeedback>() != null)
                UiKit.ApplyVariant(UiKit.ButtonParts(actionButton), paired ? ButtonVariant.Secondary : ButtonVariant.Primary);
        }

        static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
        }

        void Reveal(float finalScore)
        {
            StopAllCoroutines();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SnapRevealed();
                return;
            }
            StartCoroutine(AnimateReveal(finalScore));
        }

        CanvasGroup ScrimGroup()
        {
            Transform backdrop = transform.Find("Backdrop");
            return backdrop == null ? null : UiKit.GetOrAdd<CanvasGroup>(backdrop.gameObject);
        }

        CanvasGroup ModalGroup() => contentRoot == null ? null : UiKit.GetOrAdd<CanvasGroup>(contentRoot);

        void SnapRevealed()
        {
            Transform backdrop = transform.Find("Backdrop");
            CanvasGroup scrim = backdrop == null ? null : backdrop.GetComponent<CanvasGroup>();
            if (scrim != null) scrim.alpha = 1f;
            if (contentRoot != null)
            {
                CanvasGroup modal = contentRoot.GetComponent<CanvasGroup>();
                if (modal != null) modal.alpha = 1f;
                contentRoot.transform.localScale = Vector3.one;
            }
            if (rankLabel != null) rankLabel.rectTransform.localScale = Vector3.one;
            if (scoreLabel != null) scoreLabel.text = finalScoreText;
        }

        IEnumerator AnimateReveal(float finalScore)
        {
            yield return FadeGroup(ScrimGroup(), ScrimDuration);
            yield return ScaleAndFadeModal();
            yield return FadeText(statusLabel, TitleDuration);
            yield return CountUpScore(finalScore);
            yield return PopRank();
        }

        static IEnumerator FadeGroup(CanvasGroup group, float duration)
        {
            if (group == null)
                yield break;
            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            group.alpha = 1f;
        }

        IEnumerator ScaleAndFadeModal()
        {
            CanvasGroup group = ModalGroup();
            if (group == null)
                yield break;
            Transform modal = contentRoot.transform;
            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < ModalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ModalDuration);
                group.alpha = t;
                modal.localScale = Vector3.one * Mathf.Lerp(.9f, 1f, t);
                yield return null;
            }
            group.alpha = 1f;
            modal.localScale = Vector3.one;
        }

        static IEnumerator FadeText(TMP_Text label, float duration)
        {
            if (label == null)
                yield break;
            Color target = label.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                label.color = new Color(target.r, target.g, target.b, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            label.color = target;
        }

        IEnumerator CountUpScore(float finalScore)
        {
            if (scoreLabel == null)
                yield break;
            float elapsed = 0f;
            while (elapsed < ScoreDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ScoreDuration);
                scoreLabel.text = Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, t)).ToString();
                yield return null;
            }
            scoreLabel.text = finalScoreText;
        }

        IEnumerator PopRank()
        {
            if (rankLabel == null)
                yield break;
            RectTransform rect = rankLabel.rectTransform;
            float elapsed = 0f;
            while (elapsed < RankDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / RankDuration);
                float scale = t < .5f ? Mathf.Lerp(.6f, 1.2f, t / .5f) : Mathf.Lerp(1.2f, 1f, (t - .5f) / .5f);
                rect.localScale = Vector3.one * scale;
                yield return null;
            }
            rect.localScale = Vector3.one;
        }
    }
}
