using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class VolleyballRulesTests
    {
        [TestCase(BallContext.Low, 0f, 1f, VolleyAction.Dig)]
        [TestCase(BallContext.Rising, 0f, 1f, VolleyAction.Set)]
        [TestCase(BallContext.ApexNearNet, 1f, -1f, VolleyAction.Spike)]
        public void ResolveGesture_UsesBallContext(BallContext context, float x, float y, VolleyAction expected)
        {
            Assert.That(VolleyballRules.ResolveGesture(context, new Vector2(x, y)), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveTouch_RequiresReachZoneAndTimingAccuracy()
        {
            var rules = new VolleyballRules();

            Assert.That(rules.ResolveTouch(BallContext.Low, new Vector2(0f, -1f), false, 1f), Is.EqualTo(VolleyAction.Invalid));
            Assert.That(rules.ResolveTouch(BallContext.Low, new Vector2(0f, -1f), true, .25f), Is.EqualTo(VolleyAction.Invalid));
            Assert.That(rules.ResolveTouch(BallContext.Low, new Vector2(0f, -1f), true, .75f), Is.EqualTo(VolleyAction.Dig));
        }

        [Test]
        public void AuthoredReturnPattern_SelectsTrajectoryBeforeLaunch()
        {
            var pattern = VolleyReturnPattern.AuthoredDefault();

            Assert.That(pattern.CueLeadSeconds, Is.GreaterThanOrEqualTo(.5f));
            Assert.That(pattern.Phases, Is.EqualTo(new[] { VolleyPhase.Dig, VolleyPhase.Set, VolleyPhase.Spike }));
            Assert.That(pattern.HasSelectedTrajectory, Is.False);

            var trajectory = pattern.SelectTrajectory(VolleyAction.Spike);

            Assert.That(pattern.HasSelectedTrajectory, Is.True);
            Assert.That(pattern.SelectedPhase, Is.EqualTo(VolleyPhase.Spike));
            Assert.That(trajectory, Is.EqualTo(VolleyTrajectory.AuthoredSpike));
            Assert.That(pattern.CanLaunch, Is.True);
        }

        [Test]
        public void ComboWithoutFivePoints_DoesNotPass()
        {
            var rules = new VolleyballRules(targetScore: 5);
            rules.SetForTest(playerScore: 4, opponentScore: 0, combo: 100);

            Assert.That(rules.BuildResult().Pass, Is.False);
        }

        [Test]
        public void ObjectiveRequiresLeadAndTimeLimit()
        {
            var tied = VolleyballRules.ForTest(playerScore: 5, opponentScore: 5, elapsed: 10f, combo: 0);
            var late = VolleyballRules.ForTest(playerScore: 5, opponentScore: 0, elapsed: 60.01f, combo: 0);

            Assert.That(tied.BuildResult().Pass, Is.False);
            Assert.That(late.BuildResult().Pass, Is.False);
        }

        [Test]
        public void EqualAuthoredTouches_ProduceEqualScores()
        {
            var first = new VolleyballRules();
            var second = new VolleyballRules();
            foreach (float accuracy in new[] { 1f, .75f, .5f, 1f })
            {
                first.RecordTouch(accuracy);
                second.RecordTouch(accuracy);
            }

            first.AwardRallyPoint();
            second.AwardRallyPoint();

            Assert.That(first.BuildResult().Score, Is.EqualTo(second.BuildResult().Score));
        }
    }
}
