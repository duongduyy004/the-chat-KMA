using System;
using KMA.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KMA.Gameplay
{
    public sealed class EnduranceInputBridge : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] GameplayInputRouter inputRouter;

        RhythmBeatInputDetector rhythmDetector;
        HoldInputDetector holdDetector;
        SwipeInputDetector swipeDetector;
        bool routerSubscribed;

        public bool InputActionsReady => inputRouter != null && inputRouter.InputActionsReady;
        public InputActionAsset InputActionsAsset => inputActions;

        void Awake()
        {
            ResolveReferences();
        }

        void OnEnable()
        {
            ResolveReferences();
            ConfigureDetectors();
            SubscribeRouter();
        }

        void OnDisable()
        {
            UnsubscribeRouter();
        }

        void OnDestroy()
        {
            UnsubscribeRouter();
        }

        internal void ProcessTouchSampleForTest(UnityEngine.InputSystem.TouchPhase phase, Vector2 position, Vector2 delta, bool pressed)
        {
            if (inputRouter == null)
                return;

            if (phase == UnityEngine.InputSystem.TouchPhase.Began || (phase == UnityEngine.InputSystem.TouchPhase.None && pressed))
                inputRouter.FeedPointerDownForTest(position, Time.realtimeSinceStartupAsDouble);
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                inputRouter.FeedPointerMoveForTest(position, Time.realtimeSinceStartupAsDouble);
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                inputRouter.FeedPointerUpForTest(position, Time.realtimeSinceStartupAsDouble);
        }

        internal void ConfigureDetectorsForTest(EnduranceController target,
            RhythmBeatInputDetector rhythm, HoldInputDetector hold, SwipeInputDetector swipe)
        {
            controller = target ?? throw new ArgumentNullException(nameof(target));
            UnsubscribeRouter();
            rhythmDetector = rhythm ?? throw new ArgumentNullException(nameof(rhythm));
            holdDetector = hold ?? throw new ArgumentNullException(nameof(hold));
            swipeDetector = swipe ?? throw new ArgumentNullException(nameof(swipe));
            rhythmDetector.OnJudge += OnRhythmJudge;
            holdDetector.OnHoldEnd += OnHoldEnd;
            swipeDetector.OnSwipe += OnSwipe;
        }

        internal void ConfigureInputRouterForTest(EnduranceController target, GameplayInputRouter router)
        {
            controller = target ?? throw new ArgumentNullException(nameof(target));
            UnsubscribeRouter();
            inputRouter = router ?? throw new ArgumentNullException(nameof(router));
            inputActions = inputRouter.InputActions;
            ConfigureDetectors();
            SubscribeRouter();
        }

        void ResolveReferences()
        {
            if (controller == null)
                controller = GetComponentInParent<EnduranceController>();
            if (inputRouter == null)
                inputRouter = FindFirstObjectByType<GameplayInputRouter>();
            if (inputActions == null && inputRouter != null)
                inputActions = inputRouter.InputActions;
        }

        void ConfigureDetectors()
        {
            if (inputRouter == null || controller == null)
                return;

            rhythmDetector ??= new RhythmBeatInputDetector();
            holdDetector ??= new HoldInputDetector();
            swipeDetector ??= new SwipeInputDetector();
            inputRouter.RhythmOffsetMs = controller.RhythmOffsetMs;
            inputRouter.SetDetectors(null, rhythmDetector, holdDetector, null, swipeDetector);
        }

        void SubscribeRouter()
        {
            if (inputRouter == null || routerSubscribed)
                return;

            inputRouter.OnRhythmJudge += OnRhythmJudge;
            inputRouter.OnHoldEnd += OnHoldEnd;
            inputRouter.OnSwipe += OnSwipe;
            routerSubscribed = true;
        }

        void UnsubscribeRouter()
        {
            if (routerSubscribed)
            {
                inputRouter.OnRhythmJudge -= OnRhythmJudge;
                inputRouter.OnHoldEnd -= OnHoldEnd;
                inputRouter.OnSwipe -= OnSwipe;
                routerSubscribed = false;
            }

            if (inputRouter == null)
            {
                if (rhythmDetector != null) rhythmDetector.OnJudge -= OnRhythmJudge;
                if (holdDetector != null) holdDetector.OnHoldEnd -= OnHoldEnd;
                if (swipeDetector != null) swipeDetector.OnSwipe -= OnSwipe;
            }
        }

        void OnRhythmJudge(KMA.Input.TimingJudge _, double deltaMs) { if (controller != null && controller.Phase == MinigamePhase.Play && controller.Rules.Mode == EnduranceInputMode.RhythmTap) controller.TapFromCalibratedDelta(deltaMs); }

        void OnHoldEnd(double duration) { if (controller != null && controller.Phase == MinigamePhase.Play && controller.Rules.Mode == EnduranceInputMode.BreathHold) controller.EndHold((float)(duration / controller.BeatIntervalSeconds)); }

        void OnSwipe(SwipeResult swipe)
        {
            if (controller == null || controller.Phase != MinigamePhase.Play || controller.Rules.Mode != EnduranceInputMode.ObstacleSwipe) return;
            if (swipe.Direction == KMA.Input.SwipeDirection.Up)
                controller.Swipe(SwipeDirection.Up);
            else if (swipe.Direction == KMA.Input.SwipeDirection.Down)
                controller.Swipe(SwipeDirection.Down);
        }
    }
}
