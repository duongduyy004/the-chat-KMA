using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class StudentJourneyFlowTests
    {
        [Test]
        public void NewStudent_CanCompleteEveryChallengeAndUnlockAllSubjects()
        {
            var session = new GameSession();
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));

            foreach (ChallengeDefinition challenge in session.Journey.Catalog.Ordered)
            {
                Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo(challenge.Id));
                JourneyCommitOutcome outcome = JourneyGameplayDriver.CompleteActiveChallenge(session);
                Assert.That(outcome.Accepted, Is.True, challenge.Id);
                Assert.That(session.Journey.IsChallengeComplete(challenge.Id), Is.True, challenge.Id);
            }

            Assert.That(session.Journey.CourseComplete, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(GameSession.MaxLives));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(session.GetRecord(SubjectId.Volleyball).Passed, Is.True);
            Assert.That(session.GetRecord(SubjectId.Football).Passed, Is.True);
        }

        [Test]
        public void FailedExam_CanBeRecoveredThroughSupplementaryPracticeAndRetry()
        {
            var session = new GameSession();
            JourneyCommitOutcome learn = JourneyGameplayDriver.CompleteActiveChallenge(session);
            JourneyCommitOutcome practice = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(learn.Accepted && practice.Accepted, Is.True);

            JourneyCommitOutcome failure = default;
            for (int attempt = 0; attempt < GameSession.MaxLives; attempt++)
            {
                failure = JourneyGameplayDriver.CompleteActiveChallenge(session, false);
                Assert.That(failure.Accepted, Is.True);
            }
            Assert.That(failure.AwaitingSupplementary, Is.True);
            Assert.That(session.Lives, Is.Zero);
            JourneyCommitOutcome recovery = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(recovery.Accepted, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));

            JourneyCommitOutcome retry = JourneyGameplayDriver.CompleteActiveChallenge(session);
            Assert.That(retry.Accepted, Is.True);
            Assert.That(session.Journey.IsChallengeComplete("sprint_exam"), Is.True);
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);
        }
    }
}
