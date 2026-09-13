using TMPro;
using UnityEngine;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    public sealed class SprintStartPresentation : MonoBehaviour, ISprintStartPresentation
    {
        const float TutorialDuration = 1.5f;
        const float PlayInstructionDuration = .25f;
        const float GoDuration = .25f;

        [SerializeField] GameObject tutorialRoot;
        [SerializeField] TMP_Text tutorialLabel;
        [SerializeField] GameObject countdownRoot;
        [SerializeField] TMP_Text countdownLabel;
        [SerializeField] GameObject instructionRoot;
        [SerializeField] TMP_Text instructionLabel;

        SprintController controller;
        CanvasGroup instructionCanvasGroup;
        float tutorialElapsed;
        float countdownElapsed;
        float instructionRemaining;
        float goRemaining;
        bool tutorialReleased;
        string countdownText = string.Empty;
        string instructionText = string.Empty;

        public bool TutorialVisible => controller != null && !tutorialReleased &&
            controller.PresentationPhase == MinigamePhase.Tutorial;
        public string TutorialText => TutorialCopy;
        public string CountdownText => countdownLabel == null ? countdownText : countdownLabel.text;
        public bool InstructionVisible => controller != null && instructionRemaining > 0f &&
            controller.PresentationPhase == MinigamePhase.Play;
        public string InstructionText => instructionLabel == null ? instructionText : instructionLabel.text;
        public const string TutorialCopy = "← TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI →";

        void OnDisable() => Unsubscribe();
        void OnDestroy() => Unsubscribe();
        void Update() => Tick(Time.unscaledDeltaTime);

        public void Configure(GameObject tutorial, TMP_Text tutorialText, GameObject countdown,
            TMP_Text countdownText, GameObject instruction, TMP_Text instructionText)
        {
            tutorialRoot = tutorial;
            tutorialLabel = tutorialText;
            countdownRoot = countdown;
            countdownLabel = countdownText;
            instructionRoot = instruction;
            instructionLabel = instructionText;
            instructionCanvasGroup = instructionRoot == null ? null :
                instructionRoot.GetComponent<CanvasGroup>() ?? instructionRoot.AddComponent<CanvasGroup>();
            SetActive(tutorialRoot, false);
            SetActive(countdownRoot, false);
            SetInstructionActive(false);
        }

        public void Bind(SprintController source)
        {
            Unsubscribe();
            controller = source;
            tutorialElapsed = 0f;
            countdownElapsed = 0f;
            instructionRemaining = 0f;
            goRemaining = 0f;
            tutorialReleased = false;
            countdownText = string.Empty;
            instructionText = string.Empty;

            if (controller == null)
            {
                SetActive(tutorialRoot, false);
                SetActive(countdownRoot, false);
                SetInstructionActive(false);
                return;
            }

            controller.PhaseChanged += ApplyPhase;
            controller.SetTutorialGate(true);
            ApplyPhase(controller.PresentationPhase);
        }

        void ISprintStartPresentation.Bind(MinigameBase source)
        {
            Bind(source as SprintController);
        }

        public void TickForTest(float deltaTime) => Tick(deltaTime);

        void Tick(float deltaTime)
        {
            if (controller == null)
                return;

            float elapsed = Mathf.Max(0f, deltaTime);
            if (!tutorialReleased)
            {
                tutorialElapsed += elapsed;
                if (tutorialElapsed >= TutorialDuration)
                {
                    tutorialReleased = true;
                    SetActive(tutorialRoot, false);
                    controller.SetTutorialGate(false);
                }
            }

            if (controller.PresentationPhase == MinigamePhase.Countdown)
            {
                countdownElapsed += elapsed;
                RefreshCountdown();
            }

            if (instructionRemaining <= 0f)
            {
                if (goRemaining <= 0f)
                    return;
            }

            if (instructionRemaining > 0f)
            {
                instructionRemaining = Mathf.Max(0f, instructionRemaining - elapsed);
                if (instructionCanvasGroup != null)
                    instructionCanvasGroup.alpha = instructionRemaining / PlayInstructionDuration;
                if (instructionRemaining <= 0f)
                    SetInstructionActive(false);
            }

            if (goRemaining > 0f)
            {
                goRemaining = Mathf.Max(0f, goRemaining - elapsed);
                if (goRemaining <= 0f)
                    SetActive(countdownRoot, false);
            }
        }

        void ApplyPhase(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Tutorial)
            {
                if (tutorialLabel != null)
                    tutorialLabel.text = TutorialMessage;
                SetActive(tutorialRoot, !tutorialReleased);
                SetActive(countdownRoot, false);
                SetInstructionActive(false);
                countdownText = string.Empty;
                instructionText = string.Empty;
                return;
            }

            if (phase == MinigamePhase.Countdown)
            {
                countdownElapsed = 0f;
                SetActive(tutorialRoot, false);
                SetActive(countdownRoot, true);
                SetInstructionActive(false);
                RefreshCountdown();
                return;
            }

            SetActive(tutorialRoot, false);
            if (phase == MinigamePhase.Play)
            {
                countdownText = "GO!";
                if (countdownLabel != null)
                    countdownLabel.text = countdownText;
                goRemaining = GoDuration;
                SetActive(countdownRoot, true);
                instructionText = "TĂNG TỐC!";
                if (instructionLabel != null)
                    instructionLabel.text = instructionText;
                instructionRemaining = PlayInstructionDuration;
                SetInstructionActive(true);
                return;
            }

            SetActive(countdownRoot, false);
            SetInstructionActive(false);
        }

        void RefreshCountdown()
        {
            int remaining = Mathf.Clamp(Mathf.CeilToInt(3f - countdownElapsed), 1, 3);
            countdownText = remaining.ToString();
            if (countdownLabel != null)
                countdownLabel.text = countdownText;
        }

        void Unsubscribe()
        {
            if (controller != null)
                controller.PhaseChanged -= ApplyPhase;
            controller = null;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        void SetInstructionActive(bool active)
        {
            if (instructionCanvasGroup != null)
                instructionCanvasGroup.alpha = active ? 1f : 0f;
            SetActive(instructionRoot, active);
        }

        const string TutorialMessage = "TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI";
    }
}
