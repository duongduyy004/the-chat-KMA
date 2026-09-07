using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BasketballControllerTests
    {
        GameObject root;
        BasketballController controller;
        BallRig ball;
        GameplayInputRouter router;
        Transform hand;
        Transform finisher;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("basketball-controller-fixture");

            var ballObject = new GameObject("ball");
            ballObject.transform.SetParent(root.transform, false);
            ballObject.AddComponent<Rigidbody2D>();
            ball = ballObject.AddComponent<BallRig>();
            ball.SetProfile(FlightProfile.Create(1f, .02f, 0f, .8f));

            var routerObject = new GameObject("router");
            routerObject.transform.SetParent(root.transform, false);
            router = routerObject.AddComponent<GameplayInputRouter>();

            var handObject = new GameObject("hand");
            handObject.transform.SetParent(root.transform, false);
            handObject.transform.position = new Vector3(-3.5f, 1.2f, 0f);
            hand = handObject.transform;

            var finisherObject = new GameObject("finisher");
            finisherObject.transform.SetParent(root.transform, false);
            finisherObject.transform.position = Vector3.zero;
            finisher = finisherObject.transform;

            var controllerObject = new GameObject("controller");
            controllerObject.transform.SetParent(root.transform, false);
            controller = controllerObject.AddComponent<BasketballController>();
            controller.ConfigureSceneRefsForTest(ball, router, hand, finisher);
            controller.SkipTutorialForTest();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void Construction_UsesAPrivateRulesLifecycleSoTheControllerStillOwnsResolve()
        {
            Assert.That(controller.Rules, Is.Not.Null);
            Assert.That(controller.Ball, Is.SameAs(ball));
            Assert.That(controller.InputRouter, Is.SameAs(router));
            Assert.That(controller.PlayerHand, Is.SameAs(hand));
            Assert.That(controller.Finisher, Is.SameAs(finisher));
            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play),
                "SkipTutorialForTest must reach Play without a second lifecycle.");
            Assert.That(controller.Rules.Phase, Is.EqualTo(MinigamePhase.Play),
                "The rules own a private lifecycle that is already in Play.");
            Assert.That(controller.HasProductionDetectors, Is.True,
                "The controller installs its own tap, hold and swipe detectors on the shared router.");
        }

        [Test]
        public void HoldingAttachesTheBallToTheHandSoTheLobLaunchesFromTheAuthoredHeight()
        {
            Assert.That(ball.Snapshot.IsAttached, Is.True);
            Assert.That(ball.Body.position.y, Is.EqualTo(1.2f).Within(.001f));
            Assert.That(ball.Body.position.x, Is.EqualTo(-3.5f).Within(.001f));
        }

        [Test]
        public void ChargeMapsToTheAuthoredAngleSpanAndReportsItLive()
        {
            controller.BeginCharge();
            Assert.That(controller.IsCharging, Is.True);

            controller.SimulateForTest(.5f);

            float span = controller.ChargeAngleSpanDegrees;
            Assert.That(span, Is.EqualTo(35f).Within(.001f), "Step 0 of the authored table opens at 35 degrees.");
            // Literal expected values, not the implementation's own formula: authored centre is
            // 47.5, step-0 span is 35 (range [30, 65]); a 0.5 charge gives 30 + 35*0.5 = 47.5.
            Assert.That(controller.ChargeRatio, Is.EqualTo(.5f).Within(.001f));
            Assert.That(controller.PassAngleDegrees, Is.EqualTo(47.5f).Within(.01f));
        }

        // The glowing band the HUD draws must be derived from the same authored pattern TryPass
        // will create, or the player is aiming at a lie.
        [Test]
        public void TargetChargeBand_MatchesTheAuthoredApexBandForTheCurrentLaunchHeight()
        {
            AlleyOopPattern authored = AlleyOopPattern.AuthoredDefault(Vector2.right);
            Assert.That(controller.AuthoredBand.ApexMin, Is.EqualTo(authored.ApexMin));
            Assert.That(controller.AuthoredBand.ApexMax, Is.EqualTo(authored.ApexMax));

            Assert.That(controller.TargetChargeMin, Is.GreaterThan(0f));
            Assert.That(controller.TargetChargeMax, Is.LessThan(1f));
            Assert.That(controller.TargetChargeMax, Is.GreaterThan(controller.TargetChargeMin));

            float inside = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            Assert.That(ApexHeightFor(inside), Is.InRange(authored.ApexMin, authored.ApexMax));
            Assert.That(ApexHeightFor(controller.TargetChargeMin - .06f), Is.LessThan(authored.ApexMin));
            Assert.That(ApexHeightFor(controller.TargetChargeMax + .06f), Is.GreaterThan(authored.ApexMax));
        }

        [Test]
        public void SubmitPass_BelowTheMinimumCharge_IsRejectedWithoutTouchingTheRules()
        {
            controller.BeginCharge();
            controller.SubmitPass(.02f, SwipeDirection.Right);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(controller.Attempts, Is.Zero);
            Assert.That(ball.Snapshot.IsInFlight, Is.False);
            // CancelCharge's observable effect: a rejected gesture must not leave the charge
            // running, or TickPlay keeps advancing it and the HUD stays stuck on "RELEASE IN THE BAND".
            Assert.That(controller.IsCharging, Is.False,
                "A rejected below-minimum charge must cancel, or the charge keeps advancing unattended.");
            Assert.That(controller.ChargeRatio, Is.Zero, "CancelCharge must reset the charge back to zero.");
        }

        // The production path that protects a player from losing a possession to an accidental
        // tap: a stationary press-then-release reports a zero-length swipe, which is below
        // minimumSwipeLengthPixels, so CancelCharge must fire the same as the direct-API case above.
        [Test]
        public void ProductionPointerGesture_ZeroLengthSwipe_CancelsTheChargeWithoutTouchingTheRules()
        {
            var point = new Vector2(500f, 400f);
            router.FeedPointerDownForTest(point, 0d);
            Assert.That(controller.IsCharging, Is.True, "Pointer down must start the charge.");

            router.FeedPointerUpForTest(point, .3d);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding));
            Assert.That(controller.Attempts, Is.Zero);
            Assert.That(controller.IsCharging, Is.False,
                "A zero-length swipe must cancel the charge, not silently keep it running.");
            Assert.That(controller.ChargeRatio, Is.Zero);
        }

        [Test]
        public void SubmitPass_AuthorsThePatternThenTheAiLaunchesItAfterTheAuthoredLead()
        {
            controller.BeginCharge();
            float charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            controller.SubmitPass(charge, SwipeDirection.Right);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Passing),
                "The swipe passes; the AI has not launched yet.");
            Assert.That(controller.Rules.AuthoredPattern, Is.Not.Null);
            Assert.That(controller.Rules.AuthoredPattern.PassVector,
                Is.EqualTo(controller.PendingPassVector).Using(Vector2Comparer.Instance));
            Assert.That(ball.Snapshot.IsInFlight, Is.False);

            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.AlleyOopFlight));
            Assert.That(ball.Snapshot.IsInFlight, Is.True);
            Assert.That(ball.Body.velocity.magnitude, Is.EqualTo(8f).Within(.01f),
                "The authored launch force stays owned by AlleyOopPattern.");
        }

        [UnityTest]
        public IEnumerator ApexPrediction_MatchesTheIntegratorAndClosesTheCueBeforeTheApex()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            Assert.That(controller.SecondsToApex, Is.GreaterThan(0f));
            Assert.That(controller.FinishCueVisible, Is.False,
                "The cue must not be up while the apex is still far away.");
            float lead = controller.FinishCueLeadSeconds;
            Assert.That(lead, Is.EqualTo(.6f).Within(.001f), "Step 0 of the authored table leads by 0.6s.");

            float apexHeight = controller.PredictedApexPoint.y;
            Assert.That(apexHeight, Is.InRange(controller.AuthoredBand.ApexMin, controller.AuthoredBand.ApexMax));

            var sawCue = false;
            float guard = Time.unscaledTime + 10f;
            while (ball.Body.velocity.y > 0f && Time.unscaledTime < guard)
            {
                if (controller.FinishCueVisible)
                {
                    sawCue = true;
                    Assert.That(controller.SecondsToApex, Is.LessThanOrEqualTo(lead + .03f));
                }
                yield return new WaitForFixedUpdate();
            }

            Assert.That(sawCue, Is.True, "The finish cue must appear before the apex, not after it.");
            Assert.That(controller.FlightApexProgress, Is.EqualTo(1f).Within(.06f));
            Assert.That(ball.Body.position.y, Is.EqualTo(apexHeight).Within(.12f),
                "The predicted apex must match where the ball actually stops rising.");
        }

        [UnityTest]
        public IEnumerator TapAtTheApexOfACorrectlyChargedLob_ScoresAPerfectBasket()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();

            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Perfect));
            Assert.That(controller.Baskets, Is.EqualTo(1));
            Assert.That(controller.Attempts, Is.EqualTo(1));
            Assert.That(controller.BestCombo, Is.EqualTo(1));
            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Holding),
                "An unfinished objective returns to Holding for the next attempt.");
            Assert.That(ball.Snapshot.IsAttached, Is.True, "The next attempt starts in the player's hand.");
        }

        [UnityTest]
        public IEnumerator TapAtTheApexOfAnUnderChargedLob_IsEarlyAndBreaksTheCombo()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();
            controller.SubmitFinishTap();
            Assert.That(controller.BestCombo, Is.EqualTo(1));

            yield return LaunchAtCharge(controller.TargetChargeMin - .12f);
            yield return WaitForApex();
            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Early),
                "A lob that apexes below the authored band cannot be finished, however well timed.");
            Assert.That(controller.Baskets, Is.EqualTo(1));
            Assert.That(controller.Attempts, Is.EqualTo(2));
            Assert.That(controller.Rules.Combo, Is.Zero);
            Assert.That(controller.BestCombo, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TapBeforeTheApexWindowOpens_IsEarlyAndCostsAnAttempt()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Early));
            Assert.That(controller.Attempts, Is.EqualTo(1));
            Assert.That(controller.Baskets, Is.Zero);
        }

        [Test]
        public void FinishTapOutsideFlight_IsIgnoredAndCostsNothing()
        {
            controller.SubmitFinishTap();

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Ignored));
            Assert.That(controller.Attempts, Is.Zero);
        }

        // Spec S10: each step raises exactly one axis - a narrower timing window OR a harder
        // alley-oop path, never both.
        [Test]
        public void AuthoredDifficultyTable_ChangesExactlyOneAxisPerStep()
        {
            var steps = controller.DifficultySteps;
            Assert.That(steps.Count, Is.EqualTo(5), "One step per basket up to the five-basket objective.");

            for (var index = 1; index < steps.Count; index++)
            {
                bool timingChanged = !Mathf.Approximately(
                    steps[index].finishCueLeadSeconds, steps[index - 1].finishCueLeadSeconds);
                bool pathChanged = !Mathf.Approximately(
                    steps[index].chargeAngleSpanDegrees, steps[index - 1].chargeAngleSpanDegrees);

                Assert.That(timingChanged || pathChanged, Is.True, "Step " + index + " raises no axis.");
                Assert.That(timingChanged && pathChanged, Is.False, "Step " + index + " raises both axes.");
                Assert.That(steps[index].finishCueLeadSeconds,
                    Is.LessThanOrEqualTo(steps[index - 1].finishCueLeadSeconds), "Timing must not get easier.");
                Assert.That(steps[index].chargeAngleSpanDegrees,
                    Is.GreaterThanOrEqualTo(steps[index - 1].chargeAngleSpanDegrees), "The path must not get easier.");
            }
        }

        // Pins the authored table's literal values, not just the invariant above. Without this,
        // a transposed .50/.45 or a 40/50-for-45/55 typo in Task 4's array would still satisfy
        // every inequality-based assertion in this file.
        //
        // Task 7's balance pass owns these five numbers. If it retunes the table, THIS test must
        // be updated in the same diff - that friction is deliberate, so a deliberate tuning change
        // is visible in review and a transcription typo is not.
        [Test]
        public void AuthoredDifficultyTable_MatchesThePlanExactly()
        {
            var steps = controller.DifficultySteps;
            Assert.That(steps.Count, Is.EqualTo(5));

            var expected = new[]
            {
                (finishCueLeadSeconds: .60f, chargeAngleSpanDegrees: 35f),
                (finishCueLeadSeconds: .45f, chargeAngleSpanDegrees: 35f),
                (finishCueLeadSeconds: .45f, chargeAngleSpanDegrees: 45f),
                (finishCueLeadSeconds: .30f, chargeAngleSpanDegrees: 45f),
                (finishCueLeadSeconds: .30f, chargeAngleSpanDegrees: 55f),
            };

            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(steps[index].finishCueLeadSeconds, Is.EqualTo(expected[index].finishCueLeadSeconds).Within(.001f),
                    "Step " + index + " finishCueLeadSeconds does not match the authored table.");
                Assert.That(steps[index].chargeAngleSpanDegrees, Is.EqualTo(expected[index].chargeAngleSpanDegrees).Within(.001f),
                    "Step " + index + " chargeAngleSpanDegrees does not match the authored table.");
            }
        }

        [UnityTest]
        public IEnumerator DifficultyStepFollowsTheBasketCountAndNarrowsTheTargetBand()
        {
            Assert.That(controller.DifficultyStep, Is.Zero);
            float bandAtStepZero = controller.TargetChargeMax - controller.TargetChargeMin;

            yield return ScoreOneBasket();
            Assert.That(controller.DifficultyStep, Is.EqualTo(1));
            Assert.That(controller.FinishCueLeadSeconds, Is.LessThan(.6f), "Step 1 narrows the timing axis.");
            Assert.That(controller.TargetChargeMax - controller.TargetChargeMin,
                Is.EqualTo(bandAtStepZero).Within(.001f), "Step 1 must not also change the path axis.");

            yield return ScoreOneBasket();
            Assert.That(controller.DifficultyStep, Is.EqualTo(2));
            Assert.That(controller.TargetChargeMax - controller.TargetChargeMin,
                Is.LessThan(bandAtStepZero), "Step 2 narrows the path axis.");
        }

        [UnityTest]
        public IEnumerator FinisherChasesThePredictedLandingWithoutTouchingTheBallBody()
        {
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);

            Vector2 predicted = ball.PredictLandingPoint();
            Vector2 velocityBefore = ball.Body.velocity;
            Vector2 positionBefore = ball.Body.position;

            yield return new WaitForFixedUpdate();

            Assert.That(controller.PredictedLandingPoint.x, Is.EqualTo(ball.PredictLandingPoint().x).Within(.001f));
            Assert.That(finisher.position.x, Is.Not.EqualTo(0f).Within(.0001f),
                "The finisher must move toward the predicted landing point.");
            Assert.That(ball.Body.velocity, Is.Not.EqualTo(velocityBefore).Using(Vector2Comparer.Instance),
                "The ball keeps integrating; the assist must not freeze it.");
            Assert.That(ball.Body.position, Is.Not.EqualTo(positionBefore).Using(Vector2Comparer.Instance));
            Assert.That(predicted.y, Is.EqualTo(0f).Within(.001f), "The profile ground plane owns the landing height.");
        }

        [UnityTest]
        public IEnumerator FifthBasket_FinishesOnceWithAPassingResult()
        {
            var completions = 0;
            MinigameResult observed = default;
            controller.Completed += result => { completions++; observed = result; };

            for (var basket = 0; basket < 5; basket++)
                yield return ScoreOneBasket();

            Assert.That(controller.Baskets, Is.EqualTo(5));
            Assert.That(completions, Is.EqualTo(1),
                "Completed must fire exactly once - the rules' own BeginResolve must not swallow it.");
            Assert.That(observed.Pass, Is.True);
            Assert.That(observed.Score, Is.GreaterThan(0f));
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));

            MinigameResult stored = controller.BuildResult();
            Assert.That(stored, Is.Not.Null, "BuildResult must not go back to a fresh Rules result after Completed already fired.");
            Assert.That(stored.Pass, Is.EqualTo(observed.Pass),
                "BuildResult must keep returning the same result Completed delivered, not a stale or recomputed one.");
            Assert.That(stored.Score, Is.EqualTo(observed.Score).Within(.0001f));

            controller.SubmitFinishTap();
            controller.SimulateForTest(1f);

            Assert.That(completions, Is.EqualTo(1), "Input after the resolve must not complete the subject twice.");
        }

        [Test]
        public void Deadline_ResolvesOnceWithAFailingResultWhenTheObjectiveIsIncomplete()
        {
            var completions = 0;
            MinigameResult observed = default;
            controller.Completed += result => { completions++; observed = result; };

            controller.SimulateForTest(61f);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(observed.Pass, Is.False);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
        }

        [Test]
        public void HudState_ReportsPhaseTimerBasketProgressAndStatus()
        {
            MinigameHudState state = controller.BuildHudState();

            Assert.That(state.phase, Is.EqualTo(MinigamePhase.Play.ToString()));
            Assert.That(state.timeRemaining, Is.EqualTo(60f).Within(.5f));
            Assert.That(state.progress01, Is.Zero);
            Assert.That(state.statusText, Is.EqualTo("HOLD TO CHARGE"));

            controller.BeginCharge();
            controller.SimulateForTest(.4f);

            Assert.That(controller.BuildHudState().statusText, Is.EqualTo("RELEASE IN THE BAND"));
        }

        // Basketball has no input bridge, so the controller owns three detectors on the scene's
        // one router. This drives the production dispatch, not the SubmitX helpers.
        [UnityTest]
        public IEnumerator ProductionPointerGesture_ChargesPassesAndFinishesThroughTheRouter()
        {
            var start = new Vector2(600f, 400f);
            router.FeedPointerDownForTest(start, 0d);

            Assert.That(controller.IsCharging, Is.True, "Pointer down must start the charge.");

            float charge = (controller.TargetChargeMin + controller.TargetChargeMax) * .5f;
            double release = charge;   // HoldInputDetector default maxChargeSeconds is 1 second.
            router.FeedPointerMoveForTest(start + new Vector2(180f, 0f), release * .5d);
            router.FeedPointerUpForTest(start + new Vector2(240f, 0f), release);

            Assert.That(controller.Rules.State, Is.EqualTo(BasketballState.Passing),
                "OnHoldEnd then OnSwipe must complete the pass in one gesture.");
            Assert.That(controller.ChargeRatio, Is.EqualTo(charge).Within(.02f));

            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);
            yield return WaitForApex();

            router.FeedPointerDownForTest(start, 10d);
            router.FeedPointerUpForTest(start, 10.05d);

            Assert.That(controller.LastJudge, Is.EqualTo(FinishJudge.Perfect),
                "A tap during flight must reach TapFinish through the router's tap detector.");
            Assert.That(controller.Baskets, Is.EqualTo(1));
        }

        IEnumerator LaunchAtCharge(float charge)
        {
            controller.BeginCharge();
            controller.SubmitPass(charge, SwipeDirection.Right);
            controller.SimulateForTest(controller.AlleyOopLeadSecondsForTest);
            Assert.That(ball.Snapshot.IsInFlight, Is.True, "The AI must have launched the alley-oop.");
            yield return new WaitForFixedUpdate();
        }

        IEnumerator WaitForApex()
        {
            float guard = Time.unscaledTime + 10f;
            while (ball.Body.velocity.y > 0f && Time.unscaledTime < guard)
                yield return new WaitForFixedUpdate();

            Assert.That(ball.Body.velocity.y, Is.LessThanOrEqualTo(0f), "The ball never reached its apex.");
        }

        IEnumerator ScoreOneBasket()
        {
            int before = controller.Baskets;
            yield return LaunchAtCharge((controller.TargetChargeMin + controller.TargetChargeMax) * .5f);
            yield return WaitForApex();
            controller.SubmitFinishTap();
            Assert.That(controller.Baskets, Is.EqualTo(before + 1),
                "The centred charge plus an apex tap must score. Judge: " + controller.LastJudge);
        }

        // The apex height a given charge produces, simulated with the integrator BallRig owns.
        float ApexHeightFor(float charge01)
        {
            float span = controller.ChargeAngleSpanDegrees;
            float angle = 47.5f - span * .5f + span * Mathf.Clamp01(charge01);
            float radians = angle * Mathf.Deg2Rad;
            var velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 8f;
            return BasketballController.PredictApex(hand.position, velocity,
                Physics2D.gravity * ball.Profile.GravityScale, ball.Profile.LinearDrag,
                Time.fixedDeltaTime, out _).y;
        }

        sealed class Vector2Comparer : System.Collections.Generic.IEqualityComparer<Vector2>
        {
            public static readonly Vector2Comparer Instance = new Vector2Comparer();
            public bool Equals(Vector2 left, Vector2 right) => Vector2.Distance(left, right) < .0001f;
            public int GetHashCode(Vector2 value) => value.GetHashCode();
        }
    }
}
