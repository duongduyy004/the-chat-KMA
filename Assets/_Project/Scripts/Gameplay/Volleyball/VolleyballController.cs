using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class VolleyballController : MinigameBase
    {
        const float DefaultTutorialSeconds = 2f;
        const float DefaultCountdownSeconds = 3f;

        [SerializeField] int targetScore = 5;
        [SerializeField] float timeLimit = 60f;
        [SerializeField] BallRig ball;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] Transform player;
        [SerializeField] Transform teammate;
        [SerializeField] BoxCollider2D reachZone;
        [SerializeField] float netX;
        [SerializeField] float apexVelocityThreshold = .1f;
        [SerializeField] float netApexWindow = 1f;
        [SerializeField] float timingWindowSeconds = 4f;
        [SerializeField] float playerLandingOffset = -1f;
        [SerializeField] float teammateLandingOffset = 1f;

        bool inputRouterSubscribed;
        bool terminalResolved;

        public VolleyballRules Rules { get; private set; }
        public BallRig Ball => ball;
        public BallContext CurrentContext => CalculateContext();
        public bool InReachZone => CalculateReach();
        public int TouchCount => Rules == null ? 0 : Rules.TotalTouches;
        public int PlayerScore => Rules == null ? 0 : Rules.PlayerScore;
        public int OpponentScore => Rules == null ? 0 : Rules.OpponentScore;
        public int LongestCombo => Rules == null ? 0 : Rules.LongestCombo;
        public int SuccessfulLaunchCount { get; private set; }
        public bool OpponentCounterCueVisible { get; private set; }
        public bool OpponentFakeCueVisible { get; private set; }
        public Vector2 PredictedLandingPoint { get; private set; }
        public VolleyAction SelectedAction { get; private set; }
        public MinigameResult LastResult { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            CacheReferences();
            Rules = new VolleyballRules(targetScore, timeLimit, Lifecycle);
        }

        void OnEnable()
        {
            CacheReferences();
            SubscribeInputRouter();
        }

        void OnDisable() => UnsubscribeInputRouter();

        void OnDestroy() => UnsubscribeInputRouter();

        public void ConfigureForTest(VolleyballRules rules, BallRig configuredBall)
        {
            Lifecycle = new MinigameLifecycle(DefaultTutorialSeconds, DefaultCountdownSeconds);
            Rules = rules ?? new VolleyballRules(targetScore, timeLimit, Lifecycle);
            if (rules != null)
                AdvanceRulesToPlayForTest();
            ball = configuredBall;
            CacheReferences();
            ResetRuntimeState();
            SubscribeInputRouter();
        }

        public void SimulateForTest(float dt)
        {
            float deltaTime = Mathf.Max(0f, dt);
            Lifecycle.Tick(deltaTime);
            if (PresentationPhase == MinigamePhase.Play)
                TickPlay(deltaTime);
        }

        public void SubmitSwipe(Vector2 swipe, bool inReachZone, float timingAccuracy)
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play || Rules.Phase != MinigamePhase.Play)
                return;

            if (ball.Snapshot.IsInFlight)
                return;

            BallContext context = CurrentContext;
            VolleyAction action = Rules.ResolveTouch(context, swipe, inReachZone, timingAccuracy);
            if (action == VolleyAction.Invalid)
                return;

            bool counterplayWasVisible = OpponentCounterCueVisible || OpponentFakeCueVisible;
            if (!Rules.TryResolveAndLaunch(ball, context, swipe, inReachZone, timingAccuracy))
                return;

            SuccessfulLaunchCount++;
            SelectedAction = action;
            if (counterplayWasVisible)
                ClearCounterplayCues();
            else if (TouchCount >= 3)
                ShowCounterplayCues();
        }

        public MinigameResult BuildResult()
        {
            if (terminalResolved && LastResult != null)
                return LastResult;

            return Rules == null ? default : Rules.BuildResult();
        }

        public MinigameHudState BuildHudState(bool directAccess = true) => CreateHudState();

        protected override MinigameHudState BuildHudState() => CreateHudState();

        protected override void TickPlay(float dt)
        {
            if (Rules == null)
                return;

            float deltaTime = Mathf.Max(0f, dt);
            if (Rules.Phase == MinigamePhase.Play)
                Rules.Tick(deltaTime);

            RefreshRuntimeState();
            ResolveTerminalState();
        }

        void CacheReferences()
        {
            if (inputRouter == null)
                inputRouter = GetComponent<GameplayInputRouter>();
            if (reachZone == null)
                reachZone = GetComponent<BoxCollider2D>();
        }

        void AdvanceRulesToPlayForTest()
        {
            if (Rules.Phase == MinigamePhase.Tutorial)
                Rules.Tick(DefaultTutorialSeconds);
            if (Rules.Phase == MinigamePhase.Countdown)
                Rules.Tick(DefaultCountdownSeconds);
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed)
                return;

            inputRouter.OnSwipe += OnRouterSwipe;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed)
                return;

            inputRouter.OnSwipe -= OnRouterSwipe;
            inputRouterSubscribed = false;
        }

        void OnRouterSwipe(SwipeResult swipe)
        {
            if (PresentationPhase != MinigamePhase.Play)
                return;

            Vector2 direction = ToVector2(swipe.Direction);
            if (CurrentContext == BallContext.ApexNearNet && swipe.Direction == SwipeDirection.Right)
                direction = new Vector2(1f, -1f);

            SubmitSwipe(direction, InReachZone, CalculateTimingAccuracy(swipe));
        }

        BallContext CalculateContext()
        {
            if (ball == null)
                return BallContext.Low;

            BallFlightSnapshot snapshot = ball.Snapshot;
            if (ball.IsNearApex(apexVelocityThreshold) && Mathf.Abs(snapshot.Position.x - netX) <= netApexWindow)
                return BallContext.ApexNearNet;
            if (ball.Body.velocity.y > 0f)
                return BallContext.Rising;
            return BallContext.Low;
        }

        bool CalculateReach() => ball != null && reachZone != null && reachZone.bounds.Contains(ball.Body.position);

        float CalculateTimingAccuracy(SwipeResult swipe)
        {
            float window = Mathf.Max(Mathf.Epsilon, timingWindowSeconds);
            return Mathf.Clamp01(1f - (float)swipe.Duration / window);
        }

        void RefreshRuntimeState()
        {
            if (ball == null)
                return;

            PredictedLandingPoint = ball.PredictLandingPoint();
            MoveActorToPrediction(player, playerLandingOffset);
            MoveActorToPrediction(teammate, teammateLandingOffset);
        }

        void MoveActorToPrediction(Transform actor, float horizontalOffset)
        {
            if (actor == null)
                return;

            actor.position = new Vector3(PredictedLandingPoint.x + horizontalOffset, actor.position.y, actor.position.z);
        }

        void ShowCounterplayCues()
        {
            OpponentCounterCueVisible = true;
            OpponentFakeCueVisible = true;
        }

        void ClearCounterplayCues()
        {
            OpponentCounterCueVisible = false;
            OpponentFakeCueVisible = false;
        }

        void ResolveTerminalState()
        {
            if (terminalResolved || Rules == null || PresentationPhase != MinigamePhase.Play)
                return;
            if (Rules.Phase != MinigamePhase.Resolve && !Rules.BuildResult().Pass)
                return;

            terminalResolved = true;
            LastResult = Rules.BuildResult();
            ClearCounterplayCues();
            Finish(LastResult);
        }

        void ResetRuntimeState()
        {
            terminalResolved = false;
            SuccessfulLaunchCount = 0;
            SelectedAction = VolleyAction.Invalid;
            PredictedLandingPoint = Vector2.zero;
            LastResult = default;
            ClearCounterplayCues();
        }

        static Vector2 ToVector2(SwipeDirection direction) => direction switch
        {
            SwipeDirection.Left => Vector2.left,
            SwipeDirection.Right => Vector2.right,
            SwipeDirection.Up => Vector2.up,
            SwipeDirection.Down => Vector2.down,
            _ => Vector2.zero
        };

        MinigameHudState CreateHudState()
        {
            if (Rules == null)
                return MinigameHudState.Empty;

            string status = OpponentFakeCueVisible ? "COUNTER THE FAKE" :
                !InReachZone ? "MOVE INTO REACH" :
                "TOUCH " + (TouchCount % 3 + 1) + "/3";
            return new MinigameHudState(
                phase: PresentationPhase.ToString(),
                timeRemaining: Mathf.Max(0f, timeLimit - Rules.Elapsed),
                progress01: Mathf.Clamp01(PlayerScore / (float)Mathf.Max(1, targetScore)),
                stamina01: TouchCount == 0 ? 0f : Rules.AccurateTouches / (float)TouchCount,
                score: Rules.BuildResult().Score,
                statusText: status);
        }
    }
}
