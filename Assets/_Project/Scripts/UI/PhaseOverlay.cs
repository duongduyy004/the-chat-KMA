using KMA.Gameplay;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class PhaseOverlay : MonoBehaviour
    {
        const float CountdownDuration = 3f;

        [SerializeField] TutorialOverlay tutorialOverlay;
        [SerializeField] GameObject tutorialRoot;
        [SerializeField] GameObject countdownRoot;
        [SerializeField] GameObject playRoot;
        [SerializeField] GameObject resolveRoot;
        [SerializeField] TMP_Text phaseLabel;
        [SerializeField] TMP_Text countdownLabel;
        [SerializeField] MinigameBase minigameSource;

        MinigameBase source;
        bool subscribed;
        bool tutorialSubscribed;
        float countdownElapsed;

        public MinigamePhase DisplayedPhase { get; private set; } = MinigamePhase.Tutorial;
        public bool IsTutorialVisible => tutorialRoot != null && tutorialRoot.activeSelf;
        public bool IsPlayVisible => playRoot != null && playRoot.activeSelf;
        public string CountdownText => countdownLabel == null ? string.Empty : countdownLabel.text;

        public void Bind(MinigameBase minigame)
        {
            Unsubscribe();
            minigameSource = minigame;
            source = minigame;
            Subscribe();
            ConfigureTutorial();
            ApplyPhase(source == null ? MinigamePhase.Tutorial : source.PresentationPhase);
        }

        void OnEnable()
        {
            if (source == null && minigameSource != null)
                source = minigameSource;
            Subscribe();
            ConfigureTutorial();
            if (source != null)
                ApplyPhase(source.PresentationPhase);
        }

        void OnDisable() => Unsubscribe();

        void Update()
        {
            if (DisplayedPhase != MinigamePhase.Countdown)
                return;

            countdownElapsed += Time.deltaTime;
            RefreshCountdown();
        }

        void Subscribe()
        {
            if (source == null || subscribed)
                return;
            source.PhaseChanged += ApplyPhase;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (source != null && subscribed)
                source.PhaseChanged -= ApplyPhase;
            subscribed = false;
            UnsubscribeTutorialCompletion();
        }

        void ApplyPhase(MinigamePhase phase)
        {
            DisplayedPhase = phase;
            if (phase == MinigamePhase.Countdown)
                countdownElapsed = 0f;

            SetActive(tutorialRoot, phase == MinigamePhase.Tutorial &&
                (tutorialOverlay == null || tutorialOverlay.ShouldShow));
            SetActive(countdownRoot, phase == MinigamePhase.Countdown);
            SetActive(playRoot, phase == MinigamePhase.Play);
            SetActive(resolveRoot, phase == MinigamePhase.Resolve);

            if (phaseLabel != null)
                phaseLabel.text = phase.ToString().ToUpperInvariant();
            RefreshCountdown();
        }

        void ConfigureTutorial()
        {
            if (tutorialOverlay == null || source == null)
                return;

            UnsubscribeTutorialCompletion();

            if (source.GetType().Name == "SprintController")
            {
                tutorialOverlay.Show("Sprint", new List<TutorialStep>
                {
                    new TutorialStep("LEFT / RIGHT", "Tap the shown side"),
                    new TutorialStep("WIND CUE", "Counter the wind before the window closes")
                });
            }
            else if (source.GetType().Name == "EnduranceController")
            {
                tutorialOverlay.Show("Endurance", new List<TutorialStep>
                {
                    new TutorialStep("RHYTHM", "Tap on the beat"),
                    new TutorialStep("RECOVER", "Hold to recover stamina"),
                    new TutorialStep("OBSTACLES", "Swipe up/down to clear obstacles")
                });
            }

            if (tutorialOverlay.ShouldShow)
            {
                tutorialOverlay.Completed += ReleaseTutorialGate;
                tutorialSubscribed = true;
                source.SetTutorialGate(true);
            }
            else
            {
                source.SetTutorialGate(false);
            }
        }

        void UnsubscribeTutorialCompletion()
        {
            if (tutorialOverlay != null && tutorialSubscribed)
                tutorialOverlay.Completed -= ReleaseTutorialGate;
            tutorialSubscribed = false;
        }

        void ReleaseTutorialGate() => source?.SetTutorialGate(false);

        void RefreshCountdown()
        {
            if (countdownLabel == null)
                return;
            var remaining = Mathf.Clamp(Mathf.CeilToInt(CountdownDuration - countdownElapsed), 1, 3);
            countdownLabel.text = DisplayedPhase == MinigamePhase.Countdown
                ? remaining.ToString()
                : string.Empty;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
