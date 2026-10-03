using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameSessionTests
    {
        [Test]
        public void StartSubject_RejectsWhileChallengeAttemptIsActive()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            Assert.Throws<InvalidOperationException>(() => session.StartSubject(SubjectId.Football));
        }

        [Test]
        public void LockedSubjectCannotStartAndPracticeFailureDoesNotSpendExamAttempt()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, Passed(8f));

            Assert.Throws<InvalidOperationException>(() => session.StartSubject(SubjectId.Volleyball));
            Assert.That(session.StartSubject(SubjectId.Sprint), Is.EqualTo(SessionRoute.Subject));
            Assert.That(session.SubmitResult(SubjectId.Sprint, CreateFailureResult()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_practice"));
        }

        [Test]
        public void ExamFailureCostsExactlyOneAttemptAndKeepsTheExamCheckpoint()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.StartSubject(SubjectId.Sprint);

            Assert.That(session.SubmitResult(SubjectId.Sprint, CreateFailureResult()), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
        }

        [Test]
        public void LastExamAttemptOpensSupplementaryPracticeInsteadOfGameOver()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");

            for (int attempt = 0; attempt < 5; attempt++)
            {
                Assert.That(session.StartSubject(SubjectId.Sprint), Is.EqualTo(SessionRoute.Subject));
                Assert.That(session.SubmitResult(SubjectId.Sprint, CreateFailureResult()), Is.EqualTo(SessionRoute.Map));
            }

            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.AwaitingPunishment, Is.True);
            Assert.That(session.PendingPunishmentSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(session.StartSubject(SubjectId.Sprint), Is.EqualTo(SessionRoute.Subject));
            Assert.That(session.Journey.ActiveAttempt.Mode, Is.EqualTo(ChallengeAttemptMode.Supplementary));
        }

        [Test]
        public void PassingExamRecordsResultAndOnlyImprovesBestScore()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, Passed(8f));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);

            Assert.That(session.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            var lowerResult = new ChallengeAttemptResult(context, true, new ChallengeMetrics(), Passed(6f));
            Assert.That(session.SubmitChallengeResult(lowerResult).Accepted, Is.True);

            SubjectRecord record = session.GetRecord(SubjectId.Sprint);
            Assert.That(record.BestScore, Is.EqualTo(8f));
            Assert.That(record.BestRank, Is.EqualTo(Rank.A));
        }

        [Test]
        public void AcceptedExamResultIsCopiedAndFailureCannotReplaceIt()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.StartSubject(SubjectId.Sprint);
            var accepted = Passed(8f);
            session.SubmitResult(SubjectId.Sprint, accepted);
            accepted.Pass = false;
            accepted.Score = 1f;
            accepted.Rank = Rank.F;

            Assert.That(session.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            var failed = new MinigameResult(false, 10f, Rank.S);
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, false,
                new ChallengeMetrics(), failed));

            MinigameResult bestResult = session.GetRecord(SubjectId.Sprint).BestResult;
            Assert.That(bestResult.Pass, Is.True);
            Assert.That(bestResult.Score, Is.EqualTo(8f));
            Assert.That(bestResult.Rank, Is.EqualTo(Rank.A));
        }

        [Test]
        public void FailedExamBonusScoreCannotCreatePassingRecord()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 10f, Rank.S));

            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.False);
            Assert.That(session.GetRecord(SubjectId.Sprint).BestScore, Is.Zero);
        }

        static MinigameResult CreateFailureResult() => new MinigameResult(false, 0f, Rank.F);
        static MinigameResult Passed(float score) => new MinigameResult(true, score, ScoreUtil.ToRank(score));
    }
}
