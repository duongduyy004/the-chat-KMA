using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceControllerTests
    {
        [UnityTest]
        public IEnumerator CalibratedInputTime_AppliesRhythmOffsetInSeconds()
        {
            var controller = CreateController();
            controller.RhythmOffsetMs = 125.0;

            Assert.That(controller.CalibratedInputTime(10.0), Is.EqualTo(10.125).Within(0.000001));
            controller.Tap(10.0, 10.125);
            Assert.That(controller.Rules.PerfectCount, Is.EqualTo(1));

            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleIcon_AppearsTwoBeatsBeforeSwipeMode()
        {
            var schedule = new EnduranceCueSchedule(obstacleBeat: 8, warningBeats: 2);

            schedule.AdvanceToBeat(5);
            Assert.That(schedule.ObstacleCueVisible, Is.False);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(6);
            Assert.That(schedule.ObstacleCueVisible, Is.True);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(8);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ObstacleActivation_UsesAuthoredObstacleBeat()
        {
            var controller = CreateController();
            controller.ConfigurePatternForTest(new LapPattern(new[]
            {
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Tap),
                new AuthoredBeat(BeatEvent.Jump)
            }));

            controller.AdvanceToBeatForTest(0);
            Assert.That(controller.ObstacleCueVisible, Is.False);
            controller.AdvanceToBeatForTest(1);
            Assert.That(controller.ObstacleCueVisible, Is.True);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));
            controller.AdvanceToBeatForTest(3);
            Assert.That(controller.Rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WarningAndActivation_RespectExactBeatBoundaries()
        {
            var schedule = new EnduranceCueSchedule(obstacleBeat: 10, warningBeats: 2);

            schedule.AdvanceToBeat(7);
            Assert.That(schedule.ObstacleCueVisible, Is.False);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(8);
            Assert.That(schedule.ObstacleCueVisible, Is.True);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(9);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));

            schedule.AdvanceToBeat(10);
            Assert.That(schedule.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));

            yield return null;
        }

        static EnduranceController CreateController()
        {
            var controller = new GameObject("EnduranceController").AddComponent<EnduranceController>();
            controller.ConfigureForTest(1);
            controller.AdvanceToPlayForTest();
            return controller;
        }

        static void DestroyController(EnduranceController controller) => Object.Destroy(controller.gameObject);
    }
}
