using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class VolleyballControllerTests : InputTestFixture
    {
        readonly List<Object> temporaryObjects = new List<Object>();

        public override void TearDown()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
                Object.DestroyImmediate(temporaryObjects[index]);
            temporaryObjects.Clear();
            base.TearDown();
        }

        [Test]
        public void ConfigureForTest_ExposesConfiguredRulesBallAndLiveStateInPlay()
        {
            var fixture = CreateFixture();

            AdvanceControllerToPlay(fixture.Controller);

            Assert.That(fixture.Controller.Rules, Is.SameAs(fixture.Rules));
            Assert.That(fixture.Controller.Ball, Is.SameAs(fixture.Ball));
            Assert.That(fixture.Controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(fixture.Rules.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.LongestCombo, Is.Zero);
        }

        [TestCase(BallContext.Low, 0f, -1f, VolleyAction.Dig, 5f, 0f)]
        [TestCase(BallContext.Rising, 0f, 1f, VolleyAction.Set, 5f, 0f)]
        [TestCase(BallContext.ApexNearNet, 1f, -1f, VolleyAction.Spike, 8f, .15f)]
        public void SubmitSwipe_ResolvesEachAuthoredActionThroughRulesAndLaunchesOnce(
            BallContext expectedContext,
            float swipeX,
            float swipeY,
            VolleyAction expectedAction,
            float expectedLaunchForce,
            float expectedCurvature)
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, expectedContext);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(expectedContext));

            fixture.Controller.SubmitSwipe(new Vector2(swipeX, swipeY), inReachZone: true, timingAccuracy: 1f);
            BallFlightSnapshot launched = fixture.Ball.Snapshot;
            int launchCountAfterFirst = fixture.Controller.SuccessfulLaunchCount;
            fixture.Controller.SubmitSwipe(new Vector2(swipeX, swipeY), inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(launchCountAfterFirst, Is.EqualTo(1));
            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(1));
            Assert.That(launched.IsInFlight, Is.True);
            Assert.That(launched.Velocity.magnitude, Is.EqualTo(expectedLaunchForce).Within(.001f));
            Assert.That(Vector2.Distance(launched.Velocity, ExpectedLaunchDirection(expectedAction).normalized * expectedLaunchForce), Is.LessThan(.001f));
            Assert.That(launched.Curvature, Is.EqualTo(expectedCurvature).Within(.001f));
            Assert.That(Vector2.Distance(fixture.Ball.Snapshot.Velocity, launched.Velocity), Is.LessThan(.001f));
            Assert.That(fixture.Ball.Snapshot.Curvature, Is.EqualTo(launched.Curvature).Within(.001f));
        }

        [UnityTest]
        public IEnumerator SubmitSwipe_KeepsSingleLaunchGuardClosedAfterBallPhysicsChangesVelocity()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            Vector2 launchVelocity = fixture.Ball.Snapshot.Velocity;

            yield return new WaitForFixedUpdate();

            Vector2 velocityAfterPhysicsTick = fixture.Ball.Snapshot.Velocity;
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            Assert.That(Vector2.Distance(velocityAfterPhysicsTick, launchVelocity), Is.GreaterThan(.0001f));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.Velocity, Is.EqualTo(velocityAfterPhysicsTick));
        }

        [Test]
        public void SubmitSwipe_RejectsOutOfReachAndBelowThresholdWithoutChangingTouchOrScore()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: false, timingAccuracy: 1f);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: .74f);

            Assert.That(fixture.Rules.TotalTouches, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.False);
        }

        [TestCase(SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up)]
        [TestCase(SwipeDirection.Down)]
        public void GameplayInputRouter_OnSwipeRoutesEveryCardinalDirectionToController(SwipeDirection direction)
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            RouteSwipe(fixture.SwipeDetector, direction);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
        }

        [Test]
        public void GameplayInputRouter_OnSwipePromotesRightGestureToDownRightSpikeAtNetApex()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.ApexNearNet);

            RouteSwipe(fixture.SwipeDetector, SwipeDirection.Right);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(Vector2.Distance(fixture.Ball.Snapshot.Velocity, new Vector2(1f, .75f).normalized * 8f), Is.LessThan(.001f));
            Assert.That(fixture.Ball.Snapshot.Curvature, Is.EqualTo(.15f).Within(.001f));
        }

        [Test]
        public void ContextAndReach_AreCalculatedFromBallVelocityNetWindowAndReachBounds()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);

            SetBallForContext(fixture, BallContext.Low);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            SetBallForContext(fixture, BallContext.Rising);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Rising));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            SetBallForContext(fixture, BallContext.ApexNearNet);
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.ApexNearNet));
            Assert.That(fixture.Controller.InReachZone, Is.True);

            fixture.Ball.Body.position = new Vector2(4f, 2f);
            fixture.Ball.Body.velocity = Vector2.down;
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.InReachZone, Is.False);
        }

        [Test]
        public void AutoPositioning_UsesBallPredictionWithoutMutatingBallPhysics()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.Launch(new Vector2(1f, 1f), 4f, .25f);
            fixture.Ball.Body.position = new Vector2(.25f, 2f);
            fixture.Ball.Body.velocity = new Vector2(3f, 2f);
            Vector2 prediction = fixture.Ball.PredictLandingPoint();
            Vector2 position = fixture.Ball.Body.position;
            Vector2 velocity = fixture.Ball.Body.velocity;

            fixture.Controller.SimulateForTest(Time.fixedDeltaTime);

            Assert.That(fixture.Controller.PredictedLandingPoint.x, Is.EqualTo(prediction.x).Within(.001f));
            Assert.That(fixture.Controller.PredictedLandingPoint.y, Is.EqualTo(prediction.y).Within(.001f));
            Assert.That(fixture.Ball.Body.position, Is.EqualTo(position));
            Assert.That(fixture.Ball.Body.velocity, Is.EqualTo(velocity));
        }

        [Test]
        public void ThirdValidTouch_ShowsCounterplayCuesWithoutChangingPointsOrPrediction()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);

            for (int touch = 0; touch < 3; touch++)
            {
                SetBallForContext(fixture, BallContext.Low);
                fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            }
            Vector2 predictionBeforeCueFrame = fixture.Ball.PredictLandingPoint();
            fixture.Controller.SimulateForTest(0f);

            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.OpponentCounterCueVisible, Is.True);
            Assert.That(fixture.Controller.OpponentFakeCueVisible, Is.True);
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.PredictedLandingPoint.x, Is.EqualTo(predictionBeforeCueFrame.x).Within(.001f));
            Assert.That(fixture.Controller.PredictedLandingPoint.y, Is.EqualTo(predictionBeforeCueFrame.y).Within(.001f));

            SetBallForContext(fixture, BallContext.Low);
            Assert.That(fixture.Controller.OpponentCounterCueVisible, Is.True);
            Assert.That(fixture.Controller.OpponentFakeCueVisible, Is.True);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(4));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
        }

        [Test]
        public void HudAndCompletion_FollowRulesResultAndCompleteOnlyOnce()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);

            Assert.That(fixture.Controller.BuildHudState().statusText, Is.EqualTo("TOUCH 1/2/3"));
            fixture.Rules.AwardRallyPoint();
            Assert.That(fixture.Controller.BuildHudState().score, Is.EqualTo(fixture.Rules.BuildResult().Score));

            int completions = 0;
            fixture.Controller.Completed += _ => completions++;
            fixture.Rules.AwardRallyPoint();
            fixture.Controller.SimulateForTest(0f);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            fixture.Controller.SimulateForTest(1f);

            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void ConfigureForTest_WaitsForPresentationPlayBeforeCompletingPreResolvedRules()
        {
            var fixture = CreateFixture(preResolveRules: true);
            var observedPhases = new List<MinigamePhase>();
            var completions = 0;
            fixture.Controller.PhaseChanged += observedPhases.Add;
            fixture.Controller.Completed += _ => completions++;

            fixture.Controller.SimulateForTest(2f);

            Assert.That(fixture.Controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
            Assert.That(completions, Is.Zero);

            fixture.Controller.SimulateForTest(3f);

            Assert.That(observedPhases, Does.Contain(MinigamePhase.Play));
            Assert.That(fixture.Controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(completions, Is.EqualTo(1));
        }

        ControllerFixture CreateFixture(bool preResolveRules = false)
        {
            var controllerObject = new GameObject("VolleyballControllerTest");
            temporaryObjects.Add(controllerObject);
            controllerObject.AddComponent<BoxCollider2D>().size = new Vector2(2f, 4f);
            var router = controllerObject.AddComponent<GameplayInputRouter>();
            var controller = controllerObject.AddComponent<VolleyballController>();

            var ballObject = new GameObject("VolleyballBallTest");
            temporaryObjects.Add(ballObject);
            ballObject.AddComponent<Rigidbody2D>();
            var ball = ballObject.AddComponent<BallRig>();
            var profile = FlightProfile.Create(1f, 0f, -10f, 1f);
            temporaryObjects.Add(profile);
            ball.SetProfile(profile);

            var detector = new SwipeInputDetector();
            router.SetDetectors(null, null, null, null, detector);
            var rules = new VolleyballRules(targetScore: 2, timeLimit: 60f);
            if (preResolveRules)
            {
                rules.Tick(2f);
                rules.Tick(3f);
                Assert.That(rules.BeginResolve(), Is.True);
            }
            controller.ConfigureForTest(rules, ball);
            return new ControllerFixture(controller, rules, ball, detector);
        }

        static void AdvanceControllerToPlay(VolleyballController controller)
        {
            controller.SimulateForTest(2f);
            controller.SimulateForTest(3f);
        }

        static void SetBallForContext(ControllerFixture fixture, BallContext context)
        {
            fixture.Ball.AttachTo(null);
            fixture.Ball.Body.position = context == BallContext.ApexNearNet
                ? new Vector2(0f, 2f)
                : new Vector2(0f, 1f);
            fixture.Ball.Body.velocity = context switch
            {
                BallContext.Low => new Vector2(0f, -2f),
                BallContext.Rising => new Vector2(0f, 2f),
                BallContext.ApexNearNet => Vector2.zero,
                _ => throw new System.ArgumentOutOfRangeException(nameof(context), context, null)
            };
        }

        static Vector2 ExpectedLaunchDirection(VolleyAction action) => action switch
        {
            VolleyAction.Dig => Vector2.up,
            VolleyAction.Set => new Vector2(1f, 1.5f),
            VolleyAction.Spike => new Vector2(1f, .75f),
            _ => throw new System.ArgumentOutOfRangeException(nameof(action), action, null)
        };

        static void RouteSwipe(SwipeInputDetector detector, SwipeDirection direction)
        {
            Vector2 end = direction switch
            {
                SwipeDirection.Left => Vector2.left,
                SwipeDirection.Right => Vector2.right,
                SwipeDirection.Up => Vector2.up,
                SwipeDirection.Down => Vector2.down,
                _ => throw new System.ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
            detector.FeedSample(Vector2.zero, 0d);
            detector.FeedSample(end * 100f, 1d);
            detector.FeedEnd();
        }

        readonly struct ControllerFixture
        {
            public ControllerFixture(VolleyballController controller, VolleyballRules rules, BallRig ball, SwipeInputDetector swipeDetector)
            {
                Controller = controller;
                Rules = rules;
                Ball = ball;
                SwipeDetector = swipeDetector;
            }

            public VolleyballController Controller { get; }
            public VolleyballRules Rules { get; }
            public BallRig Ball { get; }
            public SwipeInputDetector SwipeDetector { get; }
        }
    }
}
