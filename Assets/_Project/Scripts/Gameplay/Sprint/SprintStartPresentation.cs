using TMPro;
using UnityEngine;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    /// Sprint-only start flow: a 1.5 s input gate, a 3-2-1-GO! countdown, and one
    /// instruction line that persists across both and fades shortly after GO!.
    public sealed class SprintStartPresentation : MonoBehaviour, ISprintStartPresentation
    {
        public const string InstructionCopy = "BẤM TRÁI VÀ PHẢI LUÂN PHIÊN ĐỂ CHẠY";

        const float GateDuration = 1.5f;
        const float GoDuration = .5f;
        const float InstructionFadeDuration = .4f;
        const float DigitPopDuration = .15f;
        const float DigitPopScale = 1.25f;
        const float GoPopScale = 1.4f;
        const float GoPopDuration = .2f;
        const float MaxAutomaticStepSeconds = .1f;
        // Bind() typically runs mid-frame (from a scene bootstrapper's Awake), so the automatic
        // Update() can still fire once or twice more before a caller gets a chance to drive the
        // gate manually (e.g. a test's very next line); one of those frames also reports whatever
        // heavy synchronous scene/chrome construction happened earlier that frame as its delta.
        // Skip a small, fixed number of automatic ticks right after Bind() so that noise never
        // steals part of the deterministic gate/countdown window.
        const int AutomaticTicksToSuppressAfterBind = 3;

        [SerializeField] GameObject countdownRoot;
        [SerializeField] TMP_Text countdownLabel;
        [SerializeField] GameObject instructionRoot;
        [SerializeField] TMP_Text instructionLabel;

        SprintController controller;
        CanvasGroup instructionCanvasGroup;
        float gateElapsed;
        float countdownElapsed;
        float goRemaining;
        float fadeRemaining;
        bool gateReleased;
        bool fading;
        string lastDigit = string.Empty;
        float popRemaining;
        float popFrom = 1f;
        int automaticTicksToSuppress;

        public string CountdownText => countdownLabel == null ? string.Empty : countdownLabel.text;
        public string InstructionText => instructionLabel == null ? string.Empty : instructionLabel.text;
        public bool InstructionVisible =>
            instructionRoot != null && instructionRoot.activeSelf &&
            (instructionCanvasGroup == null || instructionCanvasGroup.alpha > .001f);
        public float CountdownScale =>
            countdownRoot == null ? 1f : countdownRoot.transform.localScale.x;

        void OnDisable() => Unsubscribe();
        void OnDestroy() => Unsubscribe();

        void Update()
        {
            if (automaticTicksToSuppress > 0)
            {
                automaticTicksToSuppress--;
                return;
            }

            // Bound, never drop: a hitch (heavy synchronous scene/chrome construction, GC) must not
            // fast-forward the deterministic gate/countdown timers, but dropping the frame outright
            // would stall the gate indefinitely on a device sustaining sub-10-FPS frames, and
            // gateElapsed is the only thing that opens the input gate.
            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxAutomaticStepSeconds));
        }

        public void Configure(GameObject countdown, TMP_Text countdownText,
            GameObject instruction, TMP_Text instructionText)
        {
            countdownRoot = countdown;
            countdownLabel = countdownText;
            instructionRoot = instruction;
            instructionLabel = instructionText;
            instructionCanvasGroup = instructionRoot == null ? null :
                instructionRoot.GetComponent<CanvasGroup>() ?? instructionRoot.AddComponent<CanvasGroup>();

            if (instructionLabel != null)
                instructionLabel.text = InstructionCopy;
            SetActive(countdownRoot, false);
        }

        public void Bind(SprintController source)
        {
            Unsubscribe();
            controller = source;
            gateElapsed = 0f;
            countdownElapsed = 0f;
            goRemaining = 0f;
            fadeRemaining = 0f;
            gateReleased = false;
            fading = false;
            lastDigit = string.Empty;
            popRemaining = 0f;
            automaticTicksToSuppress = AutomaticTicksToSuppressAfterBind;

            if (controller == null)
            {
                SetActive(countdownRoot, false);
                SetInstructionAlpha(0f);
                return;
            }

            controller.PhaseChanged += ApplyPhase;
            controller.SetTutorialGate(true);
            SetActive(instructionRoot, true);
            SetInstructionAlpha(1f);
            ApplyPhase(controller.PresentationPhase);
        }

        void ISprintStartPresentation.Bind(MinigameBase source) => Bind(source as SprintController);

        public void TickForTest(float deltaTime) => Tick(deltaTime);

        void Tick(float deltaTime)
        {
            if (controller == null)
                return;

            float elapsed = Mathf.Max(0f, deltaTime);
            // If the gate releases mid-tick, only the overshoot past GateDuration has actually
            // elapsed inside Countdown; crediting the whole tick would skip a digit.
            float countdownContribution = elapsed;

            if (!gateReleased)
            {
                gateElapsed += elapsed;
                if (gateElapsed >= GateDuration)
                {
                    gateReleased = true;
                    countdownContribution = gateElapsed - GateDuration;
                    controller.SetTutorialGate(false);
                }
                else
                {
                    countdownContribution = 0f;
                }
            }

            if (controller.PresentationPhase == MinigamePhase.Countdown)
            {
                countdownElapsed += countdownContribution;
                RefreshCountdown();
            }

            if (goRemaining > 0f)
            {
                goRemaining = Mathf.Max(0f, goRemaining - elapsed);
                if (goRemaining <= 0f)
                {
                    if (countdownLabel != null)
                        countdownLabel.text = string.Empty;
                    SetActive(countdownRoot, false);
                }
            }

            if (fading)
            {
                fadeRemaining = Mathf.Max(0f, fadeRemaining - elapsed);
                SetInstructionAlpha(fadeRemaining / InstructionFadeDuration);
                if (fadeRemaining <= 0f)
                {
                    fading = false;
                    SetActive(instructionRoot, false);
                }
            }

            TickPop(elapsed);
        }

        void ApplyPhase(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Countdown)
            {
                countdownElapsed = 0f;
                SetActive(countdownRoot, true);
                RefreshCountdown();
                return;
            }

            if (phase == MinigamePhase.Play)
            {
                if (countdownLabel != null)
                    countdownLabel.text = "GO!";
                SetActive(countdownRoot, true);
                goRemaining = GoDuration;
                Pop(GoPopScale, GoPopDuration);
                fading = true;
                fadeRemaining = InstructionFadeDuration;
                return;
            }

            if (phase != MinigamePhase.Tutorial)
            {
                SetActive(countdownRoot, false);
                SetActive(instructionRoot, false);
            }
        }

        void RefreshCountdown()
        {
            string digit = Mathf.Clamp(Mathf.CeilToInt(3f - countdownElapsed), 1, 3).ToString();
            if (digit == lastDigit)
                return;

            lastDigit = digit;
            if (countdownLabel != null)
                countdownLabel.text = digit;
            Pop(DigitPopScale, DigitPopDuration);
        }

        void Pop(float fromScale, float duration)
        {
            popFrom = fromScale;
            popRemaining = duration;
            if (countdownRoot != null)
                countdownRoot.transform.localScale = Vector3.one * fromScale;
        }

        void TickPop(float deltaTime)
        {
            if (popRemaining <= 0f || countdownRoot == null)
                return;

            popRemaining = Mathf.Max(0f, popRemaining - deltaTime);
            float duration = popFrom == GoPopScale ? GoPopDuration : DigitPopDuration;
            float t = duration <= 0f ? 1f : 1f - popRemaining / duration;
            countdownRoot.transform.localScale = Vector3.one * Mathf.Lerp(popFrom, 1f, t);
        }

        void Unsubscribe()
        {
            if (controller != null)
                controller.PhaseChanged -= ApplyPhase;
            controller = null;
        }

        void SetInstructionAlpha(float alpha)
        {
            if (instructionCanvasGroup != null)
                instructionCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
