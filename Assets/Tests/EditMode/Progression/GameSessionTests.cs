using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameSessionTests
    {
        [Test]
        public void StartSubject_RejectsWhileSubjectAttemptIsActive()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            Assert.Throws<InvalidOperationException>(() => session.StartSubject(SubjectId.Endurance));
        }

        [Test]
        public void StartSubject_CannotBypassPunishmentBeforeSecondFailure()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Punishment));

            Assert.Throws<InvalidOperationException>(() => session.StartSubject(SubjectId.Endurance));

            Assert.That(session.CompletePunishment(), Is.EqualTo(SessionRoute.RetrySubject));
            Assert.That(session.SubmitResult(SubjectId.Sprint, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
        }

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
        public void AcceptedResult_IsRetainedAsSnapshot_AndFailedResultCannotReplaceIt()
        {
            var session = new GameSession();
            var accepted = new MinigameResult(true, 8f, Rank.A);

            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, accepted), Is.EqualTo(SessionRoute.Map));

            accepted.Pass = false;
            accepted.Score = 1f;
            accepted.Rank = Rank.F;

            session.StartSubject(SubjectId.Sprint);
            Assert.That(session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 10f, Rank.S)),
                Is.EqualTo(SessionRoute.Punishment));

            var bestResult = session.GetRecord(SubjectId.Sprint).BestResult;
            Assert.That(bestResult.Pass, Is.True);
            Assert.That(bestResult.Score, Is.EqualTo(8f));
            Assert.That(bestResult.Rank, Is.EqualTo(Rank.A));
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
