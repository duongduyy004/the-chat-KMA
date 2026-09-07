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
        static readonly Vector2 PlainReturnDirection = Vector2.left + Vector2.up;
        static readonly Vector2 CounterplayReturnDirection = Vector2.left + Vector2.up * 1.6f;
        const float CounterplayReturnCurvature = .2f;

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
        // Vertical speed within which the ball counts as at its apex. Sized so the spike window
        // lasts a few hundred milliseconds instead of a single physics step.
        [SerializeField] float apexVelocityThreshold = 1.5f;
        [SerializeField] float netApexWindow = 1f;
        [SerializeField] float opponentReturnForce = 7f;
        [SerializeField] float timingWindowSeconds = 4f;
        [SerializeField, Min(0f)] float minimumSwipeLengthPixels = 24f;
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
        bool hasLastFlightObservation;
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
        public bool HasProductionSwipeDetector => productionSwipeDetector != null;
        public bool LastReturnWasCounterplay { get; private set; }

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

        public void ConfigureActorsForTest(Transform playerActor, Transform teammateActor, Transform opponentActor)
        {
            player = playerActor;
            teammate = teammateActor;
            opponent = opponentActor;
        }

        public void ConfigureReachForTest(Vector2 offset, Vector2 size)
        {
            if (reachZone == null) return;
            reachZone.offset = offset;
            reachZone.size = size;
        }

        public void ScheduleOpponentReturnForTest() => ScheduleOpponentReturn();

        public void SimulateForTest(float dt)
        {
            float deltaTime = Mathf.Max(0f, dt);
            Lifecycle.Tick(deltaTime);
            if (PresentationPhase == MinigamePhase.Play) TickPlay(deltaTime);
        }

        // The preview depicts the authored trajectory for the expected action, which is what the
        // launch will actually use - a screen-space swipe direction would draw the wrong arc.
        public void BeginGesturePreparation()
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.Phase != MinigamePhase.Play || !ball.Snapshot.IsAttached) return;

            gesturePreparation = true;
            if (trajectoryPreview == null || ExpectedAction == VolleyAction.Invalid) return;
            GetLaunchParameters(ExpectedAction, out Vector2 direction, out float force, out float curvature);
            trajectoryPreview.Refresh(direction, force, curvature);
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
            MarkFlightRestarted();

            // The opponent cannot put a new ball in play while the player owns this one, so a
            // scheduled return is cancelled the moment the possession starts.
            if (touchNumber == 1)
            {
                pendingOpponentReturn = false;
                ClearCounterplayCues();
            }
        }

        // Court trigger volumes can call this directly; BallRig collision callbacks use it too.
        // An opponent-owned ball landing on the opponent half is the player's point - the mirror
        // of a player fault, which the owner check alone used to award to the opponent.
        public void ResolveCourtContact(bool ownCourt)
        {
            if (PresentationPhase != MinigamePhase.Play || InFlightOwner == VolleyBallOwner.None) return;

            bool playerWon = InFlightOwner == VolleyBallOwner.Opponent
                ? !ownCourt
                : !ownCourt && touchNumber == TouchesPerPossession;
            ResolvePossession(playerWon);
        }

        public MinigameResult BuildResult() => terminalResolved && LastResult != null ? LastResult : Rules == null ? default : Rules.BuildResult();
        public MinigameHudState BuildHudState(bool directAccess = true) => CreateHudState();
        protected override MinigameHudState BuildHudState() => CreateHudState();

        protected override void TickPlay(float dt)
        {
            if (Rules == null) return;
            float deltaTime = Mathf.Max(0f, dt);

            // The controller owns the deadline. Rules would otherwise flip the shared lifecycle
            // to Resolve itself, and ResolveTerminalState would then skip its own Play guard and
            // never call Finish - leaving the scene with no Result panel on a timeout.
            if (Rules.Phase == MinigamePhase.Play)
            {
                if (Rules.Elapsed + deltaTime >= timeLimit)
                {
                    ResolveDeadline();
                    return;
                }

                Rules.Tick(deltaTime);
            }

            TickOpponentReturn(deltaTime);
            ResolveGroundedFlight();
            RefreshRuntimeState();
            ResolveTerminalState();
        }

        void CacheReferences()
        {
            if (inputRouter == null) inputRouter = GetComponent<GameplayInputRouter>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<GameplayInputRouter>();
            if (reachZone == null) reachZone = GetComponent<BoxCollider2D>();
            if (inputRouter == null)
                Debug.LogError("VolleyballController found no GameplayInputRouter; the scene accepts no gestures.", this);
        }

        void AdvanceRulesToPlayForTest()
        {
            if (Rules.Phase == MinigamePhase.Tutorial) Rules.Tick(DefaultTutorialSeconds);
            if (Rules.Phase == MinigamePhase.Countdown) Rules.Tick(DefaultCountdownSeconds);
        }

        // The Volleyball scene has no input bridge, so the controller owns the swipe detector the
        // shared router feeds. Only the swipe slot is replaced, and only during Awake, so a
        // detector another owner or a test installs afterwards stays in place.
        void InstallProductionSwipeDetector()
        {
            if (productionSwipeDetector != null || inputRouter == null) return;
            productionSwipeDetector = new SwipeInputDetector();
            inputRouter.SetSwipeDetector(productionSwipeDetector);
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed) return;
            inputRouter.OnSwipe += OnRouterSwipe;
            inputRouter.OnSwipeProgress += OnRouterSwipeProgress;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed) return;
            inputRouter.OnSwipe -= OnRouterSwipe;
            inputRouter.OnSwipeProgress -= OnRouterSwipeProgress;
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

        // A stationary press has a zero delta, which the detector reports as a Right swipe; that
        // would otherwise consume a touch, so gestures shorter than the threshold are ignored.
        void OnRouterSwipe(SwipeResult swipe)
        {
            if (PresentationPhase != MinigamePhase.Play) return;
            if (swipe.Length < minimumSwipeLengthPixels) return;
            Vector2 direction = ToVector2(swipe.Direction);
            if (CurrentContext == BallContext.ApexNearNet && swipe.Direction == SwipeDirection.Right) direction = new Vector2(1f, -1f);
            SubmitSwipe(direction, InReachZone, CalculateTimingAccuracy(swipe));
        }

        void OnRouterSwipeProgress(Vector2 delta)
        {
            if (PresentationPhase != MinigamePhase.Play) return;
            if (delta.sqrMagnitude < minimumSwipeLengthPixels * minimumSwipeLengthPixels) return;
            BeginGesturePreparation();
        }

        void OnBallCollided(Collision2D collision)
        {
            if (ball == null || !ball.Snapshot.IsInFlight) return;
            ResolveCourtContact(ball.Body.position.x <= netX);
        }

        // The ball has no collider, so flight resolves against the profile ground plane that
        // BallRig and Ballistics already own, plus the authored net and court bounds. Court
        // trigger volumes may still call ResolveCourtContact directly; the possession token keeps
        // either path single-shot. A launch is given one step before it can resolve, because every
        // anchor sits on the ground plane.
        void ResolveGroundedFlight()
        {
            if (ball == null || !ball.Snapshot.IsInFlight || InFlightOwner == VolleyBallOwner.None) return;

            if (!hasLastFlightObservation)
            {
                // Every anchor sits on the ground plane, so a launch is given one observed step
                // to leave it before a landing can resolve.
                hasLastFlightObservation = true;
                return;
            }

            Vector2 position = ball.Body.position;
            if (position.y > GroundPlane || ball.Body.velocity.y > 0f) return;
            ResolveCourtContact(position.x <= netX);
        }

        void MarkFlightRestarted() => hasLastFlightObservation = false;

        float GroundPlane => ball != null && ball.Profile != null ? ball.Profile.GroundY : 0f;

        void ResolvePossession(bool playerWon)
        {
            if (resolvedPossessionToken == possessionToken) return;
            resolvedPossessionToken = possessionToken;
            if (playerWon)
            {
                // Counterplay unlocks after three *completed* rallies, so only a won rally counts.
                CompletedRallyCount++;
                Rules.AwardRallyPoint();
            }
            else
            {
                Rules.AwardOpponentPoint();
            }

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
            ScheduleOpponentReturn();
        }

        void ScheduleOpponentReturn()
        {
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

            // The cue that has been showing for CueLeadSeconds must actually mean something, so a
            // flagged return launches the authored spin serve instead of the plain one. Curvature
            // is fixed at launch; nothing mutates a trajectory once it is in flight.
            bool counterplay = OpponentCounterCueVisible;
            pendingOpponentReturn = false;
            ClearCounterplayCues();

            // A 45-degree serve at force 5 has a range of only v2/g = 2.55 units, which cannot
            // cross the 4.5 units from the opponent anchor to the player. The authored return
            // needs enough energy to actually land in the player half.
            Vector2 direction = counterplay ? CounterplayReturnDirection : PlainReturnDirection;
            float curvature = counterplay ? CounterplayReturnCurvature : 0f;
            ball.Launch(direction, opponentReturnForce, curvature);
            InFlightOwner = VolleyBallOwner.Opponent;
            LastReturnWasCounterplay = counterplay;
            MarkFlightRestarted();
        }

        BallContext CalculateContext()
        {
            if (ball == null) return BallContext.Low;
            BallFlightSnapshot snapshot = ball.Snapshot;
            // A resting attached ball also has zero vertical speed, so the apex context is only
            // offered to a ball that is actually flying.
            if (!snapshot.IsAttached && ball.IsNearApex(apexVelocityThreshold) &&
                Mathf.Abs(snapshot.Position.x - netX) <= netApexWindow) return BallContext.ApexNearNet;
            return ball.Body.velocity.y > 0f ? BallContext.Rising : BallContext.Low;
        }

        bool CalculateReach() => ball != null && reachZone != null && reachZone.bounds.Contains(ball.Body.position);
        float CalculateTimingAccuracy(SwipeResult swipe) => Mathf.Clamp01(1f - (float)swipe.Duration / Mathf.Max(Mathf.Epsilon, timingWindowSeconds));

        // The assist only runs during a live flight. An attached ball is pinned to its anchor by
        // BallRig, so its predicted landing is the anchor's own position - moving the anchor there
        // plus an offset would walk both the actor and the ball off the court a unit per frame.
        void RefreshRuntimeState()
        {
            if (ball == null) return;
            PredictedLandingPoint = ball.PredictLandingPoint();
            if (InFlightOwner != VolleyBallOwner.None && !ball.Snapshot.IsAttached)
            {
                MoveActorToPrediction(player, playerLandingOffset);
                MoveActorToPrediction(teammate, teammateLandingOffset);
            }

            ballShadow?.Refresh();
        }

        // The player's own spike lands deep in the opponent half, so the assist target is clamped
        // to the player side of the net - otherwise both actors visibly teleport across it once
        // per rally while chasing a landing they are not allowed to reach.
        void MoveActorToPrediction(Transform actor, float horizontalOffset)
        {
            if (actor == null || reachZone == null) return;
            Bounds reach = reachZone.bounds;
            float target = Mathf.Clamp(PredictedLandingPoint.x + horizontalOffset, reach.min.x, reach.max.x);
            actor.position = new Vector3(target, actor.position.y, actor.position.z);
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
            ResolveTerminal();
        }

        // The deadline is terminal even though no point was scored on this frame.
        void ResolveDeadline()
        {
            if (terminalResolved || Rules == null || PresentationPhase != MinigamePhase.Play) return;
            ResolveTerminal();
        }

        void ResolveTerminal()
        {
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
        static void GetLaunchParameters(VolleyAction action, out Vector2 direction, out float force, out float curvature)
        {
            VolleyPhase phase = VolleyReturnPattern.PhaseFor(action);
            direction = VolleyReturnPattern.AuthoredDirection(phase);
            force = VolleyReturnPattern.AuthoredForce(phase);
            curvature = VolleyReturnPattern.AuthoredCurvature(phase);
        }
        static Vector2 ToVector2(SwipeDirection direction) => direction switch { SwipeDirection.Left => Vector2.left, SwipeDirection.Right => Vector2.right, SwipeDirection.Up => Vector2.up, SwipeDirection.Down => Vector2.down, _ => Vector2.zero };

        MinigameHudState CreateHudState()
        {
            if (Rules == null) return MinigameHudState.Empty;
            string status = OpponentFakeCueVisible ? "COUNTER THE FAKE" : !InReachZone ? "OUT OF REACH" : $"TOUCH {Mathf.Clamp(touchNumber + 1, 1, TouchesPerPossession)}/{TouchesPerPossession}";
            return new MinigameHudState(PresentationPhase.ToString(), Mathf.Max(0f, timeLimit - Rules.Elapsed), Mathf.Clamp01(PlayerScore / (float)Mathf.Max(1, targetScore)), Rules.TotalTouches == 0 ? 0f : Rules.AccurateTouches / (float)Rules.TotalTouches, Rules.BuildResult().Score, status);
        }
    }
}
