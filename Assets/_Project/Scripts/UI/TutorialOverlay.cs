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
            ApplyFestivalStyle();
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

        void ApplyFestivalStyle()
        {
            if (contentRoot != null)
            {
                Image card = contentRoot.GetComponent<Image>();
                if (card != null)
                    card.color = new Color32(255, 249, 231, 255);
                Outline outline = contentRoot.GetComponent<Outline>() ?? contentRoot.AddComponent<Outline>();
                outline.effectColor = new Color32(8, 35, 61, 255);
                outline.effectDistance = new Vector2(5f, -5f);
                if (contentRoot.transform is RectTransform rect)
                    rect.sizeDelta = new Vector2(900f, 600f);
            }

            StyleLabel(titleLabel, 42f, new Color32(8, 35, 61, 255));
            StyleLabel(instructionLabel, 25f, new Color32(62, 79, 96, 255));
            StyleLabel(stepLabel, 18f, new Color32(25, 130, 196, 255));
            StyleButton(backButton, "QUAY LẠI", new Color32(82, 96, 112, 255));
            StyleButton(nextButton, "TIẾP", new Color32(255, 202, 58, 255));
            StyleButton(skipButton, "BỎ QUA", new Color32(255, 89, 94, 255));
            StyleButton(closeButton, "BẮT ĐẦU", new Color32(54, 112, 72, 255));
        }

        static void StyleLabel(TMP_Text label, float size, Color color)
        {
            if (label == null)
                return;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
        }

        static void StyleButton(Button button, string value, Color color)
        {
            if (button == null)
                return;
            Image image = button.GetComponent<Image>();
            if (image != null)
                image.color = color;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = value;
                label.fontSize = 20f;
                label.fontStyle = FontStyles.Bold;
                label.color = Color.white;
            }
            Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(8, 35, 61, 255);
            outline.effectDistance = new Vector2(3f, -3f);
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(255, 237, 176, 255);
            colors.pressedColor = new Color32(225, 180, 50, 255);
            colors.fadeDuration = .08f;
            button.colors = colors;
        }
    }
}
