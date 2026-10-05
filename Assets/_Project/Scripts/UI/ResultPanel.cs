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
    public sealed class ResultPanel : MonoBehaviour, IRetryResultPreviewPanel, IChallengeResultPanel,
        IFrogJumpResultPanel
    {
        static float ScrimDuration => Mathf.Max(0f, UITheme.Shared.Motion.resultScrim);
        static float ModalDuration => Mathf.Max(0f, UITheme.Shared.Motion.resultModal);
        static float TitleDuration => Mathf.Max(0f, UITheme.Shared.Motion.resultTitle);
        static float ScoreDuration => Mathf.Max(0f, UITheme.Shared.Motion.resultScore);
        static float RankDuration => Mathf.Max(0f, UITheme.Shared.Motion.resultRank);

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
        bool frogMode;
        string finalScoreText = string.Empty;
        ChallengeAttemptContext challengeContext;
        string challengeSaveError;
        JourneyCommitOutcome? challengeOutcome;
        ChallengeAttemptResult challengeResult;

        public event Action<string> ActionRequested;
        public event Action<JourneyResultAction> JourneyActionRequested;
        public event Action FrogJumpContinueRequested;

        public MinigameResult CurrentResult { get; private set; }
        public string PreviewRoute { get; private set; } = string.Empty;
        public bool HasContinued { get; private set; }
        public bool RetryAvailable => retryAvailable;
        public bool IsActionPending => actionPending;
        public bool IsVisible => contentRoot ? contentRoot.activeInHierarchy : gameObject.activeInHierarchy;
        public bool ContinueInteractable => actionButton && actionButton.interactable;
        public bool SupportsRetry => retryButton && detailLabel && livesLabel;
        /// The main button's caption exactly as SetButtonLabel wrote it (VietText-normalized).
        public string ContinueLabel => actionButton != null
            ? actionButton.GetComponentInChildren<TMP_Text>(true)?.text : null;

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
            detailLabel.text = VietText.Fix(text ?? string.Empty);
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
            frogMode = false;
            challengeContext = null;
            challengeSaveError = null;
            challengeOutcome = null;
            challengeResult = null;
            CurrentResult = result ?? throw new ArgumentNullException(nameof(result));
            PreviewRoute = previewRoute ?? string.Empty;
            HasContinued = false;
            actionPending = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null)
                contentRoot.SetActive(true);
            ApplyTheme();
            if (errorLabel != null)
                errorLabel.text = VietText.Fix(string.Empty);
            if (statusLabel != null)
            {
                statusLabel.text = VietText.Fix(result.Pass ? successTitle : failureTitle);
                statusLabel.color = result.Pass ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            finalScoreText = Mathf.RoundToInt(result.Score).ToString();
            if (scoreLabel != null)
                scoreLabel.text = VietText.Fix(finalScoreText);
            if (rankLabel != null)
                rankLabel.text = VietText.Fix($"XẾP HẠNG {result.Rank}");
            if (livesLabel != null)
            {
                livesLabel.text = VietText.Fix($"CÒN {remainingLives} MẠNG");
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
            if (frogMode)
            {
                FrogJumpContinueRequested?.Invoke();
                return;
            }
            if (challengeContext != null)
            {
                JourneyActionRequested?.Invoke(challengeSaveError != null && !challengeOutcome.HasValue
                    ? JourneyResultAction.RetrySave
                    : challengeOutcome.HasValue && challengeOutcome.Value.FrogJumpRequired
                        ? JourneyResultAction.FrogJump : JourneyResultAction.Continue);
                return;
            }
            ActionRequested?.Invoke(PreviewRoute);
        }

        public void Retry()
        {
            if (CurrentResult == null || HasContinued || actionPending || !retryAvailable)
                return;
            HasContinued = true;
            if (challengeContext != null)
            {
                JourneyActionRequested?.Invoke(JourneyResultAction.Retry);
                return;
            }
            ActionRequested?.Invoke(ResultPanelActions.Retry);
        }

        public void ShowChallenge(ChallengeAttemptContext context, ChallengeAttemptResult result,
            JourneyCommitOutcome? outcome, string saveError)
        {
            frogMode = false;
            challengeContext = context;
            challengeSaveError = outcome.HasValue ? null : saveError;
            challengeOutcome = outcome;
            challengeResult = result;
            var display = result.ExamResult ?? new MinigameResult(result.Pass,
                result.Metrics.CompletedTargets, result.Pass ? Rank.C : Rank.F);
            ShowChallengeCore(context, display, result, outcome, saveError);
        }

        void ShowChallengeCore(ChallengeAttemptContext context, MinigameResult display,
            ChallengeAttemptResult result, JourneyCommitOutcome? outcome, string saveError)
        {
            CurrentResult = display;
            PreviewRoute = string.Empty;
            HasContinued = false;
            actionPending = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null) contentRoot.SetActive(true);
            ApplyTheme();
            if (statusLabel != null)
            {
                bool practice = context.Mode != ChallengeAttemptMode.Journey;
                statusLabel.text = VietText.Fix(result.Pass ? practice ? "LUYỆN TẬP HOÀN THÀNH" : successTitle
                    : failureTitle);
                statusLabel.color = result.Pass ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            finalScoreText = result.ExamResult == null
                ? result.Metrics.CompletedTargets.ToString()
                : Mathf.RoundToInt(result.ExamResult.Score).ToString();
            if (scoreLabel != null) scoreLabel.text = VietText.Fix(finalScoreText);
            if (rankLabel != null)
            {
                rankLabel.text = result.ExamResult == null
                    ? VietText.Fix($"{result.Metrics.CompletedTargets} MỤC TIÊU")
                    : VietText.Fix($"XẾP HẠNG {result.ExamResult.Rank}");
                rankLabel.gameObject.SetActive(result.ExamResult != null);
            }
            bool frogJump = outcome.HasValue && outcome.Value.FrogJumpRequired;
            SetDetail(saveError ?? (context.Mode == ChallengeAttemptMode.Journey
                ? outcome.HasValue
                    ? frogJump
                        ? outcome.Value.FrogJumpSavesLife
                            ? "Về đích trong 60 s để giữ lượt thi"
                            : "−1 lượt thi. Bật cóc xong mới được thi lại"
                        : "Kết quả đã lưu"
                    : "Kết quả đang chờ lưu"
                : $"Thời gian {result.Metrics.Elapsed:0.0}s"));
            if (errorLabel != null) errorLabel.text = VietText.Fix(saveError ?? string.Empty);
            SetButtonLabel(actionButton, saveError == null ? frogJump ? "BẬT CÓC" : "TIẾP TỤC"
                : outcome.HasValue ? "THỬ LẠI" : "LƯU LẠI");
            retryAvailable = result.ExamResult != null && !result.Pass && outcome.HasValue &&
                outcome.Value.AttemptsRemaining > 0 && !frogJump;
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(retryAvailable);
                SetButtonLabel(retryButton, "THI LẠI");
            }
            if (livesLabel != null)
            {
                int attemptsRemaining = outcome.HasValue ? outcome.Value.AttemptsRemaining : 0;
                livesLabel.text = VietText.Fix($"LƯỢT THI: {attemptsRemaining}/{GameSession.MaxLives}");
                livesLabel.gameObject.SetActive(result.ExamResult != null || frogJump);
            }
            RefreshButtons();
            Reveal(result.ExamResult == null ? result.Metrics.CompletedTargets : result.ExamResult.Score);
        }

        public void ShowFrogJump(FrogJumpResultView view)
        {
            frogMode = true;
            challengeContext = null;
            challengeSaveError = null;
            challengeOutcome = null;
            challengeResult = null;
            CurrentResult = new MinigameResult(view.ReachedFinish, 0f, view.ReachedFinish ? Rank.C : Rank.F);
            PreviewRoute = string.Empty;
            HasContinued = false;
            actionPending = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (contentRoot != null) contentRoot.SetActive(true);
            ApplyTheme();
            if (statusLabel != null)
            {
                statusLabel.text = VietText.Fix(view.ReachedFinish ? "VỀ ĐÍCH!" : "HẾT GIỜ!");
                statusLabel.color = view.ReachedFinish ? MinigameUiTheme.Success : MinigameUiTheme.Energy;
            }
            // The frog jump has no score: blank the score, hide the rank, and reveal without the
            // count-up so no stray "0" is ever written.
            finalScoreText = string.Empty;
            if (scoreLabel != null) scoreLabel.text = string.Empty;
            if (rankLabel != null) rankLabel.gameObject.SetActive(false);
            SetDetail(!view.SavesLife ? "Lượt thi đã bị trừ khi trượt bài"
                : view.ReachedFinish ? "Giữ được lượt thi" : "−1 lượt thi");
            if (livesLabel != null)
            {
                livesLabel.text = VietText.Fix($"LƯỢT THI: {view.LivesRemaining}/{GameSession.MaxLives}");
                livesLabel.gameObject.SetActive(true);
            }
            if (errorLabel != null) errorLabel.text = VietText.Fix(view.Error ?? string.Empty);
            SetButtonLabel(actionButton, view.LivesRemaining > 0 ? "THI LẠI" : "VỀ BẢN ĐỒ");
            retryAvailable = false;
            RefreshButtons();
            Reveal(0f, scoreless: true);
        }

        public void SetActionPending(bool pending, string error)
        {
            actionPending = pending;
            if (errorLabel != null)
                errorLabel.text = VietText.Fix(error ?? string.Empty);
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

        void ApplyTheme()
        {
            Image backdrop = transform.Find("Backdrop")?.GetComponent<Image>();
            if (backdrop != null) backdrop.color = MinigameUiTheme.Scrim;
            Image card = contentRoot != null ? contentRoot.GetComponent<Image>() : null;
            if (card != null) UiKit.StylePanel(card);
            if (scoreLabel != null) scoreLabel.color = MinigameUiTheme.Accent;
            if (rankLabel != null) rankLabel.color = MinigameUiTheme.TextPrimary;
            if (detailLabel != null) detailLabel.color = MinigameUiTheme.TextPrimary;
            if (livesLabel != null) livesLabel.color = MinigameUiTheme.TextPrimary;
            if (errorLabel != null) errorLabel.color = MinigameUiTheme.Energy;
            TMP_Text caption = contentRoot != null
                ? contentRoot.transform.Find("ScoreCaption")?.GetComponent<TMP_Text>() : null;
            if (caption != null)
            {
                caption.color = MinigameUiTheme.WithAlpha(MinigameUiTheme.TextPrimary, .7f);
                // The frog jump card has no score, so its caption would float over nothing.
                caption.gameObject.SetActive(!frogMode);
            }
            if (actionButton != null) UiKit.StyleButton(actionButton, ButtonVariant.Primary);
            if (retryButton != null) UiKit.StyleButton(retryButton, ButtonVariant.Primary);
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
                label.text = VietText.Fix(value);
        }

        void Reveal(float finalScore, bool scoreless = false)
        {
            StopAllCoroutines();
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                SnapRevealed();
                return;
            }
            PrepareReveal(scoreless);
            StartCoroutine(AnimateReveal(finalScore, scoreless));
        }

        /// Puts every animated element in its pre-stage state so nothing shows before its turn.
        void PrepareReveal(bool scoreless)
        {
            CanvasGroup scrim = ScrimGroup();
            if (scrim != null) scrim.alpha = 0f;
            CanvasGroup modal = ModalGroup();
            if (modal != null)
            {
                modal.alpha = 0f;
                contentRoot.transform.localScale = Vector3.one * .9f;
            }
            if (statusLabel != null)
            {
                Color c = statusLabel.color;
                statusLabel.color = new Color(c.r, c.g, c.b, 0f);
            }
            if (scoreLabel != null) scoreLabel.text = scoreless ? string.Empty : VietText.Fix("0");
            if (rankLabel != null) rankLabel.rectTransform.localScale = Vector3.one * .6f;
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
            if (scoreLabel != null) scoreLabel.text = VietText.Fix(finalScoreText);
        }

        IEnumerator AnimateReveal(float finalScore, bool scoreless)
        {
            yield return FadeGroup(ScrimGroup(), ScrimDuration);
            yield return ScaleAndFadeModal();
            yield return FadeText(statusLabel, TitleDuration);
            if (scoreless)
                yield break;
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
            target.a = 1f;
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
                scoreLabel.text = VietText.Fix(Mathf.RoundToInt(Mathf.Lerp(0f, finalScore, t)).ToString());
                yield return null;
            }
            scoreLabel.text = VietText.Fix(finalScoreText);
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
