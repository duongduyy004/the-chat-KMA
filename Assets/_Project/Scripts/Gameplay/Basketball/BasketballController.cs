using System.Collections.Generic;
using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class BasketballController : MinigameBase
    {
        const float DefaultTutorialSeconds = 2f;
        const float DefaultCountdownSeconds = 3f;
        const int ChargeBandSamples = 201;

        [SerializeField] int targetBaskets = 5;
        [SerializeField] float timeLimit = 60f;
        [SerializeField] BallRig ball;
        [SerializeField] GameplayInputRouter inputRouter;
        [SerializeField] Transform playerHand;
        [SerializeField] Transform finisher;
        [SerializeField] BoxCollider2D finisherBounds;
        [SerializeField] TrajectoryPreview trajectoryPreview;
        [SerializeField] BallShadow ballShadow;
        [SerializeField] float passAngleCentreDegrees = 47.5f;
        [SerializeField, Range(0f, 1f)] float minimumChargeRatio = .05f;
        [SerializeField] float alleyOopLeadSeconds = .35f;
        [SerializeField, Min(0f)] float minimumSwipeLengthPixels = .5f;
        [SerializeField] float finisherLandingOffset = -.4f;
        [SerializeField] BasketballDifficultyStep[] difficultySteps =
        {
            new BasketballDifficultyStep(.60f, 35f),
            new BasketballDifficultyStep(.45f, 35f),
            new BasketballDifficultyStep(.45f, 45f),
            new BasketballDifficultyStep(.30f, 45f),
            new BasketballDifficultyStep(.30f, 55f)
        };

        TapMashInputDetector productionTapDetector;
        HoldInputDetector productionHoldDetector;
        SwipeInputDetector productionSwipeDetector;
        bool inputRouterSubscribed;
        bool terminalResolved;
        bool pendingAlleyOop;
        float alleyOopDelay;
        float chargeElapsed;
        float launchSecondsToApex;
        int cachedBandStep = -1;
        float cachedBandLaunchY = float.NaN;

        public BasketballRules Rules { get; private set; }
        public AlleyOopPattern AuthoredBand { get; private set; }
        public BallRig Ball => ball;
        public GameplayInputRouter InputRouter => inputRouter;
        public Transform PlayerHand => playerHand;
        public Transform Finisher => finisher;
        public TrajectoryPreview Preview => trajectoryPreview;
        public BallShadow Shadow => ballShadow;

        public bool IsCharging { get; private set; }
        public float ChargeRatio { get; private set; }
        public float PassAngleDegrees => AngleForCharge(ChargeRatio);
        public Vector2 PendingPassVector { get; private set; }
        public float TargetChargeMin { get; private set; }
        public float TargetChargeMax { get; private set; }

        public Vector2 PredictedApexPoint { get; private set; }
        public float SecondsToApex { get; private set; }
        public float FlightApexProgress { get; private set; }
        public bool FinishCueVisible { get; private set; }
        public FinishJudge LastJudge { get; private set; } = FinishJudge.Ignored;
        public Vector2 PredictedLandingPoint { get; private set; }

        public int Baskets => Rules == null ? 0 : Rules.Baskets;
        public int Attempts => Rules == null ? 0 : Rules.Attempts;
        public int BestCombo => Rules == null ? 0 : Rules.BestCombo;
        public MinigameResult LastResult { get; private set; }

        public int DifficultyStep => Mathf.Clamp(Baskets, 0, Mathf.Max(0, difficultySteps.Length - 1));
        public float FinishCueLeadSeconds => CurrentStep.finishCueLeadSeconds;
        public float ChargeAngleSpanDegrees => CurrentStep.chargeAngleSpanDegrees;
        public IReadOnlyList<BasketballDifficultyStep> DifficultySteps => difficultySteps;
        public float AlleyOopLeadSecondsForTest => alleyOopLeadSeconds;
        public bool HasProductionDetectors =>
            productionTapDetector != null && productionHoldDetector != null && productionSwipeDetector != null;

        BasketballDifficultyStep CurrentStep => difficultySteps.Length == 0
            ? new BasketballDifficultyStep(.6f, 35f)
            : difficultySteps[DifficultyStep];

        protected override void Awake()
        {
            base.Awake();
            CacheReferences();
            InstallProductionDetectors();
            // Rules get their own lifecycle on purpose: TapFinish calls BeginResolve itself when
            // the objective completes, and a shared lifecycle would make MinigameBase.Finish
            // return false so Completed would never fire. See the plan's Reference section.
            Rules = new BasketballRules(targetBaskets, timeLimit);
            AuthoredBand = AlleyOopPattern.AuthoredDefault(Vector2.right);
            ResetRuntimeState();
        }

        void OnEnable()
        {
            CacheReferences();
            SubscribeInputRouter();
        }

        void OnDisable() => UnsubscribeInputRouter();
        void OnDestroy() => UnsubscribeInputRouter();

        void CacheReferences()
        {
            if (inputRouter == null) inputRouter = GetComponent<GameplayInputRouter>();
            if (inputRouter == null) inputRouter = FindFirstObjectByType<GameplayInputRouter>();
            if (finisherBounds == null) finisherBounds = GetComponent<BoxCollider2D>();
            if (inputRouter == null)
                Debug.LogError("BasketballController found no GameplayInputRouter; the scene accepts no gestures.", this);
        }

        // The Basketball scene has no input bridge, so the controller owns the three detectors the
        // shared router feeds. Each slot is replaced through its own additive setter, so a
        // detector another owner installed on the same router survives.
        void InstallProductionDetectors()
        {
            if (inputRouter == null || HasProductionDetectors) return;
            productionTapDetector = new TapMashInputDetector();
            productionHoldDetector = new HoldInputDetector();
            productionSwipeDetector = new SwipeInputDetector();
            inputRouter.SetTapMashDetector(productionTapDetector);
            inputRouter.SetHoldDetector(productionHoldDetector);
            inputRouter.SetSwipeDetector(productionSwipeDetector);
        }

        void SubscribeInputRouter()
        {
            if (inputRouter == null || inputRouterSubscribed) return;
            inputRouter.OnTap += OnRouterTap;
            inputRouter.OnHoldEnd += OnRouterHoldEnd;
            inputRouter.OnSwipe += OnRouterSwipe;
            inputRouterSubscribed = true;
        }

        void UnsubscribeInputRouter()
        {
            if (!inputRouterSubscribed) return;
            inputRouter.OnTap -= OnRouterTap;
            inputRouter.OnHoldEnd -= OnRouterHoldEnd;
            inputRouter.OnSwipe -= OnRouterSwipe;
            inputRouterSubscribed = false;
        }

        // Pointer down starts the charge. In flight the same pointer down is the finishing tap,
        // which TapFinish already separates by state, so no extra mode flag is needed.
        void OnRouterTap()
        {
            if (PresentationPhase != MinigamePhase.Play) return;
            if (Rules != null && Rules.State == BasketballState.AlleyOopFlight)
            {
                SubmitFinishTap();
                return;
            }

            BeginCharge();
        }

        // The hold always resolves before the swipe on the same pointer up, so the charge this
        // release belongs to is already final when the swipe arrives.
        void OnRouterHoldEnd(double duration)
        {
            if (!IsCharging || productionHoldDetector == null) return;
            ChargeRatio = Mathf.Clamp01((float)productionHoldDetector.ChargeRatio);
        }

        // A stationary press reports a zero delta, which the detector calls a Right swipe. The
        // pass already requires a real charge, so the swipe only has to say the finger left.
        void OnRouterSwipe(SwipeResult swipe)
        {
            if (PresentationPhase != MinigamePhase.Play || !IsCharging) return;
            if (swipe.Length < minimumSwipeLengthPixels)
            {
                CancelCharge();
                return;
            }

            SubmitPass(ChargeRatio, swipe.Direction);
        }

        public void BeginCharge()
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.State != BasketballState.Holding) return;

            IsCharging = true;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            RefreshTargetChargeBand();
        }

        public void CancelCharge()
        {
            IsCharging = false;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            trajectoryPreview?.SetVisible(false);
        }

        public void SubmitPass(float chargeRatio01, SwipeDirection direction)
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play ||
                Rules.State != BasketballState.Holding) return;

            float charge = Mathf.Clamp01(chargeRatio01);
            if (charge < minimumChargeRatio)
            {
                CancelCharge();
                return;
            }

            ChargeRatio = charge;
            PendingPassVector = PassVectorForCharge(charge, direction);
            if (!Rules.TryPass(ball, PendingPassVector))
            {
                CancelCharge();
                return;
            }

            IsCharging = false;
            pendingAlleyOop = true;
            alleyOopDelay = alleyOopLeadSeconds;
            trajectoryPreview?.SetVisible(false);
        }

        float AngleForCharge(float charge01)
        {
            float span = ChargeAngleSpanDegrees;
            return passAngleCentreDegrees - span * .5f + span * Mathf.Clamp01(charge01);
        }

        // The horizontal sign follows the swipe so a left-handed layout still works; the elevation
        // is the charge. Force stays owned by AlleyOopPattern, which normalises this vector.
        Vector2 PassVectorForCharge(float charge01, SwipeDirection direction)
        {
            float radians = AngleForCharge(charge01) * Mathf.Deg2Rad;
            float sign = direction == SwipeDirection.Left ? -1f : 1f;
            return new Vector2(sign * Mathf.Cos(radians), Mathf.Sin(radians));
        }

        // Scans the charge range for the sub-range whose apex lands inside the authored band, so
        // the glowing HUD band is derived from the same pattern TryPass will create. Recomputed
        // only when the difficulty step or the launch height changes - never per frame.
        void RefreshTargetChargeBand()
        {
            float launchY = LaunchOrigin.y;
            if (cachedBandStep == DifficultyStep && Mathf.Approximately(cachedBandLaunchY, launchY)) return;

            cachedBandStep = DifficultyStep;
            cachedBandLaunchY = launchY;
            var min = float.NaN;
            var max = float.NaN;
            for (var sample = 0; sample < ChargeBandSamples; sample++)
            {
                float charge = sample / (float)(ChargeBandSamples - 1);
                float apexY = ApexHeightForCharge(charge);
                if (apexY < AuthoredBand.ApexMin || apexY > AuthoredBand.ApexMax) continue;
                if (float.IsNaN(min)) min = charge;
                max = charge;
            }

            TargetChargeMin = float.IsNaN(min) ? 0f : min;
            TargetChargeMax = float.IsNaN(max) ? 0f : max;
        }

        float ApexHeightForCharge(float charge01)
        {
            float radians = AngleForCharge(charge01) * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * AuthoredBand.LaunchForce;
            return PredictApex(LaunchOrigin, velocity, ProfileGravity, ProfileDrag,
                Time.fixedDeltaTime, out _).y;
        }

        Vector2 LaunchOrigin => playerHand != null
            ? (Vector2)playerHand.position
            : ball != null ? ball.Body.position : Vector2.zero;

        Vector2 ProfileGravity => Physics2D.gravity * (ball != null && ball.Profile != null ? ball.Profile.GravityScale : 1f);
        float ProfileDrag => ball != null && ball.Profile != null ? ball.Profile.LinearDrag : 0f;

        // Mirrors the integrator BallRig/Ballistics own, so the ring closes on the frame the ball
        // actually stops rising instead of on a closed-form estimate that linear drag invalidates.
        public static Vector2 PredictApex(Vector2 position, Vector2 velocity, Vector2 gravity,
            float linearDrag, float deltaTime, out float secondsToApex, int maxSteps = 10000)
        {
            secondsToApex = 0f;
            if (deltaTime <= 0f || maxSteps <= 0 || velocity.y <= 0f) return position;

            Vector2 current = velocity;
            Vector2 currentPosition = position;
            for (var step = 0; step < maxSteps; step++)
            {
                Vector2 next = Ballistics.AdvanceVelocity(current, gravity, 0f, linearDrag, deltaTime);
                if (next.y <= 0f) return currentPosition;
                currentPosition += next * deltaTime;
                secondsToApex += deltaTime;
                current = next;
            }

            return currentPosition;
        }

        protected override void TickPlay(float dt)
        {
            if (Rules == null) return;
            float deltaTime = Mathf.Max(0f, dt);

            // The controller owns the deadline. Rules would otherwise flip their own lifecycle to
            // Resolve and the terminal path would then skip its Play guard, leaving the scene with
            // no Result panel on a timeout - exactly the S9 Volleyball failure.
            if (Rules.Phase == MinigamePhase.Play)
            {
                if (Rules.Elapsed + deltaTime >= timeLimit)
                {
                    ResolveTerminal();
                    return;
                }

                Rules.Tick(deltaTime);
            }

            if (IsCharging)
            {
                chargeElapsed += deltaTime;
                ChargeRatio = Mathf.Clamp01(chargeElapsed);   // HoldInputDetector maxChargeSeconds is 1
                Rules.Hold(deltaTime);
                RefreshChargePreview();
            }

            TickAlleyOopLaunch(deltaTime);
            RefreshFlightState();
            ResolveTerminalState();
        }

        void TickAlleyOopLaunch(float deltaTime)
        {
            if (!pendingAlleyOop || Rules.State != BasketballState.Passing) return;
            alleyOopDelay -= deltaTime;
            if (alleyOopDelay > 0f) return;

            pendingAlleyOop = false;
            if (!Rules.TryLaunchAlleyOop(ball)) return;

            PredictApex(ball.Body.position, ball.Body.velocity, ProfileGravity, ProfileDrag,
                Time.fixedDeltaTime, out launchSecondsToApex);
            launchSecondsToApex = Mathf.Max(launchSecondsToApex, Mathf.Epsilon);
        }

        void RefreshFlightState()
        {
            if (ball == null) return;
            PredictedLandingPoint = ball.PredictLandingPoint();

            if (Rules.State != BasketballState.AlleyOopFlight || !ball.Snapshot.IsInFlight)
            {
                FinishCueVisible = false;
                FlightApexProgress = 0f;
                SecondsToApex = 0f;
                ballShadow?.Refresh();
                return;
            }

            PredictedApexPoint = PredictApex(ball.Body.position, ball.Body.velocity, ProfileGravity,
                ProfileDrag, Time.fixedDeltaTime, out float remaining);
            SecondsToApex = remaining;
            FlightApexProgress = Mathf.Clamp01(1f - remaining / launchSecondsToApex);
            // Strict less-than, and guarded with Mathf.Approximately: for the widest authored lead
            // (step 0), the discrete, quantized flight time to the true apex and the lead constant
            // land on the same physics-step boundary, so the freshly-launched "remaining" can come
            // out a few float-ULPs under the lead purely from step quantization, not because the
            // ball is genuinely inside the window yet. Treating that near-tie as "not yet" (the same
            // tolerance Unity's own Mathf.Approximately uses) keeps the cue off at launch without
            // weakening the window once the ball is actually within it.
            FinishCueVisible = remaining < FinishCueLeadSeconds && !Mathf.Approximately(remaining, FinishCueLeadSeconds);
            MoveFinisherToPrediction();
            ballShadow?.Refresh();
        }

        // Transforms only. The assist must never write to the ball body, or it fights the physics
        // BallRig owns and walks both the actor and the ball off the court.
        void MoveFinisherToPrediction()
        {
            if (finisher == null) return;
            float target = PredictedLandingPoint.x + finisherLandingOffset;
            if (finisherBounds != null)
            {
                Bounds bounds = finisherBounds.bounds;
                target = Mathf.Clamp(target, bounds.min.x, bounds.max.x);
            }

            finisher.position = new Vector3(target, finisher.position.y, finisher.position.z);
        }

        public void SubmitFinishTap()
        {
            if (Rules == null || ball == null || PresentationPhase != MinigamePhase.Play)
            {
                LastJudge = FinishJudge.Ignored;
                return;
            }

            LastJudge = Rules.TapFinish(ball.Body.position.y, ball.Body.velocity.y);
            if (LastJudge == FinishJudge.Ignored) return;

            FinishCueVisible = false;
            FlightApexProgress = 0f;
            SecondsToApex = 0f;
            if (Rules.State == BasketballState.Holding) AttachForNextAttempt();
            ResolveTerminalState();
        }

        void AttachForNextAttempt()
        {
            CancelCharge();
            cachedBandStep = -1;                 // the difficulty step just moved
            RefreshTargetChargeBand();
            ball.AttachTo(playerHand != null ? playerHand : transform);
            SyncBallTransform();
        }

        // BallRig.AttachTo assigns Rigidbody2D.position directly. After the ball has spent a real
        // flight as a Dynamic body, Unity does not carry that assignment over to the ball's own
        // Transform within the same frame. BasketballRules.TryPass (immutable) re-attaches via
        // ball.AttachTo(ball.transform) on the very next pass, which reads the Transform - without
        // this explicit copy it would read the stale, still-mid-air position and launch the next
        // alley-oop from the wrong height.
        void SyncBallTransform()
        {
            Vector2 position = ball.Body.position;
            Vector3 current = ball.transform.position;
            ball.transform.position = new Vector3(position.x, position.y, current.z);
        }

        void RefreshChargePreview()
        {
            if (trajectoryPreview == null) return;
            Vector2 direction = PassVectorForCharge(ChargeRatio, SwipeDirection.Right);
            trajectoryPreview.Refresh(direction, AuthoredBand.LaunchForce, AuthoredBand.Curvature);
            trajectoryPreview.SetVisible(true);
        }

        void ResolveTerminalState()
        {
            if (terminalResolved || Rules == null || PresentationPhase != MinigamePhase.Play) return;
            if (!Rules.PrimaryObjectiveComplete) return;
            ResolveTerminal();
        }

        void ResolveTerminal()
        {
            if (terminalResolved || PresentationPhase != MinigamePhase.Play) return;
            terminalResolved = true;
            pendingAlleyOop = false;
            CancelCharge();
            FinishCueVisible = false;
            LastResult = Rules.BuildResult();
            Finish(LastResult);
        }

        void ResetRuntimeState()
        {
            terminalResolved = false;
            pendingAlleyOop = false;
            IsCharging = false;
            chargeElapsed = 0f;
            ChargeRatio = 0f;
            PendingPassVector = Vector2.zero;
            LastJudge = FinishJudge.Ignored;
            FinishCueVisible = false;
            FlightApexProgress = 0f;
            SecondsToApex = 0f;
            launchSecondsToApex = 1f;
            PredictedApexPoint = Vector2.zero;
            PredictedLandingPoint = Vector2.zero;
            LastResult = default;
            cachedBandStep = -1;
            RefreshTargetChargeBand();
            if (ball != null)
            {
                ball.AttachTo(playerHand != null ? playerHand : transform);
                SyncBallTransform();   // see SyncBallTransform's comment
            }
        }

        // Deviation from the brief: MinigameResult is a sealed class, not a struct, and a
        // legitimately stored failing result (Score == 0 from ScoreUtil.Build(pass: false, ...))
        // must not be discarded and silently recomputed. A reference-null check is the correct
        // guard here, matching VolleyballController's precedent.
        public MinigameResult BuildResult() =>
            terminalResolved && LastResult != null ? LastResult : Rules == null ? default : Rules.BuildResult();

        public MinigameHudState BuildHudState(bool directAccess = true) => CreateHudState();
        protected override MinigameHudState BuildHudState() => CreateHudState();

        MinigameHudState CreateHudState()
        {
            if (Rules == null) return MinigameHudState.Empty;
            string status = FinishCueVisible ? "TAP AT THE APEX"
                : Rules.State == BasketballState.AlleyOopFlight ? "WAIT FOR THE APEX"
                : IsCharging ? "RELEASE IN THE BAND"
                : "HOLD TO CHARGE";
            return new MinigameHudState(
                PresentationPhase.ToString(),
                Mathf.Max(0f, timeLimit - Rules.Elapsed),
                Mathf.Clamp01(Baskets / (float)Mathf.Max(1, targetBaskets)),
                Mathf.Clamp01(ChargeRatio),
                Rules.BuildResult().Score,
                status);
        }

        public void ConfigureSceneRefsForTest(BallRig configuredBall, GameplayInputRouter router,
            Transform hand, Transform finisherActor)
        {
            ball = configuredBall;
            inputRouter = router;
            playerHand = hand;
            finisher = finisherActor;
            InstallProductionDetectors();
            SubscribeInputRouter();
            ResetRuntimeState();
        }

        public void ConfigurePresentationForTest(TrajectoryPreview preview, BallShadow shadow)
        {
            trajectoryPreview = preview;
            ballShadow = shadow;
        }

        // Closes the tutorial gate the same way PhaseOverlay does in the build, so the test path
        // and the production path share one lifecycle.
        public void SkipTutorialForTest()
        {
            SetTutorialGate(false);
            Lifecycle.Tick(DefaultCountdownSeconds);
        }

        public void SimulateForTest(float dt)
        {
            float deltaTime = Mathf.Max(0f, dt);
            Lifecycle.Tick(deltaTime);
            if (PresentationPhase == MinigamePhase.Play) TickPlay(deltaTime);
        }
    }
}
