using KMA.Gameplay;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceRulesTests
    {
        [Test]
        public void ObstacleBeat_DisablesRhythmMissAndAcceptsOnlySwipe()
        {
            var rules = EnduranceRules.AtObstacleBeat();

            rules.AdvanceToPlayForTest();
            rules.Tap(0d, 0d);
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
            Assert.That(rules.MissCount, Is.Zero);

            rules.Swipe(SwipeDirection.Up);

            Assert.That(rules.MissCount, Is.Zero);
            Assert.That(rules.ObstacleCleared, Is.True);
        }

        [Test]
        public void ModeSwitch_ActivatesOnlyTheAuthoredBeatMode()
        {
            var rules = new EnduranceRules();

            rules.AdvanceToPlayForTest();
            rules.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));
            rules.EndHold(1f);
            Assert.That(rules.Stamina, Is.EqualTo(100f));

            rules.Dispatch(new AuthoredBeat(BeatEvent.Breath));
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.BreathHold));
            rules.Tap(0d, 0d);
            Assert.That(rules.JudgedCount, Is.Zero);

            rules.Dispatch(new AuthoredBeat(BeatEvent.Slide));
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
            rules.EndHold(1f);
            Assert.That(rules.JudgedCount, Is.Zero);
        }

        [Test]
        public void ComboCannotPassWithoutCompletingAuthoredPrimaryObjective()
        {
            var rules = RulesWithAuthoredLaps(2, 3);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void EqualAuthoredInputs_ProduceEqualOutcomes()
        {
            var a = EnduranceRules.Default();
            var b = EnduranceRules.Default();

            a.AdvanceToPlayForTest();
            b.AdvanceToPlayForTest();
            foreach (var beat in LapPattern.Default.Events)
            {
                a.Dispatch(beat);
                b.Dispatch(beat);
                if (beat.Beat == BeatEvent.Tap)
                {
                    a.Tap(0d, 0d);
                    b.Tap(0d, 0d);
                }
                else if (beat.Beat == BeatEvent.Breath)
                {
                    a.EndHold(1f);
                    b.EndHold(1f);
                }
                else
                {
                    a.Swipe(beat.Beat == BeatEvent.Jump ? SwipeDirection.Up : SwipeDirection.Down);
                    b.Swipe(beat.Beat == BeatEvent.Jump ? SwipeDirection.Up : SwipeDirection.Down);
                }

            }

            Assert.That(a.BuildResult().Pass, Is.EqualTo(b.BuildResult().Pass));
            Assert.That(a.BuildResult().Score, Is.EqualTo(b.BuildResult().Score));
            Assert.That(a.BuildResult().Rank, Is.EqualTo(b.BuildResult().Rank));
        }
        [Test]
        public void NonTerminalAuthoredBeat_DoesNotAdvanceLapOrPass()
        {
            var rules = RulesWithAuthoredLaps(2, 3);
            rules.Dispatch(new AuthoredBeat(BeatEvent.Tap));
            Assert.That(rules.Laps, Is.EqualTo(2));
            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void ProductionFactoryCannotSetLapProgress()
        {
            Assert.That(typeof(EnduranceRules).GetMethod("ForTest"), Is.Null);
            Assert.That(typeof(EnduranceRules).GetMethods().Any(method => method.Name == "CompleteLap" && method.IsPublic), Is.False);
        }

        [Test]
        public void ControllerTicksTutorialCountdownAndPlayOncePerFrame()
        {
            var gameObject = new GameObject("endurance-lifecycle-test");
            var controller = gameObject.AddComponent<EnduranceController>();
            controller.ConfigureLifecycleForTest(2f, 3f, 1);

            controller.Simulate(1f);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Tutorial));
            controller.Simulate(1f);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Countdown));
            controller.Simulate(2f);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Countdown));
            controller.Simulate(1f);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Play));
            controller.Simulate(1f);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(controller.Rules.Elapsed, Is.EqualTo(2f));
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void RequiredAuthoredLapEndBeats_PassWithNonZeroDeterministicScore()
        {
            var first = RunAuthoredPattern();
            var second = RunAuthoredPattern();
            Assert.That(first.Laps, Is.EqualTo(3));
            Assert.That(first.Result.Pass, Is.True);
            Assert.That(first.Result.Score, Is.GreaterThan(0));
            Assert.That(second.Laps, Is.EqualTo(first.Laps));
            Assert.That(second.Result.Score, Is.EqualTo(first.Result.Score));
            Assert.That(second.Result.Rank, Is.EqualTo(first.Result.Rank));
        }

        [Test]
        public void EnduranceController_GatesInputsAndEmitsResultExactlyOnce()
        {
            var gameObject = new GameObject("endurance-test");
            var controller = gameObject.AddComponent<EnduranceController>();
            controller.ConfigureForTest(1);
            var emitted = 0;
            controller.Completed += _ => emitted++;
            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap, endsLap: true));
            Assert.That(controller.Rules.JudgedCount, Is.Zero);
            controller.AdvanceToPlayForTest();
            controller.Dispatch(new AuthoredBeat(BeatEvent.Tap, endsLap: true));
            controller.Tap(0d, 0d);
            controller.Resolve();
            controller.Resolve();
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(emitted, Is.EqualTo(1));
            Assert.That(controller.LastResult, Is.Not.Null);
            Object.DestroyImmediate(gameObject);
        }

        static EnduranceRules RulesWithAuthoredLaps(int laps, int requiredLaps)
        {
            var rules = new EnduranceRules(requiredLaps);
            rules.AdvanceToPlayForTest();
            for (var index = 0; index < laps; index++)
                rules.Dispatch(new AuthoredBeat(BeatEvent.Slide, endsLap: true));
            return rules;
        }

        static (int Laps, MinigameResult Result) RunAuthoredPattern()
        {
            var rules = EnduranceRules.Default();
            rules.AdvanceToPlayForTest();
            for (var lap = 0; lap < rules.RequiredLaps; lap++)
            {
                foreach (var beat in LapPattern.Default.Events)
                {
                    rules.Dispatch(beat);
                    if (beat.Beat == BeatEvent.Tap) rules.Tap(0d, 0d);
                    else if (beat.Beat == BeatEvent.Breath) rules.EndHold(1f);
                    else rules.Swipe(beat.Beat == BeatEvent.Jump ? SwipeDirection.Up : SwipeDirection.Down);
                }
            }
            return (rules.Laps, rules.BuildResult());
        }
    }
}
