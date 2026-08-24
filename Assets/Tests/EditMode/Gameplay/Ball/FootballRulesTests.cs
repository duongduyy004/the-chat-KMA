using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

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
        public void PhaseRejectsConflictingOrMissingDifficultyModifiers()
        {
            Assert.That(() => new FootballPhase(GKReaction.Fast, TargetWidth.Narrow),
                Throws.ArgumentException);
            Assert.That(() => new FootballPhase(GKReaction.Normal, TargetWidth.Normal),
                Throws.ArgumentException);
        }

        [Test]
        public void DifficultyModifierChangesKeeperResolution()
        {
            var fast = new GKPattern(new FootballPhase(GKReaction.Fast, TargetWidth.Normal),
                .5f, .25f, .7f, .1f, ShotKind.Curve);
            var slow = new GKPattern(new FootballPhase(GKReaction.Slow, TargetWidth.Normal),
                .5f, .25f, .7f, .1f, ShotKind.Curve);
            var shot = new FootballShot(.9f, .75f, .2f, ShotKind.Curve);

            Assert.That(fast.Resolve(shot), Is.False);
            Assert.That(slow.Resolve(shot), Is.True);
        }

        [Test]
        public void TargetWidthModifierChangesKeeperResolution()
        {
            var narrow = new GKPattern(new FootballPhase(GKReaction.Normal, TargetWidth.Narrow),
                .5f, .25f, .5f, .1f, ShotKind.Curve);
            var wide = new GKPattern(new FootballPhase(GKReaction.Normal, TargetWidth.Wide),
                .5f, .25f, .5f, .1f, ShotKind.Curve);
            var shot = new FootballShot(.8f, .75f, .2f, ShotKind.Curve);

            Assert.That(narrow.Resolve(shot), Is.True);
            Assert.That(wide.Resolve(shot), Is.False);
        }

        [Test]
        public void AuthoredShotResolution_RejectsMismatchedKeeperPattern()
        {
            var lifecycle = new MinigameLifecycle(2f, 3f);
            var rules = new FootballRules(lifecycle: lifecycle);

            Assert.That(rules.ResolveAuthoredShot(GoalShot(rules.PatternSet.Patterns[0]), rules.PatternSet.Patterns[1]), Is.False);
            lifecycle.Tick(2f);
            lifecycle.Tick(3f);

            for (var kick = 0; kick < 5; kick++)
                Assert.That(rules.ResolveAuthoredShot(GoalShot(rules.PatternSet.Patterns[kick])), Is.True);

            Assert.That(rules.Kicks, Is.EqualTo(5));
            Assert.That(rules.Goals, Is.EqualTo(5));
            Assert.That(rules.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(rules.ResolveAuthoredShot(GoalShot(rules.PatternSet.Patterns[0])), Is.False);
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
                rules.ResolveAuthoredShot(kick < 2 ? GoalShot(rules.PatternSet.Patterns[kick]) :
                    MissShot(rules.PatternSet.Patterns[kick]));

            Assert.That(rules.BuildResult().Pass, Is.False);

            rules.ResolveAuthoredShot(MissShot(rules.PatternSet.Patterns[4]));
            var result = rules.BuildResult();

            Assert.That(result.Pass, Is.False);
            Assert.That(result.Score, Is.Zero);
        }

        [Test]
        public void AuthoredShots_ThreeGoalsInFiveKicks_Pass()
        {
            var rules = new FootballRules();

            for (var kick = 0; kick < 5; kick++)
                Assert.That(rules.ResolveAuthoredShot(kick < 3 ? GoalShot(rules.PatternSet.Patterns[kick]) :
                    MissShot(rules.PatternSet.Patterns[kick])), Is.EqualTo(kick < 3));

            Assert.That(rules.Goals, Is.EqualTo(3));
            Assert.That(rules.BuildResult().Pass, Is.True);
            Assert.That(rules.BuildResult().Score, Is.InRange(0f, 10f));
        }

        static FootballShot GoalShot(GKPattern pattern)
        {
            var placement = pattern.KeeperPlacement > .5f ?
                pattern.KeeperPlacement - pattern.Coverage - .2f :
                pattern.KeeperPlacement + pattern.Coverage + .2f;
            return new FootballShot(Mathf.Clamp01(placement),
                1f, 1f, pattern.CounterShot);
        }

        static FootballShot MissShot(GKPattern pattern)
        {
            return new FootballShot(pattern.KeeperPlacement, 0f, 0f, pattern.CounterShot);
        }
    }
}
