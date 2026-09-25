using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay.Volleyball
{
    // Merges the touch controls with a keyboard fallback (WASD/arrows + Space). The keyboard
    // actions live in code because KMA.inputactions' map list is pinned by InputAssetContractTests.
    public sealed class VolleyballInputBridge : MonoBehaviour
    {
        [SerializeField] VirtualJoystick joystick;
        [SerializeField] ActionButton actionButton;

        InputAction moveAction;
        InputAction pressAction;
        ActionButton subscribedButton;
        int pendingPresses;
        Vector2? testMove;

        public void Configure(VirtualJoystick stick, ActionButton button)
        {
            joystick = stick;
            actionButton = button;
            if (isActiveAndEnabled)
                SubscribeButton();
        }

        public Vector2 Move
        {
            get
            {
                if (testMove.HasValue)
                    return testMove.Value;
                if (joystick && joystick.Value.sqrMagnitude > .0001f)
                    return joystick.Value;
                return moveAction == null ? Vector2.zero : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            }
        }

        public int ConsumePresses()
        {
            int presses = pendingPresses;
            pendingPresses = 0;
            return presses;
        }

        public void ClearPresses() => pendingPresses = 0;

        public void FeedMoveForTest(Vector2 move) => testMove = move;

        public void FeedActionForTest() => pendingPresses++;

        void OnEnable()
        {
            EnsureActions();
            moveAction.Enable();
            pressAction.Enable();
            SubscribeButton();
        }

        void OnDisable()
        {
            moveAction?.Disable();
            pressAction?.Disable();
            UnsubscribeButton();
        }

        void OnDestroy()
        {
            moveAction?.Dispose();
            pressAction?.Dispose();
        }

        void EnsureActions()
        {
            if (moveAction != null)
                return;

            moveAction = new InputAction("VolleyballMove", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            pressAction = new InputAction("VolleyballAction", InputActionType.Button, "<Keyboard>/space");
            pressAction.performed += _ => OnPressed();
        }

        void SubscribeButton()
        {
            if (subscribedButton == actionButton)
                return;

            UnsubscribeButton();
            if (!actionButton)
                return;

            actionButton.Pressed += OnPressed;
            subscribedButton = actionButton;
        }

        void UnsubscribeButton()
        {
            if (subscribedButton)
                subscribedButton.Pressed -= OnPressed;
            subscribedButton = null;
        }

        void OnPressed() => pendingPresses++;
    }
}
