using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballChallengeTests
    {
        [Test]
        public void LearnCountsThreeReceivesEvenAcrossRallies()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_learn"));
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(0));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(0f));
            Assert.That(rules.CompletedTargets, Is.EqualTo(0));
        }

        [Test]
        public void PracticeRequiresReceiveReceiveSmashSequenceForPoint()
        {
            var definition = ChallengeCatalog.LoadDefault().Get("volleyball_practice");
            var rules = new VolleyballChallengeRules(definition);
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(0));
            Assert.That(rules.Match.ClockLimit, Is.EqualTo(0f));
            Assert.That(rules.IsComplete, Is.False);
        }

        [Test]
        public void ExamCompletesAtDeadlineAfterProcessingTheFinalTick()
        {
            var definition = ChallengeCatalog.LoadDefault().Get("volleyball_exam");
            var rules = new VolleyballChallengeRules(definition);
            Assert.That(rules.Match.WinningPoints, Is.EqualTo(5));
            rules.Match.SetScoreForTest(4, 3);
            rules.Tick(119.999f);
            Assert.That(rules.Match.IsOver, Is.False);
            rules.Tick(.002f);
            Assert.That(rules.Match.Elapsed, Is.LessThanOrEqualTo(120f));
            Assert.That(rules.Match.IsOver, Is.True);
            Assert.That(rules.BuildResult(new ChallengeAttemptContext("attempt", "volleyball_exam",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal)).Pass, Is.False);
        }

        [Test]
        public void ExamClampsLargeFrameDeltaToDeadline()
        {
            var rules = new VolleyballChallengeRules(ChallengeCatalog.LoadDefault().Get("volleyball_exam"));
            rules.Tick(120.001f);
            Assert.That(rules.Match.Elapsed, Is.EqualTo(120f).Within(.001f));
            Assert.That(rules.Match.IsOver, Is.True);
        }
    }
}
