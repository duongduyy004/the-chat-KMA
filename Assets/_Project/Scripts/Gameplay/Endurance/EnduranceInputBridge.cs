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

        InputAction tapAction;
        InputAction holdAction;
        InputAction swipeUpAction;
        InputAction swipeDownAction;

        public bool InputActionsReady => tapAction != null && holdAction != null && swipeUpAction != null && swipeDownAction != null;

        void Awake()
        {
            if (controller == null)
                controller = GetComponentInParent<EnduranceController>();
        }

        void OnEnable() => ConfigureInputActions();

        void OnDisable() => UnsubscribeInputActions();

        void OnDestroy() => UnsubscribeInputActions();

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
    }
}
