using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Input
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] InputActionReference tapAction;
        [SerializeField] InputActionReference holdAction;
        [SerializeField] InputActionReference swipeAction;
        [SerializeField] InputActionReference rhythmAction;
        [SerializeField] string sprintActionMapName = "Sprint";
        [SerializeField] string enduranceActionMapName = "Endurance";
        [SerializeField] string bossActionMapName = "Boss";
        [SerializeField] string punishmentActionMapName = "Punishment";
        [SerializeField] string uiActionMapName = "UI";
        [SerializeField] string gameplayActionMapName = "Endurance";
        [SerializeField] double rhythmOffsetMs;

        TapMashInputDetector tapMashDetector;
        RhythmBeatInputDetector rhythmBeatDetector;
        HoldInputDetector holdDetector;
        AlternateTapInputDetector alternateTapDetector;
        SwipeInputDetector swipeDetector;
        InputActionMap sprintActionMap;
        InputActionMap enduranceActionMap;
        InputActionMap bossActionMap;
        InputActionMap punishmentActionMap;
        InputActionMap uiActionMap;
        InputActionMap gameplayActionMap;
        InputAction resolvedTapAction;
        InputAction resolvedHoldAction;
        InputAction resolvedSwipeAction;
        InputAction resolvedRhythmAction;
        InputAction testRhythmAction;
        bool keyboardHoldActive;
        bool subscribed;

        public InputActionAsset InputActions => inputActions;
        public string SprintActionMapName => sprintActionMapName;
        public string EnduranceActionMapName => enduranceActionMapName;
        public string BossActionMapName => bossActionMapName;
        public string PunishmentActionMapName => punishmentActionMapName;
        public string UiActionMapName => uiActionMapName;
        public double RhythmOffsetMs { get => rhythmOffsetMs; set => rhythmOffsetMs = value; }
        public double RhythmBeatDsp { get; set; }
        public bool InputActionsReady => subscribed;

        void OnEnable() => ConfigureInputActions();

        void OnDisable() => UnsubscribeInputActions();

        void OnDestroy() => UnsubscribeInputActions();

        public void SetDetectors(
            TapMashInputDetector tapMash,
            RhythmBeatInputDetector rhythmBeat,
            HoldInputDetector hold,
            AlternateTapInputDetector alternateTap,
            SwipeInputDetector swipe)
        {
            tapMashDetector = tapMash;
            rhythmBeatDetector = rhythmBeat;
            holdDetector = hold;
            alternateTapDetector = alternateTap;
            swipeDetector = swipe;
        }

        public void ConfigureInputForTest(InputActionAsset actions, string actionMapName, InputAction rhythm = null)
        {
            inputActions = actions;
            gameplayActionMapName = actionMapName;
            testRhythmAction = rhythm;
            ConfigureInputActions();
        }

        internal void FeedPointerDown(Vector2 position) => FeedPointerDown(position, Timestamp());

        internal void FeedPointerMove(Vector2 position) => FeedPointerMove(position, Timestamp());

        internal void FeedPointerUp(Vector2 position) => FeedPointerUp(position, Timestamp());

        public void FeedPointerDownForTest(Vector2 position, double timestamp) => FeedPointerDown(position, timestamp);

        public void FeedPointerMoveForTest(Vector2 position, double timestamp) => FeedPointerMove(position, timestamp);

        public void FeedPointerUpForTest(Vector2 position, double timestamp) => FeedPointerUp(position, timestamp);

        public void FeedRhythmTap(double beatDsp) => FeedRhythmTap(AudioSettings.dspTime, beatDsp);

        public void FeedRhythmTapForTest(double inputDsp, double beatDsp) => FeedRhythmTap(inputDsp, beatDsp);

        void ConfigureInputActions()
        {
            UnsubscribeInputActions();
            ResolveActionMaps();
            if (!isActiveAndEnabled || inputActions == null || gameplayActionMap == null)
                return;

            resolvedTapAction = ResolveAction(tapAction, "Tap");
            resolvedHoldAction = ResolveAction(holdAction, "Hold");
            resolvedSwipeAction = ResolveAction(swipeAction, "SwipeUp");
            resolvedRhythmAction = testRhythmAction ?? ResolveAction(rhythmAction, "Rhythm");

            Subscribe(resolvedTapAction, OnTapPerformed);
            if (resolvedHoldAction != null)
            {
                resolvedHoldAction.started += OnHoldStarted;
                resolvedHoldAction.canceled += OnHoldCanceled;
            }
            Subscribe(resolvedSwipeAction, OnSwipePerformed);
            Subscribe(resolvedRhythmAction, OnRhythmPerformed);
            EnableResolvedActions();
            subscribed = true;
        }

        void ResolveActionMaps()
        {
            if (inputActions == null)
                return;

            sprintActionMap = inputActions.FindActionMap(sprintActionMapName, false);
            enduranceActionMap = inputActions.FindActionMap(enduranceActionMapName, false);
            bossActionMap = inputActions.FindActionMap(bossActionMapName, false);
            punishmentActionMap = inputActions.FindActionMap(punishmentActionMapName, false);
            uiActionMap = inputActions.FindActionMap(uiActionMapName, false);
            gameplayActionMap = inputActions.FindActionMap(gameplayActionMapName, false);
        }

        InputAction ResolveAction(InputActionReference reference, string fallbackName)
        {
            return reference != null && reference.action != null
                ? reference.action
                : gameplayActionMap.FindAction(fallbackName, false);
        }

        void EnableResolvedActions()
        {
            Enable(resolvedTapAction);
            Enable(resolvedHoldAction);
            Enable(resolvedSwipeAction);
            Enable(resolvedRhythmAction);
        }

        void UnsubscribeInputActions()
        {
            Unsubscribe(resolvedTapAction, OnTapPerformed);
            if (resolvedHoldAction != null)
            {
                resolvedHoldAction.started -= OnHoldStarted;
                resolvedHoldAction.canceled -= OnHoldCanceled;
            }
            Unsubscribe(resolvedSwipeAction, OnSwipePerformed);
            Unsubscribe(resolvedRhythmAction, OnRhythmPerformed);
            Disable(resolvedTapAction);
            Disable(resolvedHoldAction);
            Disable(resolvedSwipeAction);
            Disable(resolvedRhythmAction);
            resolvedTapAction = null;
            resolvedHoldAction = null;
            resolvedSwipeAction = null;
            resolvedRhythmAction = null;
            gameplayActionMap = null;
            keyboardHoldActive = false;
            subscribed = false;
        }

        void OnTapPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                tapMashDetector?.FeedTap(Timestamp());
        }

        void OnHoldStarted(InputAction.CallbackContext context)
        {
            if (!IsKeyboard(context))
                return;

            keyboardHoldActive = true;
            holdDetector?.FeedDown(Timestamp());
        }

        void OnHoldCanceled(InputAction.CallbackContext context)
        {
            if (!keyboardHoldActive || !IsKeyboard(context))
                return;

            keyboardHoldActive = false;
            holdDetector?.FeedUp(Timestamp());
        }

        void OnSwipePerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || !IsKeyboard(context) || swipeDetector == null)
                return;

            double timestamp = Timestamp();
            swipeDetector.FeedSample(Vector2.zero, timestamp);
            swipeDetector.FeedSample(SwipeVector(context.action.name), timestamp);
            swipeDetector.FeedEnd();
        }

        void OnRhythmPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedRhythmTap(RhythmBeatDsp);
        }

        void FeedPointerDown(Vector2 position, double timestamp)
        {
            tapMashDetector?.FeedTap(timestamp);
            holdDetector?.FeedDown(timestamp);
            alternateTapDetector?.FeedTap(position.x < Screen.width * .5f ? Side.Left : Side.Right, timestamp);
            swipeDetector?.FeedSample(position, timestamp);
        }

        void FeedPointerMove(Vector2 position, double timestamp) => swipeDetector?.FeedSample(position, timestamp);

        void FeedPointerUp(Vector2 position, double timestamp)
        {
            holdDetector?.FeedUp(timestamp);
            if (swipeDetector == null)
                return;

            swipeDetector.FeedSample(position, timestamp);
            swipeDetector.FeedEnd();
        }

        void FeedRhythmTap(double inputDsp, double beatDsp)
        {
            rhythmBeatDetector?.FeedTap(inputDsp + rhythmOffsetMs / 1000d, beatDsp);
        }

        static void Subscribe(InputAction action, System.Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
                action.performed += callback;
        }

        static void Unsubscribe(InputAction action, System.Action<InputAction.CallbackContext> callback)
        {
            if (action != null)
                action.performed -= callback;
        }

        static void Enable(InputAction action)
        {
            if (action != null)
                action.Enable();
        }

        static void Disable(InputAction action)
        {
            if (action != null && action.enabled)
                action.Disable();
        }

        static bool IsKeyboard(InputAction.CallbackContext context) => context.control?.device is Keyboard;

        static Vector2 SwipeVector(string actionName)
        {
            switch (actionName)
            {
                case "SwipeDown": return Vector2.down;
                case "Left": return Vector2.left;
                case "Right": return Vector2.right;
                default: return Vector2.up;
            }
        }

        static double Timestamp() => Time.realtimeSinceStartupAsDouble;
    }
}
