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
            rhythm.FeedTap(AudioSettings.dspTime, AudioSettings.dspTime);
            int judgedAfterTap = controller.Rules.JudgedCount;
            float staminaAfterTap = controller.Rules.Stamina;
            hold.FeedDown(1d);
            hold.FeedUp(2d);
            FeedVerticalSwipe(swipe, 1d, 100f);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.EqualTo(staminaAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            rhythm.FeedTap(AudioSettings.dspTime, AudioSettings.dspTime);
            FeedVerticalSwipe(swipe, 3d, 100f);
            hold.FeedDown(4d);
            hold.FeedUp(5d);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.Stamina, Is.GreaterThan(staminaAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.False);

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            rhythm.FeedTap(AudioSettings.dspTime, AudioSettings.dspTime);
            hold.FeedDown(6d);
            hold.FeedUp(7d);
            FeedVerticalSwipe(swipe, 8d, 100f);
            Assert.That(controller.Rules.JudgedCount, Is.EqualTo(judgedAfterTap));
            Assert.That(controller.Rules.ObstacleCleared, Is.True);
        }

        [Test]
        public void RoutedRhythmTap_AppliesControllerOffsetOnceAndDoesNotMissObstacleBeat()
        {
            var controller = CreateController();
            controller.RhythmOffsetMs = 100d;
            var rhythm = new RhythmBeatInputDetector();
            ConnectDetectorBridge(controller, rhythm, new HoldInputDetector(), new SwipeInputDetector());

            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            rhythm.FeedTap(AudioSettings.dspTime, AudioSettings.dspTime);
            Assert.That(controller.Rules.GoodCount, Is.EqualTo(1));
            Assert.That(controller.Rules.MissCount, Is.EqualTo(0));

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            rhythm.FeedTap(AudioSettings.dspTime, AudioSettings.dspTime);
            Assert.That(controller.Rules.MissCount, Is.EqualTo(0));
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
            Assert.That(controller.Rules.Stamina, Is.EqualTo(100f));

            controller.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            FeedVerticalSwipe(swipe, 5d, 100f);
            swipe.FeedEnd();
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

        static void FeedVerticalSwipe(SwipeInputDetector swipe, double startedAt, float verticalDistance)
        {
            swipe.FeedSample(Vector2.zero, startedAt);
            swipe.FeedSample(new Vector2(0f, verticalDistance), startedAt + .1d);
            swipe.FeedEnd();
        }
    }
}
