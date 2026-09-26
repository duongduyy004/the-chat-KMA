using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballResultPanel : MonoBehaviour, IRetryResultPreviewPanel
    {
        [SerializeField] GameObject contentRoot;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text goalsLabel;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] TMP_Text livesLabel;
        [SerializeField] TMP_Text errorLabel;
        [SerializeField] Button continueButton;
        [SerializeField] Button retryButton;

        MinigameResult currentResult;
        string previewRoute;
        int goals;
        int remainingLives;
        bool actionPending;
        bool actionDispatched;
        bool retryAvailable;
        bool listenersBound;

        public event Action<string> ActionRequested;
        public bool IsVisible => contentRoot ? contentRoot.activeInHierarchy : gameObject.activeInHierarchy;
        public bool IsActionPending => actionPending;
        public bool RetryAvailable => retryAvailable;
        public bool ContinueInteractable => continueButton && continueButton.interactable;
        public MinigameResult CurrentResult => currentResult;

        public void Configure(GameObject content, TMP_Text status, TMP_Text goalsText, TMP_Text score,
            TMP_Text rank, TMP_Text lives, Button continueAction, Button retryAction, TMP_Text error = null)
        {
            contentRoot = content;
            statusLabel = status;
            goalsLabel = goalsText;
            scoreLabel = score;
            rankLabel = rank;
            livesLabel = lives;
            continueButton = continueAction;
            retryButton = retryAction;
            errorLabel = error;
            BindButtons();
        }

        public bool ValidateReferences() => contentRoot && statusLabel && goalsLabel && scoreLabel && rankLabel &&
            livesLabel && continueButton && retryButton;

        public void SetGoals(int count) => goals = Mathf.Clamp(count, 0, 5);

        public void ConfigureRetry(int livesAfterFailure)
        {
            remainingLives = Mathf.Max(0, livesAfterFailure);
            retryAvailable = remainingLives > 0;
            if (retryButton) retryButton.gameObject.SetActive(retryAvailable);
            if (livesLabel) livesLabel.text = remainingLives.ToString();
        }

        public void Show(MinigameResult result, string nextRoute)
        {
            currentResult = result ?? throw new ArgumentNullException(nameof(result));
            previewRoute = nextRoute ?? string.Empty;
            actionPending = false;
            actionDispatched = false;
            if (errorLabel) errorLabel.text = string.Empty;
            if (contentRoot) contentRoot.SetActive(true);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (statusLabel) statusLabel.text = result.Pass ? "CHIẾN THẮNG" : "THẤT BẠI";
            if (goalsLabel) goalsLabel.text = $"{goals}/5";
            if (scoreLabel) scoreLabel.text = Mathf.RoundToInt(result.Score).ToString();
            if (rankLabel) rankLabel.text = $"XẾP HẠNG {result.Rank}";
            if (continueButton)
            {
                continueButton.interactable = true;
                SetButtonLabel(continueButton, "TIẾP TỤC");
            }
            retryAvailable = !result.Pass && remainingLives > 0;
            if (retryButton)
            {
                retryButton.gameObject.SetActive(retryAvailable);
                retryButton.interactable = true;
                SetButtonLabel(retryButton, "CHƠI LẠI");
            }
        }

        public void Continue() => RequestAction(ResultPanelActions.Continue);
        public void Retry()
        {
            if (retryAvailable)
                RequestAction(ResultPanelActions.Retry);
        }

        public void SetActionPending(bool pending, string error)
        {
            actionPending = pending;
            if (errorLabel) errorLabel.text = error ?? string.Empty;
            if (continueButton) continueButton.interactable = !pending;
            if (retryButton) retryButton.interactable = !pending && retryAvailable;
            if (!pending)
                actionDispatched = false;
        }

        void Awake() => BindButtons();
        void OnEnable() => BindButtons();

        void OnDisable()
        {
            if (!listenersBound) return;
            if (continueButton) continueButton.onClick.RemoveListener(Continue);
            if (retryButton) retryButton.onClick.RemoveListener(Retry);
            listenersBound = false;
        }

        void Update()
        {
            if (IsVisible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Continue();
        }

        void BindButtons()
        {
            if (listenersBound) return;
            if (continueButton) continueButton.onClick.AddListener(Continue);
            if (retryButton) retryButton.onClick.AddListener(Retry);
            listenersBound = continueButton || retryButton;
        }

        void RequestAction(string action)
        {
            if (!IsVisible || actionPending || actionDispatched || currentResult == null)
                return;
            actionDispatched = true;
            SetActionPending(true, null);
            ActionRequested?.Invoke(action);
        }

        static void SetButtonLabel(Button button, string value)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label) label.text = value;
        }
    }
}
