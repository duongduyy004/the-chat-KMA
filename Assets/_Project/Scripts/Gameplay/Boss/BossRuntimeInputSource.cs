using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using KMA.Gameplay;

namespace KMA.Gameplay.Boss
{
    public sealed class BossRuntimeInputSource : MonoBehaviour
    {
        [SerializeField] BossPhaseController bossController;
        [SerializeField] Keyboard keyboard;

        bool holdWasDown;
        bool spaceWasDown;
        bool leftWasDown;
        bool rightWasDown;
        float holdStartedAt;

        public bool IsWired => bossController != null &&
            bossController.TapMashDetector != null &&
            bossController.RhythmHoldDetector != null &&
            bossController.AlternateTapDetector != null;

        public Keyboard KeyboardDevice
        {
            get
            {
                if (keyboard != null && keyboard.added)
                    return keyboard;

                var currentKeyboard = Keyboard.current;
                return currentKeyboard != null && currentKeyboard.added
                    ? currentKeyboard
                    : InputSystem.devices.OfType<Keyboard>().LastOrDefault();
            }
            set => keyboard = value;
        }

        public void OnTapMashPressed() => bossController.TapMashDetector.SubmitTap();

        public void OnRhythmHoldReleased(float secondsHeld) =>
            bossController.RhythmHoldDetector.SubmitHold(secondsHeld);

        public void OnAlternateTapPressed(BossTapSide side) =>
            bossController.AlternateTapDetector.SubmitTap(side);

        void OnEnable() => InputSystem.onAfterUpdate += PollInput;

        void OnDisable() => InputSystem.onAfterUpdate -= PollInput;

        void Update() => PollInput();

        void PollInput()
        {
            var keyboard = KeyboardDevice;
            if (keyboard == null || !IsWired || !bossController.IsRunning)
            {
                holdWasDown = false;
                spaceWasDown = false;
                leftWasDown = false;
                rightWasDown = false;
                return;
            }

            var spaceIsDown = keyboard.spaceKey.isPressed;
            if (spaceIsDown && !spaceWasDown)
                OnTapMashPressed();
            spaceWasDown = spaceIsDown;

            var holdIsDown = keyboard.hKey.isPressed;
            if (holdIsDown && !holdWasDown)
                holdStartedAt = Time.time;
            else if (!holdIsDown && holdWasDown)
                OnRhythmHoldReleased(Time.time - holdStartedAt);
            holdWasDown = holdIsDown;

            var leftIsDown = keyboard.leftArrowKey.isPressed;
            if (leftIsDown && !leftWasDown)
                OnAlternateTapPressed(BossTapSide.Left);
            leftWasDown = leftIsDown;

            var rightIsDown = keyboard.rightArrowKey.isPressed;
            if (rightIsDown && !rightWasDown)
                OnAlternateTapPressed(BossTapSide.Right);
            rightWasDown = rightIsDown;
        }
    }
}
