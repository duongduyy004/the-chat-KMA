using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintChallengeTests
    {
        ChallengeCatalog catalog;

        [SetUp]
        public void SetUp() => catalog = ChallengeCatalog.LoadDefault();

        [Test]
        public void BalanceDefaultsMatchTheAuthoredTuningContract()
        {
            SprintBalanceParameters value = SprintBalanceParameters.Default;
            Assert.That(new[] { value.InitialStamina, value.MaxStamina, value.CorrectImpulse,
                value.WrongImpulseFactor, value.SpeedCap, value.CorrectTapCost, value.WrongTapCost,
                value.BurstRateThreshold, value.BurstExtraCost, value.ActiveDrainSpeedThreshold,
                value.ActiveDrainPerSpeed, value.RestRegenPerSecond, value.FatigueThreshold,
                value.FatigueImpulseFactor, value.FatigueSpeedCap, value.DragPerSecond,
                value.DistanceScale }, Is.EqualTo(new[] { 100f, 100f, 18f, .4f, 150f, .25f,
                1.5f, 6f, .75f, 20f, .02f, 6f, 30f, .75f, 90f, 15f, .08f }));
            Assert.That(new[] { value.ComboBoostStartStreak, value.ComboBoostFullStreak },
                Is.EqualTo(new[] { 10, 30 }));
            Assert.That(new[] { value.ComboBoostMax, value.IdleGraceSeconds, value.IdleBrakeRampSeconds,
                value.IdleBrakePerSecond }, Is.EqualTo(new[] { .25f, .35f, .25f, 240f }));
            Assert.That(SprintBalanceConfig.LoadDefault().ToRuntime(), Is.EqualTo(value));
        }

        [Test]
        public void CatalogDefinesDistinctLearnPracticeAndExamGoals()
        {
            ChallengeDefinition learn = catalog.Get("sprint_learn");
            ChallengeDefinition practice = catalog.Get("sprint_practice");
            ChallengeDefinition exam = catalog.Get("sprint_exam");
            Assert.That(learn.Kind, Is.EqualTo(ChallengeKind.Learn));
            Assert.That(learn.TargetCount, Is.EqualTo(12));
            Assert.That(practice.Distance, Is.EqualTo(150f));
            Assert.That(practice.TimeLimit, Is.EqualTo(30f));
            Assert.That(exam.Distance, Is.EqualTo(150f));
            Assert.That(exam.TimeLimit, Is.EqualTo(22f));
        }

        [Test]
        public void LearnRequiresTwelveAlternatingInputsAndResetsOnError()
        {
            var challenge = new SprintChallengeRules(catalog.Get("sprint_learn"),
                SprintBalanceParameters.Default, Array.Empty<RivalPaceProfile>());
            for (int tap = 0; tap < 8; tap++) challenge.Tap(tap % 2 == 0 ? Side.Left : Side.Right);
            challenge.Tap(Side.Right);
            Assert.That(challenge.Race.CorrectStreak, Is.Zero);
            for (int tap = 0; tap < 12; tap++) challenge.Tap(tap % 2 == 0 ? Side.Left : Side.Right);
            Assert.That(challenge.IsComplete, Is.True);
            ChallengeAttemptResult result = challenge.BuildResult(new ChallengeAttemptContext(
                "learn-1", "sprint_learn", ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal));
            Assert.That(result.Pass, Is.True);
            Assert.That(result.Metrics.CompletedTargets, Is.EqualTo(12));
            Assert.That(result.ExamResult, Is.Null);
        }

        [Test]
        public void StaminaCostsBurstDrainRestRegenerationAndFatigueCapsAreApplied()
        {
            SprintBalanceParameters tuning = SprintBalanceParameters.Default;
            var race = new SprintRules(14f, null, null, tuning);
            race.Tap(Side.Left);
            Assert.That(race.Stamina, Is.EqualTo(99.75f).Within(.001f));
            race.Tap(Side.Left);
            Assert.That(race.Stamina, Is.EqualTo(98.25f).Within(.001f));

            var rest = new SprintRules(14f, null, null, tuning);
            rest.Tap(Side.Right);
            rest.Tick(.1f);
            Assert.That(rest.Stamina, Is.EqualTo(99.1f).Within(.001f));

            var burst = new SprintRules(14f, null, null, tuning);
            burst.Tap(Side.Left);
            burst.Tick(.1f);
            float beforeSecond = burst.Stamina;
            burst.Tap(Side.Right);
            Assert.That(beforeSecond - burst.Stamina, Is.EqualTo(1f).Within(.001f),
                "a 10 Hz tap adds the .75 burst cost to the .25 correct-tap cost");

            var fatigued = new SprintRules(14f, null, null, tuning);
            for (int tap = 0; tap < 48; tap++) fatigued.Tap(Side.Right);
            Assert.That(fatigued.Stamina, Is.LessThanOrEqualTo(tuning.FatigueThreshold));
            Assert.That(fatigued.Speed, Is.LessThanOrEqualTo(tuning.FatigueSpeedCap));
            float stableStamina = fatigued.Stamina;
            fatigued.Tick(float.PositiveInfinity);
            Assert.That(fatigued.Stamina, Is.EqualTo(stableStamina));
        }

        [Test]
        public void SixHertzRaceFinishesExamAndPracticeHasLongerDeadline()
        {
            var practice = RunAtCadence("sprint_practice", 6f);
            var exam = RunAtCadence("sprint_exam", 6f);
            Assert.That(exam.Pass, Is.True);
            Assert.That(exam.Metrics.Distance, Is.GreaterThanOrEqualTo(150f));
            Assert.That(exam.Metrics.Elapsed, Is.LessThanOrEqualTo(22f));
            Assert.That(exam.ExamResult, Is.Not.Null);
            Assert.That(practice.Pass, Is.True);
            Assert.That(practice.ExamResult, Is.Null);
        }

        [Test]
        public void HighCadenceConsumesStaminaAndSlowsFinishComparedWithSixHertz()
        {
            ChallengeAttemptResult regular = RunAtCadence("sprint_exam", 6f);
            ChallengeAttemptResult excessive = RunAtCadence("sprint_exam", 10f);
            // Over the full 150 m both runners end up exhausted, so compare stamina at the same early moment.
            Assert.That(StaminaAfter(10f, 4f), Is.LessThan(StaminaAfter(6f, 4f)));
            Assert.That(excessive.Metrics.Elapsed, Is.GreaterThan(regular.Metrics.Elapsed));
        }

        [Test]
        public void FourthPlaceDoesNotPreventAnOnTimeExamPass()
        {
            var rivals = new[]
            {
                new RivalPaceProfile("A", 100f, 100f), new RivalPaceProfile("B", 100f, 100f),
                new RivalPaceProfile("C", 100f, 100f)
            };
            var challenge = new SprintChallengeRules(catalog.Get("sprint_exam"),
                SprintBalanceParameters.Default, rivals);
            Simulate(challenge, 6f, 22f);
            Assert.That(challenge.Race.Rank, Is.EqualTo(4));
            ChallengeAttemptResult result = challenge.BuildResult(new ChallengeAttemptContext(
                "exam-rank", "sprint_exam", ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal));
            Assert.That(result.Pass, Is.True);
        }

        static float StaminaAfter(float hertz, float seconds)
        {
            var catalog = ChallengeCatalog.LoadDefault();
            var challenge = new SprintChallengeRules(catalog.Get("sprint_exam"),
                SprintBalanceParameters.Default, Array.Empty<RivalPaceProfile>());
            Simulate(challenge, hertz, seconds);
            return challenge.Race.Stamina;
        }

        static ChallengeAttemptResult RunAtCadence(string challengeId, float hertz)
        {
            var catalog = ChallengeCatalog.LoadDefault();
            var challenge = new SprintChallengeRules(catalog.Get(challengeId),
                SprintBalanceParameters.Default, Array.Empty<RivalPaceProfile>());
            Simulate(challenge, hertz, catalog.Get(challengeId).TimeLimit);
            return challenge.BuildResult(new ChallengeAttemptContext("attempt", challengeId,
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal));
        }

        static void Simulate(SprintChallengeRules challenge, float hertz, float maxSeconds)
        {
            const float dt = 1f / 240f;
            int cadenceTicks = (int)Math.Round(240f / hertz);
            int tick = 0;
            Side expected = Side.Left;
            for (float elapsed = 0f; elapsed < maxSeconds && !challenge.IsComplete; elapsed += dt, tick++)
            {
                if (tick % cadenceTicks == 0)
                {
                    challenge.Tap(expected);
                    expected = expected == Side.Left ? Side.Right : Side.Left;
                }
                challenge.Tick(dt);
            }
        }
    }
}
