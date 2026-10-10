using System;
using System.Collections.Generic;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class TutorialOverlay : MonoBehaviour
    {
        [SerializeField] GameObject contentRoot;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text instructionLabel;
        [SerializeField] TMP_Text stepLabel;
        [SerializeField] Image iconImage;
        [SerializeField] Button backButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button skipButton;
        [SerializeField] Button closeButton;

        readonly List<TutorialStep> steps = new List<TutorialStep>();
        ITutorialSeenStore seenStore;
        string subjectId = string.Empty;

        public int CurrentIndex { get; private set; }
        public bool ShouldShow { get; private set; }
        public bool CanGoBack => ShouldShow && CurrentIndex > 0;
        public bool CanGoNext => ShouldShow && CurrentIndex < steps.Count - 1;
        public TutorialStep CurrentStep => ShouldShow && CurrentIndex < steps.Count ? steps[CurrentIndex] : null;

        public event Action Completed;

        void Awake()
        {
            if (seenStore == null)
                seenStore = new SaveDataTutorialSeenStore();
            ApplyKitStyle();
            WireButtons();
            Refresh();
        }

        public void Show(string newSubjectId, IReadOnlyList<TutorialStep> newSteps)
        {
            if (seenStore == null)
                seenStore = new SaveDataTutorialSeenStore();
            subjectId = newSubjectId ?? string.Empty;
            steps.Clear();
            if (newSteps != null)
            {
                for (var index = 0; index < newSteps.Count; index++)
                {
                    if (newSteps[index] != null)
                        steps.Add(newSteps[index]);
                }
            }

            CurrentIndex = 0;
            ShouldShow = steps.Count > 0 && !seenStore.HasSeen(subjectId);
            Refresh();
        }

        public void ConfigureForTest(
            ITutorialSeenStore store,
            string newSubjectId,
            IReadOnlyList<TutorialStep> newSteps)
        {
            seenStore = store ?? throw new ArgumentNullException(nameof(store));
            Show(newSubjectId, newSteps);
        }

        public void Next()
        {
            if (!CanGoNext)
                return;
            CurrentIndex++;
            Refresh();
        }

        public void Back()
        {
            if (!CanGoBack)
                return;
            CurrentIndex--;
            Refresh();
        }

        public void Skip() => Complete();

        public void Close() => Complete();

        void Complete()
        {
            if (!ShouldShow)
                return;
            seenStore.MarkSeen(subjectId);
            ShouldShow = false;
            Refresh();
            Completed?.Invoke();
        }

        void Refresh()
        {
            if (contentRoot != null)
                contentRoot.SetActive(ShouldShow);

            var step = CurrentStep;
            if (titleLabel != null)
                titleLabel.text = VietText.Fix(step?.Title ?? string.Empty);
            if (instructionLabel != null)
                instructionLabel.text = VietText.Fix(step?.Instruction ?? string.Empty);
            if (stepLabel != null)
                stepLabel.text = VietText.Fix(ShouldShow ? $"{CurrentIndex + 1} / {steps.Count}" : string.Empty);
            if (iconImage != null)
            {
                iconImage.sprite = step?.Icon;
                iconImage.enabled = step?.Icon != null;
            }
            if (backButton != null)
                backButton.interactable = CanGoBack;
            if (nextButton != null)
                nextButton.interactable = CanGoNext;
            if (skipButton != null)
                skipButton.interactable = ShouldShow;
            if (closeButton != null)
                closeButton.interactable = ShouldShow && !CanGoNext;
        }

        void WireButtons()
        {
            if (backButton != null)
                backButton.onClick.AddListener(Back);
            if (nextButton != null)
                nextButton.onClick.AddListener(Next);
            if (skipButton != null)
                skipButton.onClick.AddListener(Skip);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        void ApplyKitStyle()
        {
            if (contentRoot != null)
            {
                Image card = contentRoot.GetComponent<Image>();
                if (card != null)
                    UiKit.StylePanel(card);
                if (contentRoot.transform is RectTransform rect)
                    rect.sizeDelta = new Vector2(900f, 600f);
            }

            StyleLabel(titleLabel, MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            StyleLabel(instructionLabel, MinigameUiTheme.Body, MinigameUiTheme.TextPrimary);
            if (instructionLabel != null)
                UiKit.FitLabel(instructionLabel, MinigameUiTheme.Body);
            StyleLabel(stepLabel, MinigameUiTheme.Caption, MinigameUiTheme.Accent);
            StyleButton(backButton, "QUAY LẠI", ButtonVariant.Secondary);
            StyleButton(nextButton, "TIẾP", ButtonVariant.Primary);
            StyleButton(skipButton, "BỎ QUA", ButtonVariant.Secondary);
            StyleButton(closeButton, "BẮT ĐẦU", ButtonVariant.Primary);
        }

        static void StyleLabel(TMP_Text label, float size, Color color)
        {
            if (label != null)
                UiKit.StyleLabel(label, size, color);
        }

        static void StyleButton(Button button, string value, ButtonVariant variant)
        {
            if (button != null)
                UiKit.StyleButton(button, variant, value);
        }
    }
}
