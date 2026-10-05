using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballChallengeTests
    {
        static readonly FootballTuning Tuning = new FootballTuning(2f, 10f, .1f);

        [Test]
        public void LearnAndPracticeDoNotStopAtFiveKicks()
        {
            var options = new FootballMatchOptions(null, 3, false);
            var rules = new FootballRules(Tuning, options);
            Assert.That(rules.MaximumKicks, Is.Null);
            Assert.That(rules.KeeperEnabled, Is.False);
            rules.Start();
            for (int i = 0; i < 6; i++)
            {
                rules.SetAim(1f);
                rules.BeginCharge();
                rules.ReleaseShot();
                rules.Tick(20f);
            }
            Assert.That(rules.Kicks, Is.GreaterThan(5));
            Assert.That(rules.State, Is.Not.EqualTo(FootballState.MatchResult));
        }

        [Test]
        public void ExamKeepsFiveKicksEvenAfterThreeGoalsAndBuildsTypedResult()
        {
            var rules = new FootballRules(Tuning, new FootballMatchOptions(5, 3, true));
            rules.Start();
            for (int i = 0; i < 5; i++)
            {
                rules.SetAim(.55f);
                rules.BeginCharge();
                if (i < 3) rules.Tick(1f);
                rules.ReleaseShot();
                rules.Tick(20f);
                if (i < 4) Assert.That(rules.State, Is.Not.EqualTo(FootballState.MatchResult));
            }
            var context = new ChallengeAttemptContext("attempt", "soccer_exam",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal);
            Assert.That(rules.BuildChallengeResult(context).ExamResult, Is.Not.Null);
            Assert.That(rules.Goals, Is.GreaterThanOrEqualTo(3));
            Assert.That(rules.BuildChallengeResult(context).Pass, Is.True);
        }

        [Test]
        public void PracticeStopsAsSoonAsTwoGoalsAreScored()
        {
            var rules = new FootballRules(Tuning, new FootballMatchOptions(6, 2, true, stopAtRequiredGoals: true));
            rules.Start();
            for (int i = 0; i < 2; i++)
            {
                rules.SetAim(.55f);
                rules.BeginCharge();
                rules.Tick(1f);
                rules.ReleaseShot();
                rules.Tick(20f);
            }
            Assert.That(rules.State, Is.EqualTo(FootballState.MatchResult));
            Assert.That(rules.Kicks, Is.EqualTo(2));
            var context = new ChallengeAttemptContext("attempt", "soccer_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal);
            Assert.That(rules.BuildChallengeResult(context).Pass, Is.True);
            Assert.That(rules.BuildChallengeResult(context).ExamResult, Is.Null);
        }

        [Test]
        public void PracticeFailsAfterSixKicksWithoutTwoGoals()
        {
            var rules = new FootballRules(Tuning, new FootballMatchOptions(6, 2, true, stopAtRequiredGoals: true));
            rules.Start();
            for (int i = 0; i < 6; i++)
            {
                Assert.That(rules.State, Is.Not.EqualTo(FootballState.MatchResult));
                rules.SetAim(.55f);
                rules.BeginCharge();
                rules.ReleaseShot();
                rules.Tick(20f);
            }
            Assert.That(rules.State, Is.EqualTo(FootballState.MatchResult));
            Assert.That(rules.Kicks, Is.EqualTo(6));
            Assert.That(rules.Goals, Is.LessThan(2));
            Assert.That(rules.BuildChallengeResult(new ChallengeAttemptContext("attempt", "soccer_practice",
                ChallengeAttemptMode.Journey, ChallengeDifficulty.Normal)).Pass, Is.False);
        }

        [Test]
        public void CatalogGivesSoccerPracticeSixKicks()
        {
            Assert.That(ChallengeCatalog.LoadDefault().Get("soccer_practice").AttemptLimit, Is.EqualTo(6));
        }
    }
}
