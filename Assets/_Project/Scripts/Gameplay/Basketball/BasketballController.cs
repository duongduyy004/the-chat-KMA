using System.Collections.Generic;
using KMA.Gameplay.UI;
using KMA.Input;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class BasketballController : MinigameBase
    {
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
            // The invariant TryPass needs is "body and transform agree at the moment it reads the
            // Transform" (see SyncBallTransform's comment) - enforced here, at the read site, not
            // at whichever earlier attach happened to run last, so it still holds even if something
            // moves the hand (an animated actor, say) between an attach and this pass.
            SyncBallTransform();
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
                Time.fixedDeltaTime, out _, AuthoredBand.Curvature).y;
        }

        Vector2 LaunchOrigin => playerHand != null
            ? (Vector2)playerHand.position
            : ball != null ? ball.Body.position : Vector2.zero;

        Vector2 ProfileGravity => Physics2D.gravity * (ball != null && ball.Profile != null ? ball.Profile.GravityScale : 1f);
        float ProfileDrag => ball != null && ball.Profile != null ? ball.Profile.LinearDrag : 0f;

        // Mirrors the integrator BallRig/Ballistics own, so the ring closes on the frame the ball
        // actually stops rising instead of on a closed-form estimate that linear drag invalidates.
        // curvature defaults to 0 (today's authored value) so existing callers - including the
        // test file's own independent recomputation - are unaffected; passing AuthoredBand.Curvature
        // explicitly keeps this prediction from silently diverging from the simulation if a future
        // balance pass ever tunes curvature away from zero.
        public static Vector2 PredictApex(Vector2 position, Vector2 velocity, Vector2 gravity,
            float linearDrag, float deltaTime, out float secondsToApex, float curvature = 0f, int maxSteps = 10000)
        {
            secondsToApex = 0f;
            // A FlightProfile with non-negative effective gravity would never satisfy the loop's
            // own descent check (next.y <= 0f), so it would run the full maxSteps every single
            // sample - RefreshTargetChargeBand's 201-sample scan would then cost 2,010,000
            // iterations inside BeginCharge. Bail out before that, the same way the other
            // physically-impossible inputs below already do.
            if (deltaTime <= 0f || maxSteps <= 0 || velocity.y <= 0f || gravity.y >= 0f) return position;

            Vector2 current = velocity;
            Vector2 currentPosition = position;
            for (var step = 0; step < maxSteps; step++)
            {
                Vector2 next = Ballistics.AdvanceVelocity(current, gravity, curvature, linearDrag, deltaTime);
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
                // The bar the player watches fill must be the same clock as the value OnRouterHoldEnd
                // will submit, or a hitch, a pause, or a backgrounded frame (Time.deltaTime not
                // tracking real elapsed time) makes the displayed charge under- or over-read relative
                // to what actually gets submitted. Whenever the production hold detector is the one
                // actually driving this hold - the only way a real player can ever reach this branch,
                // since BeginCharge() is only ever called directly (bypassing the detector) by tests -
                // its own Time.realtimeSinceStartupAsDouble-based ratio is the single source of truth
                // for both the live display and the value FeedUp will commit at release. The
                // chargeElapsed fallback below only serves that direct-API test path, where the
                // detector was never engaged; HoldInputDetector's maxChargeSeconds is 1, so chargeElapsed
                // maps onto the same 0..1 range.
                ChargeRatio = productionHoldDetector != null && productionHoldDetector.IsHolding
                    ? Mathf.Clamp01((float)productionHoldDetector.CurrentRatio(Time.realtimeSinceStartupAsDouble))
                    : Mathf.Clamp01(chargeElapsed);
                Rules.Hold(deltaTime);
                RefreshChargePreview();
            }

            TickAlleyOopLaunch(deltaTime);
            RefreshFlightState();
            ResolveWhiffOnGroundContact();
            ResolveTerminalState();
        }

        void TickAlleyOopLaunch(float deltaTime)
        {
            if (!pendingAlleyOop || Rules.State != BasketballState.Passing) return;
            alleyOopDelay -= deltaTime;
            if (alleyOopDelay > 0f) return;

            pendingAlleyOop = false;
            if (!Rules.TryLaunchAlleyOop(ball))
            {
                // Unreachable today (the authored pattern always launches after its lead), but if it
                // ever did fail the player would otherwise be soft-locked in Passing with no cue until
                // the 60s deadline - log loudly rather than fail silently.
                Debug.LogError("BasketballController: TryLaunchAlleyOop failed after the authored lead " +
                    "elapsed; the player is stuck in Passing until the timeout.", this);
                return;
            }

            BallFlightSnapshot launchSnapshot = ball.Snapshot;
            PredictApex(launchSnapshot.Position, launchSnapshot.Velocity, ProfileGravity, ProfileDrag,
                Time.fixedDeltaTime, out launchSecondsToApex, AuthoredBand.Curvature);
            launchSecondsToApex = Mathf.Max(launchSecondsToApex, Mathf.Epsilon);
        }

        void RefreshFlightState()
        {
            if (ball == null) return;
            PredictedLandingPoint = ball.PredictLandingPoint();

            BallFlightSnapshot snapshot = ball.Snapshot;
            if (Rules.State != BasketballState.AlleyOopFlight || !snapshot.IsInFlight)
            {
                FinishCueVisible = false;
                FlightApexProgress = 0f;
                SecondsToApex = 0f;
                ballShadow?.Refresh();
                return;
            }

            PredictedApexPoint = PredictApex(snapshot.Position, snapshot.Velocity, ProfileGravity,
                ProfileDrag, Time.fixedDeltaTime, out float remaining, AuthoredBand.Curvature);
            SecondsToApex = remaining;
            FlightApexProgress = Mathf.Clamp01(1f - remaining / launchSecondsToApex);
            // PredictApex truncates rather than interpolating the apex-crossing step, so it
            // quantizes "remaining" to whole multiples of Time.fixedDeltaTime. At the widest
            // authored lead the freshly-launched "remaining" can therefore land a few float-ULPs
            // under FinishCueLeadSeconds purely from that quantization, not because the ball is
            // genuinely inside the window. A tolerance at the ULP scale (Mathf.Approximately) would
            // defend a boundary five orders of magnitude finer than the quantity being compared, so
            // it would flip with any change to fixed timestep, gravity, launch force or
            // passAngleCentreDegrees. Defending the boundary at the quantization's own scale - half
            // a fixed step - is the tolerance that actually matches what is being compared.
            //
            // The cue must also never latch through the descent: once the ball is at or past its
            // true apex, PredictApex early-returns remaining=0, which would otherwise read as
            // "always within the lead" for the entire fall.
            bool ascending = snapshot.Velocity.y > 0f;
            FinishCueVisible = ascending && remaining <= FinishCueLeadSeconds - Time.fixedDeltaTime * .5f;
            MoveFinisherToPrediction();
            ballShadow?.Refresh();
        }

        // A flight that reaches the ground without a tap is a whiff: it must consume the attempt
        // and return the ball to the hand, the same as a genuine late tap, rather than leaving the
        // possession stuck until the 60s deadline. The non-positive-velocity guard keeps the launch
        // frame itself (which can start at or below the ground plane before its first upward step)
        // from tripping this early - only a ball that is actually descending can whiff.
        void ResolveWhiffOnGroundContact()
        {
            if (Rules == null || ball == null || Rules.State != BasketballState.AlleyOopFlight) return;
            BallFlightSnapshot snapshot = ball.Snapshot;
            if (snapshot.Velocity.y > 0f) return;
            float groundY = ball.Profile != null ? ball.Profile.GroundY : 0f;
            if (snapshot.Position.y > groundY) return;
            SubmitFinishTap();
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
        }

        // BallRig.AttachTo assigns Rigidbody2D.position directly. After the ball has spent a real
        // flight as a Dynamic body, Unity does not carry that assignment over to the ball's own
        // Transform within the same frame. BasketballRules.TryPass (immutable) re-attaches via
        // ball.AttachTo(ball.transform) on its own very next call, which reads the Transform -
        // without a fresh copy it would read a stale, still-mid-air position and launch the next
        // alley-oop from the wrong height. Called from SubmitPass, immediately before TryPass reads
        // it, rather than from wherever the ball was last attached - the read site is the only place
        // that actually needs the invariant to hold, and the only place guaranteed to run right
        // before it matters even if something else moves the hand in between (Task 5's animated
        // actor, for instance).
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
            if (ball != null) ball.AttachTo(playerHand != null ? playerHand : transform);
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
            // Unsubscribe from whatever router OnEnable already found (e.g. via
            // FindFirstObjectByType) before swapping it out - otherwise that subscription stays
            // live while SubscribeInputRouter below early-returns on inputRouterSubscribed already
            // being true, so the new router silently never gets hooked up.
            UnsubscribeInputRouter();
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
