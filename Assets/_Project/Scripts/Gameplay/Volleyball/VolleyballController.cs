using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum VolleyBallOwner { None, Player, Opponent }

    public sealed class VolleyballController : MinigameBase
    {
        const float DefaultTutorialSeconds = 2f;
        const float DefaultCountdownSeconds = 3f;
        const int TouchesPerPossession = 3;

        [SerializeField] int targetScore = 5;
        [SerializeField] float timeLimit = 60f;
        [SerializeField] BallRig ball;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] Transform player;
        [SerializeField] Transform teammate;
        [SerializeField] Transform opponent;
        [SerializeField] BoxCollider2D reachZone;
        [SerializeField] TrajectoryPreview trajectoryPreview;
        [SerializeField] BallShadow ballShadow;
        [SerializeField] float netX;
        [SerializeField] float apexVelocityThreshold = .1f;
        [SerializeField] float netApexWindow = 1f;
        [SerializeField] float timingWindowSeconds = 4f;
        [SerializeField] float playerLandingOffset = -1f;
        [SerializeField] float teammateLandingOffset = 1f;

        bool inputRouterSubscribed;
        bool ballSubscribed;
        bool terminalResolved;
        bool gesturePreparation;
        bool pendingOpponentReturn;
        int possessionToken;
        int resolvedPossessionToken = -1;
        float opponentReturnDelay;
        int touchNumber;
        SwipeInputDetector productionSwipeDetector;

        public VolleyballRules Rules { get; private set; }
        public BallRig Ball => ball;
        public BallContext CurrentContext => CalculateContext();
        public bool InReachZone => CalculateReach();
        public int TouchCount => touchNumber;
        public VolleyAction ExpectedAction => ActionForTouch(touchNumber + 1);
        public int CompletedRallyCount { get; private set; }
        public VolleyBallOwner InFlightOwner { get; private set; }
        public int PlayerScore => Rules == null ? 0 : Rules.PlayerScore;
        public int OpponentScore => Rules == null ? 0 : Rules.OpponentScore;
        public int LongestCombo => Rules == null ? 0 : Rules.LongestCombo;
        public int SuccessfulLaunchCount { get; private set; }
        public bool OpponentCounterCueVisible { get; private set; }
        public bool OpponentFakeCueVisible { get; private set; }
        public bool IsGesturePreparation => gesturePreparation;
        public Vector2 PredictedLandingPoint { get; private set; }
        public VolleyAction SelectedAction { get; private set; }
        public MinigameResult LastResult { get; private set; }
        public GameplayInputRouter InputRouter => inputRouter;
        public BoxCollider2D ReachZone => reachZone;
        public TrajectoryPreview Preview => trajectoryPreview;
        public BallShadow Shadow => ballShadow;
        public SwipeInputDetector InstalledSwipeDetector => productionSwipeDetector;

        protected override void Awake()
        {
            base.Awake();
            CacheReferences();
            InstallProductionSwipeDetector();
            Rules = new VolleyballRules(targetScore, timeLimit, Lifecycle);
            ResetRuntimeState();
        }

        void OnEnable()
        {
            CacheReferences();
            SubscribeInputRouter();
            SubscribeBall();
        }

        void OnDisable()
        {
            UnsubscribeInputRouter();
            UnsubscribeBall();
        }

        void OnDestroy()
        {
            UnsubscribeInputRouter();
            UnsubscribeBall();
        }

        public void ConfigureForTest(VolleyballRules rules, BallRig configuredBall)
        {
            Lifecycle = new MinigameLifecycle(DefaultTutorialSeconds, DefaultCountdownSeconds);
            Rules = rules ?? new VolleyballRules(targetScore, timeLimit, Lifecycle);
            if (rules != null) AdvanceRulesToPlayForTest();
            ball = configuredBall;
            CacheReferences();
            ResetRuntimeState();
            SubscribeInputRouter();
            SubscribeBall();
        }

        public void ConfigurePresentationForTest(TrajectoryPreview preview, BallShadow shadow)
        {
            trajectoryPreview = preview;
            ballShadow = shadow;
        }

        public void SimulateForTest(float dt)
        {
            float deltaTime = Mathf.Max(0f, dt);
            Lifecycle.Tick(deltaTime);
            if (PresentationPhase == MinigamePhase.Play) TickPlay(deltaTime);
        }

        public void BeginGesturePreparation(Vector2 swipeDirection)
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.Phase != MinigamePhase.Play || !ball.Snapshot.IsAttached) return;

            gesturePreparation = true;
            if (trajectoryPreview == null) return;
            GetLaunchParameters(ExpectedAction, out float force, out float curvature);
            trajectoryPreview.Refresh(swipeDirection, force, curvature);
            trajectoryPreview.SetVisible(true);
        }

        public void EndGesturePreparation()
        {
            gesturePreparation = false;
            trajectoryPreview?.SetVisible(false);
        }

        public void SubmitSwipe(Vector2 swipe, bool inReachZone, float timingAccuracy)
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play || Rules.Phase != MinigamePhase.Play) return;
            EndGesturePreparation();
            if (ball.Snapshot.IsInFlight || InFlightOwner == VolleyBallOwner.Opponent) return;

            BallContext context = CurrentContext;
            VolleyAction action = Rules.ResolveTouch(context, swipe, inReachZone, timingAccuracy);
            if (action == VolleyAction.Invalid || action != ExpectedAction || touchNumber >= TouchesPerPossession)
            {
                ResolvePossession(false);
                return;
            }
            if (!Rules.TryResolveAndLaunch(ball, context, swipe, inReachZone, timingAccuracy))
            {
                ResolvePossession(false);
                return;
            }

            touchNumber++;
            SuccessfulLaunchCount++;
            SelectedAction = action;
            InFlightOwner = VolleyBallOwner.Player;
        }

        // Court trigger volumes can call this directly; BallRig collision callbacks use it too.
        public void ResolveCourtContact(bool ownCourt)
        {
            if (PresentationPhase != MinigamePhase.Play || InFlightOwner == VolleyBallOwner.None) return;
            bool playerWon = InFlightOwner == VolleyBallOwner.Player && !ownCourt && touchNumber == TouchesPerPossession;
            ResolvePossession(playerWon);
        }

        public MinigameResult BuildResult() => terminalResolved && LastResult != null ? LastResult : Rules == null ? default : Rules.BuildResult();
        public MinigameHudState BuildHudState(bool directAccess = true) => CreateHudState();
        protected override MinigameHudState BuildHudState() => CreateHudState();

        protected override void TickPlay(float dt)
        {
            if (Rules == null) return;
            float deltaTime = Mathf.Max(0f, dt);
            if (Rules.Phase == MinigamePhase.Play) Rules.Tick(deltaTime);
            TickOpponentReturn(deltaTime);
            RefreshRuntimeState();
            ResolveTerminalState();
        }

        void CacheReferences()
        {
            if (inputRouter == null) inputRouter = GetComponent<GameplayInputRouter>();
            if (reachZone == null) reachZone = GetComponent<BoxCollider2D>();
        }

        void AdvanceRulesToPlayForTest()
        {
            if (Rules.Phase == MinigamePhase.Tutorial) Rules.Tick(DefaultTutorialSeconds);
            if (Rules.Phase == MinigamePhase.Countdown) Rules.Tick(DefaultCountdownSeconds);
        }

        // The Volleyball scene has no input bridge, so the controller owns the one swipe detector
        // the shared router feeds. Installed once so test-supplied detectors are never clobbered.
        void InstallProductionSwipeDetector()
        {
            if (productionSwipeDetector != null || inputRouter == null) return;
            productionSwipeDetector = new SwipeInputDetector();
            inputRouter.SetDetectors(null, null, null, null, productionSwipeDetector);
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed) return;
            inputRouter.OnSwipe += OnRouterSwipe;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed) return;
            inputRouter.OnSwipe -= OnRouterSwipe;
            inputRouterSubscribed = false;
        }

        void SubscribeBall()
        {
            if (ball == null || ballSubscribed) return;
            ball.Collided += OnBallCollided;
            ballSubscribed = true;
        }

        void UnsubscribeBall()
        {
            if (!ballSubscribed || ball == null) return;
            ball.Collided -= OnBallCollided;
            ballSubscribed = false;
        }

        void OnRouterSwipe(SwipeResult swipe)
        {
            if (PresentationPhase != MinigamePhase.Play) return;
            Vector2 direction = ToVector2(swipe.Direction);
            if (CurrentContext == BallContext.ApexNearNet && swipe.Direction == SwipeDirection.Right) direction = new Vector2(1f, -1f);
            SubmitSwipe(direction, InReachZone, CalculateTimingAccuracy(swipe));
        }

        void OnBallCollided(Collision2D collision)
        {
            if (ball == null || !ball.Snapshot.IsInFlight) return;
            ResolveCourtContact(ball.Body.position.x <= netX);
        }

        void ResolvePossession(bool playerWon)
        {
            if (resolvedPossessionToken == possessionToken) return;
            resolvedPossessionToken = possessionToken;
            CompletedRallyCount++;
            if (playerWon) Rules.AwardRallyPoint(); else Rules.AwardOpponentPoint();

            EndGesturePreparation();
            touchNumber = 0;
            SelectedAction = VolleyAction.Invalid;
            InFlightOwner = VolleyBallOwner.None;
            possessionToken++;
            AttachForNextReturn();
            ResolveTerminalState();
        }

        void AttachForNextReturn()
        {
            if (ball == null) return;
            Transform anchor = opponent != null ? opponent : player != null ? player : transform;
            ball.AttachTo(anchor);
            pendingOpponentReturn = !terminalResolved;
            opponentReturnDelay = VolleyReturnPattern.AuthoredDefault().CueLeadSeconds;
            bool counterplayUnlocked = CompletedRallyCount >= TouchesPerPossession;
            OpponentCounterCueVisible = pendingOpponentReturn && counterplayUnlocked;
            OpponentFakeCueVisible = pendingOpponentReturn && counterplayUnlocked;
        }

        void TickOpponentReturn(float deltaTime)
        {
            if (!pendingOpponentReturn || terminalResolved || Rules == null || Rules.Phase != MinigamePhase.Play) return;
            opponentReturnDelay -= deltaTime;
            if (opponentReturnDelay > 0f) return;
            pendingOpponentReturn = false;
            ClearCounterplayCues();
            ball.Launch(Vector2.left + Vector2.up * .25f, 5f, 0f);
            InFlightOwner = VolleyBallOwner.Opponent;
        }

        BallContext CalculateContext()
        {
            if (ball == null) return BallContext.Low;
            BallFlightSnapshot snapshot = ball.Snapshot;
            if (ball.IsNearApex(apexVelocityThreshold) && Mathf.Abs(snapshot.Position.x - netX) <= netApexWindow) return BallContext.ApexNearNet;
            return ball.Body.velocity.y > 0f ? BallContext.Rising : BallContext.Low;
        }

        bool CalculateReach() => ball != null && reachZone != null && reachZone.bounds.Contains(ball.Body.position);
        float CalculateTimingAccuracy(SwipeResult swipe) => Mathf.Clamp01(1f - (float)swipe.Duration / Mathf.Max(Mathf.Epsilon, timingWindowSeconds));

        void RefreshRuntimeState()
        {
            if (ball == null) return;
            PredictedLandingPoint = ball.PredictLandingPoint();
            MoveActorToPrediction(player, playerLandingOffset);
            MoveActorToPrediction(teammate, teammateLandingOffset);
            ballShadow?.Refresh();
        }

        void MoveActorToPrediction(Transform actor, float horizontalOffset)
        {
            if (actor == null) return;
            actor.position = new Vector3(PredictedLandingPoint.x + horizontalOffset, actor.position.y, actor.position.z);
        }

        void ClearCounterplayCues()
        {
            OpponentCounterCueVisible = false;
            OpponentFakeCueVisible = false;
        }

        void ResolveTerminalState()
        {
            if (terminalResolved || Rules == null || PresentationPhase != MinigamePhase.Play) return;
            if (Rules.Phase != MinigamePhase.Resolve && !Rules.BuildResult().Pass) return;
            terminalResolved = true;
            pendingOpponentReturn = false;
            LastResult = Rules.BuildResult();
            EndGesturePreparation();
            ClearCounterplayCues();
            Finish(LastResult);
        }

        void ResetRuntimeState()
        {
            terminalResolved = false;
            gesturePreparation = false;
            pendingOpponentReturn = false;
            possessionToken = 0;
            resolvedPossessionToken = -1;
            touchNumber = 0;
            CompletedRallyCount = 0;
            InFlightOwner = VolleyBallOwner.None;
            SuccessfulLaunchCount = 0;
            SelectedAction = VolleyAction.Invalid;
            PredictedLandingPoint = Vector2.zero;
            LastResult = default;
            ClearCounterplayCues();
            if (ball != null) ball.AttachTo(player != null ? player : transform);
        }

        static VolleyAction ActionForTouch(int touch) => touch switch { 1 => VolleyAction.Dig, 2 => VolleyAction.Set, 3 => VolleyAction.Spike, _ => VolleyAction.Invalid };
        static void GetLaunchParameters(VolleyAction action, out float force, out float curvature)
        {
            force = action == VolleyAction.Spike ? 8f : 5f;
            curvature = action == VolleyAction.Spike ? .15f : 0f;
        }
        static Vector2 ToVector2(SwipeDirection direction) => direction switch { SwipeDirection.Left => Vector2.left, SwipeDirection.Right => Vector2.right, SwipeDirection.Up => Vector2.up, SwipeDirection.Down => Vector2.down, _ => Vector2.zero };

        MinigameHudState CreateHudState()
        {
            if (Rules == null) return MinigameHudState.Empty;
            string status = OpponentFakeCueVisible ? "COUNTER THE FAKE" : !InReachZone ? "MOVE INTO REACH" : $"TOUCH {Mathf.Clamp(touchNumber + 1, 1, TouchesPerPossession)}/{TouchesPerPossession}";
            return new MinigameHudState(PresentationPhase.ToString(), Mathf.Max(0f, timeLimit - Rules.Elapsed), Mathf.Clamp01(PlayerScore / (float)Mathf.Max(1, targetScore)), Rules.TotalTouches == 0 ? 0f : Rules.AccurateTouches / (float)Rules.TotalTouches, Rules.BuildResult().Score, status);
        }
    }
}
