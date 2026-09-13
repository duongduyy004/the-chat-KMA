using KMA.Gameplay;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public interface ISprintStartPresentation
    {
        void Bind(MinigameBase source);
    }

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

            SetActive(tutorialRoot, !IsSprintSource && phase == MinigamePhase.Tutorial &&
                (tutorialOverlay == null || tutorialOverlay.ShouldShow));
            SetActive(countdownRoot, phase == MinigamePhase.Countdown);
            SetActive(playRoot, phase == MinigamePhase.Play);
            SetActive(resolveRoot, phase == MinigamePhase.Resolve);

            if (phaseLabel != null)
                phaseLabel.text = PhaseName(phase);
            RefreshCountdown();
        }

        void ConfigureTutorial()
        {
            if (source == null)
                return;

            UnsubscribeTutorialCompletion();

            if (IsSprintSource)
            {
                FindSprintStartPresentation()?.Bind(source);
                return;
            }

            if (tutorialOverlay == null)
                return;

            if (source.GetType().Name == "EnduranceController")
            {
                tutorialOverlay.Show("Endurance", new List<TutorialStep>
                {
                    new TutorialStep("RHYTHM", "Tap on the beat"),
                    new TutorialStep("RECOVER", "Hold to recover stamina"),
                    new TutorialStep("OBSTACLES", "Swipe up/down to clear obstacles")
                });
            }
            else if (source.GetType().Name == "VolleyballController")
            {
                tutorialOverlay.Show("Volleyball", new List<TutorialStep>
                {
                    new TutorialStep("DIG", "Swipe down when the ball is low."),
                    new TutorialStep("SET", "Swipe up while the ball is rising."),
                    new TutorialStep("SPIKE", "Swipe toward the net near the apex.")
                });
            }
            else if (source.GetType().Name == "BasketballController")
            {
                tutorialOverlay.Show("Basketball", new List<TutorialStep>
                {
                    new TutorialStep("HOLD", "Hold to charge the lob."),
                    new TutorialStep("AIM", "Release inside the glowing charge band."),
                    new TutorialStep("FINISH", "Tap when the ball reaches the apex ring.")
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

        bool IsSprintSource => source != null && source.GetType().Name == "SprintController";

        static ISprintStartPresentation FindSprintStartPresentation()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < behaviours.Length; index++)
                if (behaviours[index] is ISprintStartPresentation presentation)
                    return presentation;
            return null;
        }

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

        static string PhaseName(MinigamePhase phase) => phase switch
        {
            MinigamePhase.Tutorial => "HƯỚNG DẪN",
            MinigamePhase.Countdown => "CHUẨN BỊ",
            MinigamePhase.Play => "TĂNG TỐC!",
            MinigamePhase.Resolve => "KẾT QUẢ",
            _ => string.Empty
        };
    }
}
