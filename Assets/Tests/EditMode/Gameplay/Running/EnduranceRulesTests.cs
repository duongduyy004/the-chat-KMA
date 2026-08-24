using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceRulesTests
    {
        [Test]
        public void ObstacleBeat_DisablesRhythmMissAndAcceptsOnlySwipe()
        {
            var rules = EnduranceRules.AtObstacleBeat();

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

            rules.EnterBeat(BeatEvent.Tap);
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.RhythmTap));
            rules.EndHold(1f);
            Assert.That(rules.Stamina, Is.EqualTo(100f));

            rules.EnterBeat(BeatEvent.Breath);
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.BreathHold));
            rules.Tap(0d, 0d);
            Assert.That(rules.JudgedCount, Is.Zero);

            rules.EnterBeat(BeatEvent.Slide);
            Assert.That(rules.Mode, Is.EqualTo(EnduranceInputMode.ObstacleSwipe));
            rules.EndHold(1f);
            Assert.That(rules.JudgedCount, Is.Zero);
        }

        [Test]
        public void ComboCannotPassWithoutCompletingAuthoredPrimaryObjective()
        {
            var rules = EnduranceRules.ForTest(laps: 2, requiredLaps: 3, combo: 999, stamina: 100);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void EqualAuthoredInputs_ProduceEqualOutcomes()
        {
            var a = EnduranceRules.Default();
            var b = EnduranceRules.Default();

            foreach (var beat in LapPattern.Default.Events)
            {
                a.EnterBeat(beat.Beat);
                b.EnterBeat(beat.Beat);
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

                if (beat.EndsLap)
                {
                    a.CompleteLap();
                    b.CompleteLap();
                }
            }

            Assert.That(a.BuildResult().Pass, Is.EqualTo(b.BuildResult().Pass));
            Assert.That(a.BuildResult().Score, Is.EqualTo(b.BuildResult().Score));
            Assert.That(a.BuildResult().Rank, Is.EqualTo(b.BuildResult().Rank));
        }
    }
}
