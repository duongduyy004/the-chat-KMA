using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace KMA.Input
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        const int NoPointer = int.MinValue;

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

        readonly Dictionary<int, PointerGestureState> pointerStates = new Dictionary<int, PointerGestureState>();
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
        InputAction resolvedSwipeUpAction;
        InputAction resolvedSwipeDownAction;
        InputAction resolvedLeftAction;
        InputAction resolvedRightAction;
        InputAction resolvedSprintLeftAction;
        InputAction resolvedSprintRightAction;
        InputAction resolvedRhythmAction;
        InputAction testRhythmAction;
        int holdPointerId = NoPointer;
        int swipePointerId = NoPointer;
        bool subscribed;
        bool keyboardHoldActive;

        public InputActionAsset InputActions => inputActions;
        public string SprintActionMapName => sprintActionMapName;
        public string EnduranceActionMapName => enduranceActionMapName;
        public string BossActionMapName => bossActionMapName;
        public string PunishmentActionMapName => punishmentActionMapName;
        public string UiActionMapName => uiActionMapName;
        public double RhythmOffsetMs { get => rhythmOffsetMs; set => rhythmOffsetMs = value; }
        public double RhythmBeatDsp { get; set; }
        public bool InputActionsReady => subscribed;
        internal bool AcceptsPointerEvents => isActiveAndEnabled;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            ConfigureInputActions();
        }

        void OnDisable()
        {
            FlushPointerState();
            CancelKeyboardHold();
            UnsubscribeInputActions();
            EnhancedTouchSupport.Disable();
        }

        void OnDestroy()
        {
            FlushPointerState();
            CancelKeyboardHold();
            UnsubscribeInputActions();
        }

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

        internal void FeedPointerDown(int pointerId, Vector2 position) => FeedPointerDown(pointerId, position, Timestamp());

        internal void FeedPointerMove(int pointerId, Vector2 position) => FeedPointerMove(pointerId, position, Timestamp());

        internal void FeedPointerUp(int pointerId, Vector2 position) => FeedPointerUp(pointerId, position, Timestamp());

        public void FeedPointerDownForTest(Vector2 position, double timestamp) => FeedPointerDown(0, position, timestamp);

        public void FeedPointerMoveForTest(Vector2 position, double timestamp) => FeedPointerMove(0, position, timestamp);

        public void FeedPointerUpForTest(Vector2 position, double timestamp) => FeedPointerUp(0, position, timestamp);

        public void FeedRhythmTap(double beatDsp) => FeedRhythmTap(AudioSettings.dspTime, beatDsp);

        public void FeedRhythmTapForTest(double inputDsp, double beatDsp) => FeedRhythmTap(inputDsp, beatDsp);

        internal void FlushPointerState()
        {
            double timestamp = Timestamp();
            if (holdPointerId != NoPointer)
                holdDetector?.FeedUp(timestamp);
            if (swipePointerId != NoPointer)
                swipeDetector?.FeedEnd();

            pointerStates.Clear();
            holdPointerId = NoPointer;
            swipePointerId = NoPointer;
        }

        void ConfigureInputActions()
        {
            CancelKeyboardHold();
            UnsubscribeInputActions();
            ResolveActionMaps();
            if (!isActiveAndEnabled || inputActions == null || gameplayActionMap == null)
                return;

            resolvedTapAction = ResolveAction(tapAction, "Tap");
            resolvedHoldAction = ResolveAction(holdAction, "Hold");
            resolvedSwipeUpAction = ResolveAction(swipeAction, "SwipeUp");
            resolvedSwipeDownAction = ResolveAction(null, "SwipeDown");
            resolvedLeftAction = ResolveAction(null, "Left");
            resolvedRightAction = ResolveAction(null, "Right");
            resolvedSprintLeftAction = ResolveAction(null, "SprintLeft");
            resolvedSprintRightAction = ResolveAction(null, "SprintRight");
            resolvedRhythmAction = testRhythmAction ?? ResolveAction(rhythmAction, "Rhythm");

            Subscribe(resolvedTapAction, OnTapPerformed);
            if (resolvedHoldAction != null)
            {
                resolvedHoldAction.started += OnHoldStarted;
                resolvedHoldAction.canceled += OnHoldCanceled;
            }
            Subscribe(resolvedSwipeUpAction, OnSwipeUpPerformed);
            Subscribe(resolvedSwipeDownAction, OnSwipeDownPerformed);
            Subscribe(resolvedLeftAction, OnLeftPerformed);
            Subscribe(resolvedRightAction, OnRightPerformed);
            Subscribe(resolvedSprintLeftAction, OnSprintLeftPerformed);
            Subscribe(resolvedSprintRightAction, OnSprintRightPerformed);
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
            Enable(resolvedSwipeUpAction);
            Enable(resolvedSwipeDownAction);
            Enable(resolvedLeftAction);
            Enable(resolvedRightAction);
            Enable(resolvedSprintLeftAction);
            Enable(resolvedSprintRightAction);
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
            Unsubscribe(resolvedSwipeUpAction, OnSwipeUpPerformed);
            Unsubscribe(resolvedSwipeDownAction, OnSwipeDownPerformed);
            Unsubscribe(resolvedLeftAction, OnLeftPerformed);
            Unsubscribe(resolvedRightAction, OnRightPerformed);
            Unsubscribe(resolvedSprintLeftAction, OnSprintLeftPerformed);
            Unsubscribe(resolvedSprintRightAction, OnSprintRightPerformed);
            Unsubscribe(resolvedRhythmAction, OnRhythmPerformed);
            Disable(resolvedTapAction);
            Disable(resolvedHoldAction);
            Disable(resolvedSwipeUpAction);
            Disable(resolvedSwipeDownAction);
            Disable(resolvedLeftAction);
            Disable(resolvedRightAction);
            Disable(resolvedSprintLeftAction);
            Disable(resolvedSprintRightAction);
            Disable(resolvedRhythmAction);
            resolvedTapAction = null;
            resolvedHoldAction = null;
            resolvedSwipeUpAction = null;
            resolvedSwipeDownAction = null;
            resolvedLeftAction = null;
            resolvedRightAction = null;
            resolvedSprintLeftAction = null;
            resolvedSprintRightAction = null;
            resolvedRhythmAction = null;
            gameplayActionMap = null;
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
            if (!IsKeyboard(context))
                return;

            CancelKeyboardHold();
        }

        void CancelKeyboardHold()
        {
            if (!keyboardHoldActive)
                return;

            keyboardHoldActive = false;
            holdDetector?.FeedUp(Timestamp());
        }

        void OnSwipeUpPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedKeyboardSwipe(Vector2.up);
        }

        void OnSwipeDownPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedKeyboardSwipe(Vector2.down);
        }

        void OnLeftPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedKeyboardSide(Side.Left);
        }

        void OnRightPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedKeyboardSide(Side.Right);
        }

        void OnSprintLeftPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                alternateTapDetector?.FeedTap(Side.Left, Timestamp());
        }

        void OnSprintRightPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                alternateTapDetector?.FeedTap(Side.Right, Timestamp());
        }

        void OnRhythmPerformed(InputAction.CallbackContext context)
        {
            if (context.performed && IsKeyboard(context))
                FeedRhythmTap(RhythmBeatDsp);
        }

        void FeedKeyboardSide(Side side)
        {
            double timestamp = Timestamp();
            alternateTapDetector?.FeedTap(side, timestamp);
            FeedKeyboardSwipe(side == Side.Left ? Vector2.left : Vector2.right, timestamp);
        }

        void FeedKeyboardSwipe(Vector2 direction) => FeedKeyboardSwipe(direction, Timestamp());

        void FeedKeyboardSwipe(Vector2 direction, double timestamp)
        {
            if (swipeDetector == null)
                return;

            swipeDetector.FeedSample(Vector2.zero, timestamp);
            swipeDetector.FeedSample(direction, timestamp);
            swipeDetector.FeedEnd();
        }

        void FeedPointerDown(int pointerId, Vector2 position, double timestamp)
        {
            if (!AcceptsPointerEvents || pointerStates.ContainsKey(pointerId))
                return;

            pointerStates.Add(pointerId, new PointerGestureState(position));
            tapMashDetector?.FeedTap(timestamp);
            alternateTapDetector?.FeedTap(position.x < Screen.width * .5f ? Side.Left : Side.Right, timestamp);
            if (holdPointerId == NoPointer)
            {
                holdPointerId = pointerId;
                holdDetector?.FeedDown(timestamp);
            }
            if (swipePointerId == NoPointer)
            {
                swipePointerId = pointerId;
                swipeDetector?.FeedSample(position, timestamp);
            }
            if (rhythmBeatDetector != null)
                FeedRhythmTap(AudioSettings.dspTime, RhythmBeatDsp);
        }

        void FeedPointerMove(int pointerId, Vector2 position, double timestamp)
        {
            if (!AcceptsPointerEvents || !pointerStates.ContainsKey(pointerId) || swipePointerId != pointerId)
                return;

            pointerStates[pointerId] = new PointerGestureState(position);
            swipeDetector?.FeedSample(position, timestamp);
        }

        void FeedPointerUp(int pointerId, Vector2 position, double timestamp)
        {
            if (!pointerStates.Remove(pointerId))
                return;

            if (holdPointerId == pointerId)
            {
                holdDetector?.FeedUp(timestamp);
                holdPointerId = NoPointer;
            }
            if (swipePointerId == pointerId)
            {
                swipeDetector?.FeedSample(position, timestamp);
                swipeDetector?.FeedEnd();
                swipePointerId = NoPointer;
            }
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

        static double Timestamp() => Time.realtimeSinceStartupAsDouble;

        readonly struct PointerGestureState
        {
            public PointerGestureState(Vector2 position)
            {
                Position = position;
            }

            public Vector2 Position { get; }
        }
    }
}
