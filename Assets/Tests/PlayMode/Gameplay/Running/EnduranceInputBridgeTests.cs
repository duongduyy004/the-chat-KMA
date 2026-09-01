using System;
using System.Reflection;
using KMA.Gameplay;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceInputBridgeTests
    {
        GameObject gameObject;

        [SetUp]
        public void SetUp() => gameObject = new GameObject("EnduranceInputBridgeTests");

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(gameObject);

        [Test]
        public void DetectorEvents_OnlyMutateTheRulesMetricForTheActiveMode()
        {
            var controller = CreateController();
            var rhythm = new RhythmBeatInputDetector();
            var hold = new HoldInputDetector();
            var swipe = new SwipeInputDetector();
            ConnectDetectorBridge(controller, rhythm, hold, swipe);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            rhythm.FeedTap(10.1d, 10d);
            int judgedAfterTap = controller.Rules.JudgedCount;
            float staminaAfterTap = controller.Rules.Stamina;
            hold.FeedDown(1d);
            hold.FeedUp(2d);
            FeedVerticalSwipe(swipe, 1d, 100f);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(staminaAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            rhythm.FeedTap(10d, 10d);
            FeedVerticalSwipe(swipe, 3d, 100f);
            hold.FeedDown(4d);
            hold.FeedUp(5d);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.GreaterThan(staminaAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            rhythm.FeedTap(10d, 10d);
            hold.FeedDown(6d);
            hold.FeedUp(7d);
            FeedVerticalSwipe(swipe, 8d, 100f);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);
        }

        [Test]
        public void RoutedRhythmTap_AppliesCalibratedDetectorDeltaOnceAndDoesNotMissAtObstacleWarningOrActivation()
        {
            var controller = CreateController();
            controller.RhythmOffsetMs = 100d;
            var router = gameObject.AddComponent<GameplayInputRouter>();
            ConnectRouterBridge(controller, router);
            controller.ConfigurePatternForTest(new LapPattern(new[]
            {
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Jump)
            }));

            controller.AdvanceToBeatForTest(1);
            Assert.That(controller.ObstacleCueVisible, Is.True);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));
            router.FeedRhythmTapForTest(10d, 10d);
            Assert.That(controller.Rules.GoodCount, Is.EqualTo(1));
            Assert.That(controller.Rules.MissCount, Is.EqualTo(0));

            controller.AdvanceToBeatForTest(3);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
            router.FeedRhythmTapForTest(10d, 10d);
            Assert.That(controller.Rules.MissCount, Is.EqualTo(0));
        }

        [Test]
        public void RouterEvents_OnlyReachControllerForTheirMatchingInputMode()
        {
            var controller = CreateController();
            var router = gameObject.AddComponent<GameplayInputRouter>();
            ConnectRouterBridge(controller, router);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            router.FeedRhythmTapForTest(10d, 10d);
            FeedRouterGesture(router, 1d);
            Assert.That(controller.InputTapCount, Is.EqualTo(2));
            Assert.That(controller.InputHoldCount, Is.Zero);
            Assert.That(controller.InputSwipeCount, Is.Zero);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            router.FeedRhythmTapForTest(10d, 10d);
            FeedRouterGesture(router, 3d);
            Assert.That(controller.InputTapCount, Is.EqualTo(2));
            Assert.That(controller.InputHoldCount, Is.EqualTo(1));
            Assert.That(controller.InputSwipeCount, Is.Zero);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            router.FeedRhythmTapForTest(10d, 10d);
            FeedRouterGesture(router, 5d);
            Assert.That(controller.InputTapCount, Is.EqualTo(2));
            Assert.That(controller.InputHoldCount, Is.EqualTo(1));
            Assert.That(controller.InputSwipeCount, Is.EqualTo(1));
        }

        [Test]
        public void HoldAndVerticalSwipe_ForwardOneCompletionAndMapUpAndDown()
        {
            var controller = CreateController();
            var hold = new HoldInputDetector();
            var swipe = new SwipeInputDetector();
            ConnectDetectorBridge(controller, new RhythmBeatInputDetector(), hold, swipe);

            controller.Tap(.1d, 0d);
            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            hold.FeedDown(1d);
            hold.FeedUp(3d);
            hold.FeedUp(4d);
            Assert.That(hold.ChargeRatio, Is.EqualTo(1d));
            Assert.That(controller.InputHoldCount, Is.EqualTo(1));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(100f));

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            FeedVerticalSwipe(swipe, 5d, 100f);
            swipe.FeedEnd();
            Assert.That(controller.InputSwipeCount, Is.EqualTo(1));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Slide));
            FeedVerticalSwipe(swipe, 7d, -100f);
            Assert.That(controller.Rules.ObstacleCleared, Is.True);
        }

        EnduranceController CreateController()
        {
            var controller = gameObject.AddComponent<EnduranceController>();
            gameObject.AddComponent<EnduranceInputBridge>();
            controller.ConfigureLifecycleForTest(0f, 0f, 1);
            controller.AdvanceToPlayForTest();
            return controller;
        }

        void ConnectDetectorBridge(EnduranceController controller, RhythmBeatInputDetector rhythm,
            HoldInputDetector hold, SwipeInputDetector swipe)
        {
            var bridge = gameObject.GetComponent<EnduranceInputBridge>();
            MethodInfo configure = typeof(EnduranceInputBridge).GetMethod(
                "ConfigureDetectorsForTest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(configure, Is.Not.Null,
                "EnduranceInputBridge needs an internal detector subscription seam for rhythm, hold, and swipe events.");
            configure.Invoke(bridge, new object[] { controller, rhythm, hold, swipe });
        }

        void ConnectRouterBridge(EnduranceController controller, GameplayInputRouter router)
        {
            var bridge = gameObject.GetComponent<EnduranceInputBridge>();
            MethodInfo configure = typeof(EnduranceInputBridge).GetMethod(
                "ConfigureInputRouterForTest", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(configure, Is.Not.Null,
                "EnduranceInputBridge needs an internal router subscription seam for the shared gameplay input path.");
            configure.Invoke(bridge, new object[] { controller, router });
        }

        static void FeedRouterGesture(GameplayInputRouter router, double startedAt)
        {
            router.FeedPointerDownForTest(Vector2.zero, startedAt);
            router.FeedPointerUpForTest(new Vector2(0f, 100f), startedAt + 1d);
        }

        static void FeedVerticalSwipe(SwipeInputDetector swipe, double startedAt, float verticalDistance)
        {
            swipe.FeedSample(Vector2.zero, startedAt);
            swipe.FeedSample(new Vector2(0f, verticalDistance), startedAt + .1d);
            swipe.FeedEnd();
        }
    }
}
