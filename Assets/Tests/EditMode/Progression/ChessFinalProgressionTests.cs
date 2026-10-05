using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class ChessFinalProgressionTests
    {
        [Test]
        public void CatalogEndsWithTheChessFinal()
        {
            ChallengeCatalog catalog = ChallengeCatalog.LoadDefault();
            Assert.That(catalog.Ordered.Count, Is.EqualTo(10));
            ChallengeDefinition final = catalog.Get(ChallengeCatalog.FinalChallengeId);
            Assert.That(catalog.Ordered[9], Is.SameAs(final));
            Assert.That(final.Subject, Is.EqualTo(SubjectId.Chess));
            Assert.That(final.Kind, Is.EqualTo(ChallengeKind.Final));
            Assert.That(final.Difficulty, Is.EqualTo(ChallengeDifficulty.Normal));
            Assert.That(final.TimeLimit, Is.EqualTo(90f));
            Assert.That(final.TargetCount, Is.EqualTo(2));
        }

        [Test]
        public void ChessUnlocksOnlyAfterTheSoccerExam()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_practice");
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Chess), Is.False);
            Assert.That(session.TryStartChallenge("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);

            JourneyTestData.Play(session, "soccer_exam", true);
            Assert.That(session.Journey.IsSubjectUnlocked(SubjectId.Chess), Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("chess_final"));
            Assert.That(session.Journey.CourseComplete, Is.False);
        }

        [Test]
        public void FailingTheFinalNeverCostsALifeOrOwesAFrogJump()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            int lives = session.Lives;
            for (int i = 0; i < 4; i++)
            {
                JourneyCommitOutcome outcome = JourneyTestData.Play(session, "chess_final", false);
                Assert.That(outcome.Accepted, Is.True);
                Assert.That(outcome.FinalChallenge, Is.True);
                Assert.That(outcome.FrogJumpRequired, Is.False);
                Assert.That(outcome.AttemptsRemaining, Is.EqualTo(lives));
            }
            Assert.That(session.Journey.FailCount("chess_final"), Is.EqualTo(0));
            Assert.That(session.PendingFrogJump, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(lives));
        }

        [Test]
        public void TheFinalCanStartWithNoLivesLeft()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            session.Journey.SetAttemptsRemaining(0);
            Assert.That(session.Journey.TryBegin("chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            Assert.That(context.ChallengeId, Is.EqualTo("chess_final"));
        }

        [Test]
        public void WinningTheFinalCompletesTheCourseAndRecordsTheScore()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "chess_final", true);
            Assert.That(outcome.CourseComplete, Is.True);
            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(session.GetRecord(SubjectId.Chess).Passed, Is.True);
            Assert.That(session.StartSubject(SubjectId.Chess), Is.EqualTo(SessionRoute.Subject));
            Assert.That(session.Journey.ActiveAttempt.Mode, Is.EqualTo(ChallengeAttemptMode.FreePlay));
        }

        [Test]
        public void RestoredSavesDropFailCountsAndFrogJumpsForTheFinal()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "soccer_exam");
            SaveData data = session.ToSaveData();
            data.journey.failCounts.Add(new JourneyFailCountData { challengeId = "chess_final", count = 2 });
            data.journey.pendingFrogJump = new JourneyFrogJumpData
            {
                id = "x", failedAttemptId = "y", failedChallengeId = "chess_final", savesLife = true
            };
            var restored = new GameSession();
            restored.Restore(data);
            Assert.That(restored.Journey.FailCount("chess_final"), Is.EqualTo(0));
            Assert.That(restored.PendingFrogJump, Is.Null);
        }
    }
}
