using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameSessionTests
    {
        [Test]
        public void FirstFail_RoutesPunishment_ThenSecondFailLosesLife()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Punishment));
            Assert.That(session.CompletePunishment(), Is.EqualTo(SessionRoute.RetrySubject));
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
        }

        [Test]
        public void LastLifeLost_ReturnsGameOver()
        {
            var session = new GameSession();

            for (var attempt = 0; attempt < 5; attempt++)
            {
                session.StartSubject(SubjectId.Sprint);
                session.SubmitResult(SubjectId.Sprint, Failed());
                session.CompletePunishment();
                Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()),
                    Is.EqualTo(attempt == 4 ? SessionRoute.GameOver : SessionRoute.Map));
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.StartSubject(SubjectId.Sprint), Is.EqualTo(SessionRoute.GameOver));
        }

        [Test]
        public void PassedResult_RecordsPassAndOnlyImprovesBestScore()
        {
            var session = new GameSession();

            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Passed(8f)), Is.EqualTo(SessionRoute.Map));
            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Passed(6f)), Is.EqualTo(SessionRoute.Map));

            var record = session.GetRecord(SubjectId.Sprint);
            Assert.That(record.Passed, Is.True);
            Assert.That(record.BestScore, Is.EqualTo(8f));
            Assert.That(record.BestRank, Is.EqualTo(Rank.A));
            Assert.That(record.FailedVisits, Is.Zero);
        }

        [Test]
        public void BonusScoreCannotOverrideFailedResult()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            Assert.That(session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 10, Rank.S)),
                Is.EqualTo(SessionRoute.Punishment));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
            Assert.That(session.GetRecord(SubjectId.Sprint).BestScore, Is.Zero);
        }

        [Test]
        public void BossUnlockRequiresEverySubjectToBePassed()
        {
            var session = new GameSession();
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                session.StartSubject(id);
                session.SubmitResult(id, Passed(6f));
            }

            Assert.That(session.BossUnlocked, Is.True);
        }

        [Test]
        public void OneUnpassedSubjectBlocksBossUnlock()
        {
            var session = new GameSession();
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                if (id == SubjectId.Football)
                {
                    continue;
                }

                session.StartSubject(id);
                session.SubmitResult(id, Passed(6f));
            }

            Assert.That(session.BossUnlocked, Is.False);
        }

        static MinigameResult Failed() => new MinigameResult(false, 0, Rank.F);

        static MinigameResult Passed(float score) => new MinigameResult(true, score, ScoreUtil.ToRank(score));
    }
}
