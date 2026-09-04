using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    [Serializable]
    public sealed class TutorialStep
    {
        [SerializeField] string title;
        [SerializeField] string instruction;
        [SerializeField] Sprite icon;
        [SerializeField] string animationKey;

        public string Title => title ?? string.Empty;
        public string Instruction => instruction ?? string.Empty;
        public Sprite Icon => icon;
        public string AnimationKey => animationKey ?? string.Empty;

        public TutorialStep(string title, string instruction, Sprite icon = null, string animationKey = null)
        {
            this.title = title;
            this.instruction = instruction;
            this.icon = icon;
            this.animationKey = animationKey;
        }
    }

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
                titleLabel.text = step?.Title ?? string.Empty;
            if (instructionLabel != null)
                instructionLabel.text = step?.Instruction ?? string.Empty;
            if (stepLabel != null)
                stepLabel.text = ShouldShow ? $"{CurrentIndex + 1} / {steps.Count}" : string.Empty;
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
    }
}
