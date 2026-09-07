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
            SetBallForContext(fixture, BallContext.Low);

            Assert.That(fixture.Controller.Rules, Is.SameAs(fixture.Rules));
            Assert.That(fixture.Controller.Ball, Is.SameAs(fixture.Ball));
            Assert.That(fixture.Controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(fixture.Rules.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(fixture.Controller.CurrentContext, Is.EqualTo(BallContext.Low));
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.LongestCombo, Is.Zero);
        }

        [Test]
        public void SubmitSwipe_ResolvesDigSetSpikeInPossessionOrderAndLaunchesEachOnce()
        {
            var fixture = CreateFixture(targetScore: 5);
            AdvanceControllerToPlay(fixture.Controller);
            SubmitTouch(fixture, BallContext.Low, Vector2.down);
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(fixture.Controller.SelectedAction, Is.EqualTo(VolleyAction.Dig));
            Assert.That(fixture.Ball.Snapshot.Velocity.magnitude, Is.EqualTo(5f).Within(.001f));
            SubmitTouch(fixture, BallContext.Rising, Vector2.up);
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(2));
            Assert.That(fixture.Controller.SelectedAction, Is.EqualTo(VolleyAction.Set));
            SubmitTouch(fixture, BallContext.ApexNearNet, new Vector2(1f, -1f));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.SelectedAction, Is.EqualTo(VolleyAction.Spike));
            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(3));
            Assert.That(fixture.Ball.Snapshot.Velocity.magnitude, Is.EqualTo(8f).Within(.001f));
            Assert.That(fixture.Ball.Snapshot.Curvature, Is.EqualTo(.15f).Within(.001f));
        }

        // Touches two and three happen while the ball is airborne, so the possession order - not
        // the flight flag - is what stops one action being played twice.
        [UnityTest]
        public IEnumerator SubmitSwipe_RepeatingAConsumedActionFailsThePossessionAfterPhysicsTicks()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            Vector2 launchVelocity = fixture.Ball.Snapshot.Velocity;

            yield return new WaitForFixedUpdate();

            Vector2 velocityAfterPhysicsTick = fixture.Ball.Snapshot.Velocity;
            Assert.That(Vector2.Distance(velocityAfterPhysicsTick, launchVelocity), Is.GreaterThan(.0001f));
            Assert.That(fixture.Controller.ExpectedAction, Is.EqualTo(VolleyAction.Set));

            SetBallForContext(fixture, BallContext.Low);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(1),
                "A second Dig cannot be resolved once the possession has moved on to Set.");
            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(1));
            Assert.That(fixture.Controller.OpponentScore, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
        }

        // The production regression the scene gates missed: every touch after the first happens
        // while the ball is in flight, so the rally must complete without reattaching the ball.
        [UnityTest]
        public IEnumerator InFlightRally_CompletesDigSetSpikeAndScoresOnTheAuthoredLanding()
        {
            var fixture = CreateFixture(targetScore: 5);
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);

            yield return new WaitForFixedUpdate();
            SetBallInFlightState(fixture, BallContext.Rising);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
            fixture.Controller.SubmitSwipe(Vector2.up, inReachZone: true, timingAccuracy: 1f);
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(2),
                "The set must resolve while the dig is still airborne.");

            yield return new WaitForFixedUpdate();
            SetBallInFlightState(fixture, BallContext.ApexNearNet);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);
            fixture.Controller.SubmitSwipe(new Vector2(1f, -1f), inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.PlayerScore, Is.Zero, "The point is only awarded on landing.");

            yield return WaitForGroundedResolution(fixture);

            Assert.That(fixture.Controller.PlayerScore, Is.EqualTo(1));
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsAttached, Is.True,
                "Resolving the possession reattaches the ball for the next opponent return.");
        }

        // An unreturned ball must resolve itself: the controller watches the profile ground plane
        // that BallRig owns, so no collider or test poke is needed to end the possession.
        [UnityTest]
        public IEnumerator UnreturnedFlight_ResolvesOnTheGroundPlaneAndAwardsTheOpponent()
        {
            var fixture = CreateFixture(targetScore: 5);
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            yield return WaitForGroundedResolution(fixture);

            Assert.That(fixture.Controller.OpponentScore, Is.EqualTo(1),
                "Letting the ball land before the third touch loses the rally.");
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
        }

        // Receiving is the normal start of every rally after the first: the opponent's authored
        // return is airborne and owned by the opponent when the player digs it.
        [UnityTest]
        public IEnumerator OpponentReturn_IsReceivedByThePlayerAndCancelsTheNextScheduledReturn()
        {
            var fixture = CreateFixture(targetScore: 5);
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            yield return WaitForGroundedResolution(fixture);
            Assert.That(fixture.Controller.InFlightOwner, Is.EqualTo(VolleyBallOwner.None));

            fixture.Controller.SimulateForTest(VolleyReturnPattern.AuthoredDefault().CueLeadSeconds);

            Assert.That(fixture.Controller.InFlightOwner, Is.EqualTo(VolleyBallOwner.Opponent));
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True);

            SetBallInFlightState(fixture, BallContext.Low);
            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(1),
                "The player must be able to dig a return that is still in the opponent's flight.");
            Assert.That(fixture.Controller.InFlightOwner, Is.EqualTo(VolleyBallOwner.Player));

            fixture.Controller.SimulateForTest(VolleyReturnPattern.AuthoredDefault().CueLeadSeconds);

            Assert.That(fixture.Controller.InFlightOwner, Is.EqualTo(VolleyBallOwner.Player),
                "A scheduled opponent return must not hijack a possession the player already owns.");
        }

        [Test]
        public void GameplayInputRouter_IgnoresAStationaryPressThatReportsAZeroLengthSwipe()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.SwipeDetector.FeedSample(new Vector2(120f, 240f), 0d);
            fixture.SwipeDetector.FeedSample(new Vector2(120f, 240f), .05d);
            fixture.SwipeDetector.FeedEnd();

            Assert.That(fixture.Rules.TotalTouches, Is.Zero,
                "A press with no travel must not consume a touch.");
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.False);
        }

        [Test]
        public void GameplayInputRouter_SwipeProgressPreviewsTheGestureWhileTheBallIsAttached()
        {
            var fixture = CreateFixture();
            PresentationFixture presentation = ConfigurePresentation(fixture, sampleCount: 24, sampleStep: .05f);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.AttachTo(CreateAnchor(new Vector2(0f, 1f)));

            fixture.SwipeDetector.FeedSample(new Vector2(120f, 400f), 0d);
            fixture.SwipeDetector.FeedSample(new Vector2(120f, 200f), .1d);

            Assert.That(fixture.Controller.IsGesturePreparation, Is.True,
                "A drag in progress must preview the pending swipe before it is committed.");
            Assert.That(presentation.Line.enabled, Is.True);

            fixture.SwipeDetector.FeedEnd();

            Assert.That(fixture.Controller.IsGesturePreparation, Is.False);
            Assert.That(presentation.Line.enabled, Is.False);
        }

        [Test]
        public void SubmitSwipe_RejectsAnOutOfReachPossessionWithOneOpponentPoint()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: false, timingAccuracy: 1f);

            Assert.That(fixture.Rules.TotalTouches, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.EqualTo(1));
            Assert.That(fixture.Ball.Snapshot.IsAttached, Is.True);
        }

        [TestCase(SwipeDirection.Left)]
        [TestCase(SwipeDirection.Right)]
        [TestCase(SwipeDirection.Up)]
        [TestCase(SwipeDirection.Down)]
        public void GameplayInputRouter_OnSwipeRoutesEveryCardinalDirectionToController(SwipeDirection direction)
        {
            var fixture = CreateFixture(targetScore: 3);
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
            SubmitTouch(fixture, BallContext.Low, Vector2.down);
            SubmitTouch(fixture, BallContext.Rising, Vector2.up);
            SetBallForContext(fixture, BallContext.ApexNearNet);
            RouteSwipe(fixture.SwipeDetector, SwipeDirection.Right);

            Assert.That(fixture.Rules.TotalTouches, Is.EqualTo(3));
            Assert.That(fixture.Controller.TouchCount, Is.EqualTo(3));
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
        public void ThirdValidTouch_ShowsCounterplayCuesWithoutChangingPrediction()
        {
            var fixture = CreateFixture(targetScore: 5);
            AdvanceControllerToPlay(fixture.Controller);

            for (int rally = 0; rally < 3; rally++)
            {
                SubmitAuthoredRally(fixture);
            }
            Vector2 predictionBeforeCueFrame = fixture.Ball.PredictLandingPoint();
            fixture.Controller.SimulateForTest(0f);

            Assert.That(fixture.Controller.CompletedRallyCount, Is.EqualTo(3));
            Assert.That(fixture.Controller.OpponentCounterCueVisible, Is.True);
            Assert.That(fixture.Controller.OpponentFakeCueVisible, Is.True);
            Assert.That(fixture.Controller.PredictedLandingPoint.x, Is.EqualTo(predictionBeforeCueFrame.x).Within(.001f));
            Assert.That(fixture.Controller.PredictedLandingPoint.y, Is.EqualTo(predictionBeforeCueFrame.y).Within(.001f));

            fixture.Controller.SimulateForTest(VolleyReturnPattern.AuthoredDefault().CueLeadSeconds);
            Assert.That(fixture.Controller.OpponentCounterCueVisible, Is.False);
            Assert.That(fixture.Controller.OpponentFakeCueVisible, Is.False);
        }

        [Test]
        public void DigSetSpike_ChangesScoreOnceAndResetsThePossessionForTheNextReturn()
        {
            var fixture = CreateFixture(targetScore: 1);
            AdvanceControllerToPlay(fixture.Controller);

            Assert.That(fixture.Controller.BuildHudState().statusText, Is.EqualTo("TOUCH 1/3"));
            SubmitAuthoredRally(fixture);

            Assert.That(fixture.Controller.PlayerScore, Is.EqualTo(1));
            Assert.That(fixture.Controller.OpponentScore, Is.Zero);
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsAttached, Is.True);
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.False);
        }

        [Test]
        public void FailedPossession_AwardsOneOpponentPointAndReattachesTheBall()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: false, timingAccuracy: 1f);

            Assert.That(fixture.Controller.PlayerScore, Is.Zero);
            Assert.That(fixture.Controller.OpponentScore, Is.EqualTo(1));
            Assert.That(fixture.Controller.TouchCount, Is.Zero);
            Assert.That(fixture.Ball.Snapshot.IsAttached, Is.True);
        }

        [Test]
        public void TargetScoreCompletion_AndDeadlineFailure_FinishOnlyOnceThroughTheController()
        {
            var winningFixture = CreateFixture(targetScore: 1);
            AdvanceControllerToPlay(winningFixture.Controller);
            int completions = 0;
            winningFixture.Controller.Completed += _ => completions++;
            SubmitAuthoredRally(winningFixture);
            winningFixture.Controller.SimulateForTest(1f);

            Assert.That(completions, Is.EqualTo(1));

            var timeoutFixture = CreateFixture(targetScore: 2);
            AdvanceControllerToPlay(timeoutFixture.Controller);
            timeoutFixture.Controller.SimulateForTest(59.99f);
            SetBallForContext(timeoutFixture, BallContext.Low);
            timeoutFixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);
            timeoutFixture.Controller.SimulateForTest(.02f);

            Assert.That(timeoutFixture.Controller.BuildResult().Pass, Is.False);
        }

        [Test]
        public void FourthTouchInOnePossession_IsRejected()
        {
            var fixture = CreateFixture();
            AdvanceControllerToPlay(fixture.Controller);
            SubmitAuthoredRally(fixture, resolve: false);
            int launches = fixture.Controller.SuccessfulLaunchCount;
            SetBallForContext(fixture, BallContext.Low);

            fixture.Controller.SubmitSwipe(Vector2.down, inReachZone: true, timingAccuracy: 1f);

            Assert.That(fixture.Controller.SuccessfulLaunchCount, Is.EqualTo(launches));
        }

        [Test]
        public void DeadlineCrossing_TicksRulesBeforeCachingQualifyingResult()
        {
            var fixture = CreateFixture(targetScore: 3);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Controller.SimulateForTest(59.99f - fixture.Rules.Elapsed);
            SubmitAuthoredRally(fixture);
            SubmitAuthoredRally(fixture);

            Assert.That(fixture.Rules.Elapsed, Is.EqualTo(59.99f).Within(.0001f));
            Assert.That(fixture.Rules.BuildResult().Pass, Is.False);

            fixture.Controller.SimulateForTest(.02f);

            Assert.That(fixture.Rules.Elapsed, Is.EqualTo(60.01f).Within(.0001f));
            Assert.That(fixture.Rules.BuildResult().Pass, Is.False);
            Assert.That(fixture.Controller.BuildResult().Pass, Is.False);
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

        [Test]
        public void Presentation_ConfiguredOnceStaysBoundToTheControllerBallAcrossPlayFrames()
        {
            var fixture = CreateFixture();
            PresentationFixture presentation = ConfigurePresentation(fixture, sampleCount: 24, sampleStep: .05f);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.AttachTo(CreateAnchor(new Vector2(-1.5f, 1.25f)));

            for (var frame = 0; frame < 30; frame++)
                fixture.Controller.SimulateForTest(Time.fixedDeltaTime);
            fixture.Controller.BeginGesturePreparation(Vector2.down);

            Assert.That(presentation.Preview.Source, Is.SameAs(fixture.Ball));
            Assert.That(presentation.Preview.Line, Is.SameAs(presentation.Line));
            Assert.That(presentation.Shadow.Target, Is.SameAs(fixture.Ball.transform));
            Assert.That(presentation.Shadow.Shadow, Is.SameAs(presentation.ShadowTransform));
            Assert.That(presentation.Shadow.Renderer, Is.SameAs(presentation.ShadowRenderer));
            Assert.That(presentation.Line.positionCount, Is.EqualTo(24),
                "The authored sample count proves the preview was configured once and never re-configured.");
        }

        [Test]
        public void GesturePreparation_ShowsThePreviewOnlyWhileAttachedAndHidesItOnSubmit()
        {
            var fixture = CreateFixture();
            PresentationFixture presentation = ConfigurePresentation(fixture, sampleCount: 24, sampleStep: .05f);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.AttachTo(CreateAnchor(new Vector2(0f, 1f)));

            fixture.Controller.BeginGesturePreparation(Vector2.down);

            Assert.That(fixture.Controller.IsGesturePreparation, Is.True);
            Assert.That(presentation.Line.enabled, Is.True);
            Assert.That(presentation.Line.positionCount, Is.EqualTo(24));

            SubmitTouch(fixture, BallContext.Low, Vector2.down);

            Assert.That(fixture.Controller.IsGesturePreparation, Is.False);
            Assert.That(presentation.Line.enabled, Is.False);
            Assert.That(presentation.Line.positionCount, Is.Zero);
        }

        [Test]
        public void GesturePreparation_IsRejectedWhileTheBallIsAlreadyInFlight()
        {
            var fixture = CreateFixture();
            PresentationFixture presentation = ConfigurePresentation(fixture, sampleCount: 24, sampleStep: .05f);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.Launch(new Vector2(1f, 1f), 5f, 0f);

            fixture.Controller.BeginGesturePreparation(Vector2.down);

            Assert.That(fixture.Controller.IsGesturePreparation, Is.False);
            Assert.That(presentation.Line.enabled, Is.False);
            Assert.That(presentation.Line.positionCount, Is.Zero);
        }

        [Test]
        public void PreviewAndShadowRefresh_NeverMutateBallBodyPositionOrVelocity()
        {
            var fixture = CreateFixture();
            PresentationFixture presentation = ConfigurePresentation(fixture, sampleCount: 24, sampleStep: .05f);
            AdvanceControllerToPlay(fixture.Controller);
            fixture.Ball.AttachTo(CreateAnchor(new Vector2(-1.5f, 1.25f)));
            Vector2 position = fixture.Ball.Body.position;
            Vector2 velocity = fixture.Ball.Body.velocity;

            fixture.Controller.BeginGesturePreparation(Vector2.down);
            presentation.Preview.Refresh(Vector2.down, 6f, 0f);
            presentation.Shadow.Refresh();
            fixture.Controller.SimulateForTest(Time.fixedDeltaTime);

            Assert.That(fixture.Ball.Body.position, Is.EqualTo(position));
            Assert.That(fixture.Ball.Body.velocity, Is.EqualTo(velocity));
            Assert.That(presentation.ShadowTransform.position.x,
                Is.EqualTo(fixture.Ball.transform.position.x).Within(.001f));
            Assert.That(presentation.ShadowTransform.position.y, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void ProductionInputPath_InstallsOneSwipeDetectorOnTheSharedRouterAndReachesTheController()
        {
            var controllerObject = new GameObject("VolleyballProductionInputTest");
            temporaryObjects.Add(controllerObject);
            controllerObject.AddComponent<BoxCollider2D>().size = new Vector2(9f, 6f);
            var router = controllerObject.AddComponent<GameplayInputRouter>();
            var controller = controllerObject.AddComponent<VolleyballController>();

            var ballObject = new GameObject("VolleyballProductionInputBall");
            temporaryObjects.Add(ballObject);
            ballObject.AddComponent<Rigidbody2D>();
            var ball = ballObject.AddComponent<BallRig>();
            var profile = FlightProfile.Create(1f, 0f, -10f, 1f);
            temporaryObjects.Add(profile);
            ball.SetProfile(profile);
            controller.ConfigureForTest(null, ball);
            AdvanceControllerToPlay(controller);
            SetBallState(ball, BallContext.Low);

            router.FeedPointerDownForTest(new Vector2(200f, 400f), 0d);
            router.FeedPointerMoveForTest(new Vector2(200f, 100f), .1d);
            router.FeedPointerUpForTest(new Vector2(200f, 100f), .2d);

            Assert.That(controller.HasProductionSwipeDetector, Is.True,
                "The production scene has no bridge, so the controller owns the router's swipe detector.");
            Assert.That(controller.SuccessfulLaunchCount, Is.EqualTo(1));
            Assert.That(controller.TouchCount, Is.EqualTo(1));
        }

        ControllerFixture CreateFixture(bool preResolveRules = false, int targetScore = 2)
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
            var rules = new VolleyballRules(targetScore: targetScore, timeLimit: 60f);
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

        static void SetBallForContext(ControllerFixture fixture, BallContext context) =>
            SetBallState(fixture.Ball, context);

        // Places an already-launched ball without detaching it, so touches two and three are
        // submitted against a genuinely in-flight ball.
        static void SetBallInFlightState(ControllerFixture fixture, BallContext context)
        {
            Assert.That(fixture.Ball.Snapshot.IsInFlight, Is.True,
                "SetBallInFlightState may only reposition a ball that is already in flight.");
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

        // Lets real physics carry the ball to the profile ground plane the controller watches.
        static IEnumerator WaitForGroundedResolution(ControllerFixture fixture)
        {
            int pointsBefore = fixture.Controller.PlayerScore + fixture.Controller.OpponentScore;
            float deadline = Time.unscaledTime + 20f;
            while (fixture.Controller.PlayerScore + fixture.Controller.OpponentScore == pointsBefore &&
                   Time.unscaledTime < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(fixture.Controller.PlayerScore + fixture.Controller.OpponentScore,
                Is.GreaterThan(pointsBefore),
                "An unreturned flight must resolve itself on the ground plane.");
        }

        static void SetBallState(BallRig ball, BallContext context)
        {
            ball.AttachTo(null);
            ball.Body.position = context == BallContext.ApexNearNet
                ? new Vector2(0f, 2f)
                : new Vector2(0f, 1f);
            ball.Body.velocity = context switch
            {
                BallContext.Low => new Vector2(0f, -2f),
                BallContext.Rising => new Vector2(0f, 2f),
                BallContext.ApexNearNet => Vector2.zero,
                _ => throw new System.ArgumentOutOfRangeException(nameof(context), context, null)
            };
        }

        Transform CreateAnchor(Vector2 position)
        {
            var anchorObject = new GameObject("VolleyballAnchorTest");
            temporaryObjects.Add(anchorObject);
            anchorObject.transform.position = position;
            return anchorObject.transform;
        }

        // Both presentation components log a configuration error in Awake, so they are authored
        // inactive and only enabled once Configure has supplied their references.
        PresentationFixture ConfigurePresentation(ControllerFixture fixture, int sampleCount, float sampleStep)
        {
            var previewObject = new GameObject("VolleyballTrajectoryPreviewTest");
            temporaryObjects.Add(previewObject);
            previewObject.SetActive(false);
            var line = previewObject.AddComponent<LineRenderer>();
            var preview = previewObject.AddComponent<TrajectoryPreview>();
            preview.Configure(fixture.Ball, line, sampleCount, sampleStep);
            previewObject.SetActive(true);

            var shadowRootObject = new GameObject("VolleyballBallShadowTest");
            temporaryObjects.Add(shadowRootObject);
            shadowRootObject.SetActive(false);
            var shadowObject = new GameObject("VolleyballBallShadowSpriteTest");
            temporaryObjects.Add(shadowObject);
            var shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            var shadow = shadowRootObject.AddComponent<BallShadow>();
            shadow.Configure(fixture.Ball.transform, shadowObject.transform, shadowRenderer,
                ground: 0f, maximumHeight: 4f, minimumScale: .35f, maximumScale: 1f,
                minimumAlpha: .2f, maximumAlpha: .75f);
            shadowRootObject.SetActive(true);

            fixture.Controller.ConfigurePresentationForTest(preview, shadow);
            return new PresentationFixture(preview, line, shadow, shadowObject.transform, shadowRenderer);
        }

        static void SubmitAuthoredRally(ControllerFixture fixture, bool resolve = true)
        {
            SubmitTouch(fixture, BallContext.Low, Vector2.down);
            SubmitTouch(fixture, BallContext.Rising, Vector2.up);
            SubmitTouch(fixture, BallContext.ApexNearNet, new Vector2(1f, -1f));
            if (resolve)
                fixture.Controller.ResolveCourtContact(ownCourt: false);
        }

        static void SubmitTouch(ControllerFixture fixture, BallContext context, Vector2 swipe)
        {
            SetBallForContext(fixture, context);
            fixture.Controller.SubmitSwipe(swipe, inReachZone: true, timingAccuracy: 1f);
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

        readonly struct PresentationFixture
        {
            public PresentationFixture(
                TrajectoryPreview preview,
                LineRenderer line,
                BallShadow shadow,
                Transform shadowTransform,
                SpriteRenderer shadowRenderer)
            {
                Preview = preview;
                Line = line;
                Shadow = shadow;
                ShadowTransform = shadowTransform;
                ShadowRenderer = shadowRenderer;
            }

            public TrajectoryPreview Preview { get; }
            public LineRenderer Line { get; }
            public BallShadow Shadow { get; }
            public Transform ShadowTransform { get; }
            public SpriteRenderer ShadowRenderer { get; }
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
