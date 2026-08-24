using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay
{
    public sealed class EnduranceInputBridge : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string tapActionName = "Tap";
        [SerializeField] string holdActionName = "Hold";
        [SerializeField] string swipeUpActionName = "SwipeUp";
        [SerializeField] string swipeDownActionName = "SwipeDown";
        [SerializeField] string touchPressActionName = "TouchPress";
        [SerializeField] string touchPositionActionName = "TouchPosition";
        [SerializeField] string touchDeltaActionName = "TouchDelta";
        [SerializeField] float swipeThresholdPixels = 80f;

        InputAction tapAction;
        InputAction holdAction;
        InputAction swipeUpAction;
        InputAction swipeDownAction;
        InputAction touchPressAction;
        InputAction touchPositionAction;
        InputAction touchDeltaAction;
        Vector2 previousTouchPosition;
        bool touchTracking;

        public bool InputActionsReady => tapAction != null && holdAction != null && swipeUpAction != null && swipeDownAction != null && touchPositionAction != null && touchDeltaAction != null;
        public InputActionAsset InputActionsAsset => inputActions;

        void Awake()
        {
            if (controller == null)
                controller = GetComponentInParent<EnduranceController>();
        }

        void OnEnable() => ConfigureInputActions();

        void OnDisable() => UnsubscribeInputActions();

        void OnDestroy() => UnsubscribeInputActions();

        void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return;

            var touch = touchscreen.primaryTouch;
            ProcessTouchSample(touch.phase.ReadValue(), touch.position.ReadValue(), touch.delta.ReadValue(), touch.press.isPressed);
        }

        internal void ProcessTouchSampleForTest(UnityEngine.InputSystem.TouchPhase phase, Vector2 position, Vector2 delta, bool pressed)
        {
            ProcessTouchSample(phase, position, delta, pressed);
        }

        void ProcessTouchSample(UnityEngine.InputSystem.TouchPhase phase, Vector2 position, Vector2 delta, bool pressed)
        {
            if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                (phase == UnityEngine.InputSystem.TouchPhase.None && pressed && !touchTracking))
            {
                previousTouchPosition = position;
                touchTracking = true;
                return;
            }

            if ((phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.None) && touchTracking)
            {
                if (position == previousTouchPosition)
                    return;
                delta = position - previousTouchPosition;
                previousTouchPosition = position;
                DispatchVerticalSwipe(delta);
                return;
            }

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                touchTracking = false;
                previousTouchPosition = default;
            }
        }

        internal void ConfigureForTest(EnduranceController target, InputActionAsset actions)
        {
            controller = target ?? throw new ArgumentNullException(nameof(target));
            inputActions = actions ?? throw new ArgumentNullException(nameof(actions));
            ConfigureInputActions();
        }

        void ConfigureInputActions()
        {
            UnsubscribeInputActions();
            if (controller == null || inputActions == null)
                return;

            tapAction = inputActions.FindAction(tapActionName, false);
            holdAction = inputActions.FindAction(holdActionName, false);
            swipeUpAction = inputActions.FindAction(swipeUpActionName, false);
            swipeDownAction = inputActions.FindAction(swipeDownActionName, false);
            touchPressAction = inputActions.FindAction(touchPressActionName, false);
            touchPositionAction = inputActions.FindAction(touchPositionActionName, false);
            touchDeltaAction = inputActions.FindAction(touchDeltaActionName, false);
            if (!InputActionsReady)
                return;

            tapAction.performed += OnTapPerformed;
            holdAction.performed += OnHoldPerformed;
            swipeUpAction.performed += OnSwipeUpPerformed;
            swipeDownAction.performed += OnSwipeDownPerformed;
            inputActions.Enable();
            tapAction.Enable();
            holdAction.Enable();
            swipeUpAction.Enable();
            swipeDownAction.Enable();
            touchPressAction.Enable();
            touchPositionAction.Enable();
            touchDeltaAction.Enable();
        }

        void UnsubscribeInputActions()
        {
            if (tapAction != null) tapAction.performed -= OnTapPerformed;
            if (holdAction != null) holdAction.performed -= OnHoldPerformed;
            if (swipeUpAction != null) swipeUpAction.performed -= OnSwipeUpPerformed;
            if (swipeDownAction != null) swipeDownAction.performed -= OnSwipeDownPerformed;
            tapAction = null;
            holdAction = null;
            swipeUpAction = null;
            swipeDownAction = null;
            touchPressAction = null;
            touchPositionAction = null;
            touchDeltaAction = null;
            touchTracking = false;
            previousTouchPosition = default;
        }

        void OnTapPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
                controller.TapAtCurrentBeat();
        }

        void OnHoldPerformed(InputAction.CallbackContext context)
        {
            controller.EndHold((float)(context.duration / controller.BeatIntervalSeconds));
        }

        void OnSwipeUpPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
                controller.Swipe(SwipeDirection.Up);
        }

        void OnSwipeDownPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
                controller.Swipe(SwipeDirection.Down);
        }

        void DispatchVerticalSwipe(Vector2 delta)
        {
            if (Mathf.Abs(delta.y) < swipeThresholdPixels || Mathf.Abs(delta.y) <= Mathf.Abs(delta.x))
                return;

            controller.Swipe(delta.y > 0f ? SwipeDirection.Up : SwipeDirection.Down);
        }
    }
}
