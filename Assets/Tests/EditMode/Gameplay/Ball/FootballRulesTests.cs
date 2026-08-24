using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballRulesTests
    {
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(5, true)]
        public void FiveKicks_PassAtThreeGoals(int goals, bool pass)
        {
            Assert.That(FootballRules.ForTest(5, goals).BuildResult().Pass, Is.EqualTo(pass));
        }

        [Test]
        public void PhaseActivatesExactlyOneDifficultyModifier()
        {
            var phase = new FootballPhase(GKReaction.Fast, TargetWidth.Normal);

            Assert.That(phase.ActiveModifierCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolveKick_RejectsInputBeforePlay_AndAfterFiveKicks()
        {
            var lifecycle = new MinigameLifecycle(2f, 3f);
            var rules = new FootballRules(lifecycle: lifecycle);

            Assert.That(rules.ResolveKick(true, 1f, ShotKind.Power), Is.False);
            lifecycle.Tick(2f);
            lifecycle.Tick(3f);

            for (var kick = 0; kick < 5; kick++)
                Assert.That(rules.ResolveKick(kick < 3, 1f, ShotKind.Placement), Is.EqualTo(kick < 3));

            Assert.That(rules.Kicks, Is.EqualTo(5));
            Assert.That(rules.Goals, Is.EqualTo(3));
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.ResolveKick(true, 1f, ShotKind.Power), Is.False);
        }

        [Test]
        public void AuthoredShot_UsesPreselectedKeeperPatternWithoutRandomness()
        {
            var first = new FootballRules();
            var second = new FootballRules();
            var shot = new FootballShot(.95f, .8f, .2f, ShotKind.Curve);

            Assert.That(first.ResolveAuthoredShot(shot), Is.EqualTo(second.ResolveAuthoredShot(shot)));
            Assert.That(first.LastKeeperPattern, Is.SameAs(first.PatternSet.Patterns[0]));
            Assert.That(first.LastShot, Is.EqualTo(shot));
        }

        [Test]
        public void AuthoredShot_RequiresTheAuthoredShotCounterplay()
        {
            var rules = new FootballRules();

            Assert.That(rules.ResolveAuthoredShot(new FootballShot(.5f, .2f, 0f, ShotKind.Power)), Is.False);
            var firstKick = new FootballRules();
            Assert.That(firstKick.ResolveAuthoredShot(new FootballShot(.95f, .8f, .2f, ShotKind.Curve)), Is.True);
        }

        [Test]
        public void BuildResult_UsesFoundationScoreAndRequiresAllFiveKicks()
        {
            var rules = new FootballRules();

            for (var kick = 0; kick < 4; kick++)
                rules.ResolveKick(kick < 3, 1f, ShotKind.Placement);

            Assert.That(rules.BuildResult().Pass, Is.False);

            rules.ResolveKick(false, .5f, ShotKind.Power);
            var result = rules.BuildResult();

            Assert.That(result.Pass, Is.True);
            Assert.That(result.Score, Is.InRange(0f, 10f));
        }
    }
}
