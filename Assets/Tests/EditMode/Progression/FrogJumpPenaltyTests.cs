using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FrogJumpPenaltyTests
    {
        [Test]
        public void LearnFailureNeverCreatesAFrogJump()
        {
            var session = new GameSession();
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "sprint_learn", false);
            Assert.That(outcome.FrogJumpRequired, Is.False);
            Assert.That(session.Journey.PendingFrogJump, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(5));
        }

        [Test]
        public void FirstFailureDefersTheLifeAndAFrogWinKeepsIt()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyCommitOutcome outcome = JourneyTestData.Play(session, "sprint_practice", false);

            Assert.That(outcome.FrogJumpRequired, Is.True);
            Assert.That(outcome.FrogJumpSavesLife, Is.True);
            Assert.That(outcome.AttemptsRemaining, Is.EqualTo(5));
            FrogJumpPending pending = session.Journey.PendingFrogJump;
            Assert.That(pending.FailedChallengeId, Is.EqualTo("sprint_practice"));
            Assert.That(session.Journey.TryApplyFrogJump(pending.Id, true), Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.Journey.PendingFrogJump, Is.Null);
        }

        [Test]
        public void FirstFailureThenFrogLossCostsOneLife()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, false), Is.True);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void SecondFailureCostsALifeAtCommitAndTheFrogResultChangesNothing()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);

            JourneyCommitOutcome second = JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(second.AttemptsRemaining, Is.EqualTo(4));
            Assert.That(second.FrogJumpSavesLife, Is.False);
            Assert.That(session.Journey.FailCount("sprint_exam"), Is.EqualTo(2));
            Assert.That(session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true), Is.True);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void PassingResetsOnlyThatChallengesCount()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyTestData.Play(session, "sprint_practice", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            JourneyTestData.Play(session, "sprint_practice", true);
            Assert.That(session.Journey.FailCount("sprint_practice"), Is.Zero);

            JourneyCommitOutcome exam = JourneyTestData.Play(session, "sprint_exam", false);
            Assert.That(exam.FrogJumpSavesLife, Is.True);
        }

        [Test]
        public void PendingFrogJumpBlocksEveryStart()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            JourneyTestData.Play(session, "sprint_practice", false);
            Assert.That(session.TryStartChallenge("sprint_practice", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.TryStartChallenge("sprint_learn", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void FrogResultAppliesOnlyOnceAndOnlyForTheCurrentId()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            string id = session.Journey.PendingFrogJump.Id;
            Assert.That(session.Journey.TryApplyFrogJump("other", false), Is.False);
            Assert.That(session.Journey.TryApplyFrogJump(id, false), Is.True);
            Assert.That(session.Journey.TryApplyFrogJump(id, false), Is.False);
            Assert.That(session.Lives, Is.EqualTo(4));
        }

        [Test]
        public void ZeroLivesBlocksPracticeAndExamButNotLearnOrReview()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.Journey.SetAttemptsRemaining(0);
            Assert.That(session.Journey.TryBegin("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
            Assert.That(session.Journey.TryBegin("sprint_learn", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext review), Is.True);
            session.Journey.AbandonAttempt();

            var fresh = new GameSession();
            fresh.Journey.SetAttemptsRemaining(0);
            Assert.That(fresh.Journey.TryBegin("sprint_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
        }

        [Test]
        public void SecondFailureAtOneLifeLeavesZeroAndTheChallengeLocked()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            session.Journey.SetAttemptsRemaining(1);
            JourneyTestData.Play(session, "sprint_exam", false);
            session.Journey.TryApplyFrogJump(session.Journey.PendingFrogJump.Id, true);
            Assert.That(session.Lives, Is.Zero);
            Assert.That(session.Journey.TryBegin("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void SupplementaryModeIsAlwaysRejected()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_learn");
            Assert.That(session.Journey.TryBegin("sprint_practice", ChallengeAttemptMode.Supplementary,
                ChallengeDifficulty.Normal, out _), Is.False);
        }

        [Test]
        public void FailCountsAndPendingFrogJumpRoundTripThroughData()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            FrogJumpPending pending = session.Journey.PendingFrogJump;

            var restored = new JourneyProgress(ChallengeCatalog.LoadDefault());
            restored.Restore(session.Journey.ToData(), 5);
            Assert.That(restored.FailCount("sprint_exam"), Is.EqualTo(1));
            Assert.That(restored.PendingFrogJump.Id, Is.EqualTo(pending.Id));
            Assert.That(restored.PendingFrogJump.SavesLife, Is.True);
            Assert.That(restored.ForfeitPendingFrogJump(), Is.True);
            Assert.That(restored.AttemptsRemaining, Is.EqualTo(4));
            Assert.That(restored.ForfeitPendingFrogJump(), Is.False);
        }
    }
}
