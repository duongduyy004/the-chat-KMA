using System;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.Core
{
    public sealed class PunishmentSceneController : MonoBehaviour
    {
        const float MinimumRhythmHoldSeconds = .5f;

        PunishmentController punishment;
        SceneRouter router;
        bool completionRouted;
        bool spaceWasDown;
        bool holdWasDown;
        bool leftWasDown;
        bool rightWasDown;
        bool touchWasDown;
        bool expectedAlternateLeft = true;
        float holdStartedAt;
        float touchStartedAt;
        float currentStepProgress;
        Vector2 touchStartedPosition;

        public PunishmentController Punishment => punishment;
        public bool IsReady => router != null && punishment != null && !completionRouted;

        void Awake()
        {
            router = SceneRouter.Instance;
            if (router == null)
            {
                DisableWithError("Punishment requires a persistent SceneRouter.");
                return;
            }

            var subject = router.Session.PendingPunishmentSubject;
            if (!subject.HasValue)
            {
                DisableWithError("Punishment requires a pending subject from the live GameSession.");
                return;
            }

            try
            {
                punishment = new PunishmentController(router.Session, subject.Value,
                    CreateAuthoredSequence());
                punishment.Completed += OnPunishmentCompleted;
            }
            catch (Exception exception)
            {
                DisableWithError(exception.Message);
            }
        }

        void OnDestroy()
        {
            if (punishment != null)
                punishment.Completed -= OnPunishmentCompleted;
        }

        void Update()
        {
            if (!IsReady)
                return;

            PollKeyboard(Keyboard.current);
            PollTouchscreen(Touchscreen.current);
        }

        public void SubmitTap()
        {
            if (IsMechanicActive(ChallengeMechanic.TapMash))
                ReportActiveProgress();
        }

        public void SubmitRhythmHold(float secondsHeld)
        {
            if (secondsHeld >= MinimumRhythmHoldSeconds &&
                IsMechanicActive(ChallengeMechanic.RhythmHold))
            {
                ReportActiveProgress();
            }
        }

        public void SubmitAlternateTap(bool isLeft)
        {
            if (!IsMechanicActive(ChallengeMechanic.AlternateTap) || isLeft != expectedAlternateLeft)
                return;

            expectedAlternateLeft = !expectedAlternateLeft;
            ReportActiveProgress();
        }

        void PollKeyboard(Keyboard keyboard)
        {
            if (keyboard == null)
                return;

            var spaceIsDown = keyboard.spaceKey.isPressed;
            if (spaceIsDown && !spaceWasDown)
                SubmitTap();
            spaceWasDown = spaceIsDown;

            var holdIsDown = keyboard.hKey.isPressed;
            if (holdIsDown && !holdWasDown)
                holdStartedAt = Time.unscaledTime;
            else if (!holdIsDown && holdWasDown)
                SubmitRhythmHold(Time.unscaledTime - holdStartedAt);
            holdWasDown = holdIsDown;

            var leftIsDown = keyboard.leftArrowKey.isPressed;
            if (leftIsDown && !leftWasDown)
                SubmitAlternateTap(true);
            leftWasDown = leftIsDown;

            var rightIsDown = keyboard.rightArrowKey.isPressed;
            if (rightIsDown && !rightWasDown)
                SubmitAlternateTap(false);
            rightWasDown = rightIsDown;
        }

        void PollTouchscreen(Touchscreen touchscreen)
        {
            if (touchscreen == null)
                return;

            var touch = touchscreen.primaryTouch;
            var touchIsDown = touch.press.isPressed;
            if (touchIsDown && !touchWasDown)
            {
                touchStartedAt = Time.unscaledTime;
                touchStartedPosition = touch.position.ReadValue();
            }
            else if (!touchIsDown && touchWasDown)
            {
                if (IsMechanicActive(ChallengeMechanic.RhythmHold))
                    SubmitRhythmHold(Time.unscaledTime - touchStartedAt);
                else if (IsMechanicActive(ChallengeMechanic.AlternateTap))
                    SubmitAlternateTap(touchStartedPosition.x <= Screen.width * .5f);
                else
                    SubmitTap();
            }
            touchWasDown = touchIsDown;
        }

        bool IsMechanicActive(ChallengeMechanic mechanic)
        {
            if (!IsReady || punishment.CurrentMechanic != mechanic || !punishment.CounterplayAvailable)
                return false;

            foreach (var detector in punishment.ActiveDetectors)
            {
                if (detector.Active && detector.Mechanic == mechanic)
                    return true;
            }

            return false;
        }

        void ReportActiveProgress()
        {
            var mechanic = punishment.CurrentMechanic;
            punishment.ReportDetectorProgress(currentStepProgress + 1f);
            if (punishment.IsComplete)
                return;

            if (punishment.CurrentMechanic != mechanic)
            {
                currentStepProgress = 0f;
                expectedAlternateLeft = true;
                return;
            }

            currentStepProgress++;
        }

        void OnPunishmentCompleted(SessionRoute route)
        {
            if (completionRouted)
                return;

            completionRouted = true;
            router.CompletePunishment(punishment.Subject);
        }

        void DisableWithError(string message)
        {
            Debug.LogError(message, this);
            enabled = false;
        }

        static ChallengeSequence CreateAuthoredSequence() => new ChallengeSequence(new[]
        {
            new ChallengeStep(ChallengeMechanic.TapMash, 5f, 3f),
            new ChallengeStep(ChallengeMechanic.RhythmHold, 5f, 1f),
            new ChallengeStep(ChallengeMechanic.AlternateTap, 5f, 2f)
        });
    }
}
