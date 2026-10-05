using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyCommitTests
    {
        [Test]
        public void SaveFailureRestoresActiveAttemptAndRetryCommitsOnce()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "volleyball_practice");
            session.TryStartChallenge("volleyball_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context);
            var result = new ChallengeAttemptResult(context, false, new ChallengeMetrics(),
                new MinigameResult(false, 0f, Rank.F));
            bool failSave = true;
            int notifications = 0;
            session.JourneyChanged += () => notifications++;
            var coordinator = new JourneySaveCoordinator(session, (out string error) =>
            {
                error = null;
                if (failSave) throw new System.IO.IOException("disk full");
                return true;
            });

            Assert.That(coordinator.TryCommit(result, out _, out string error), Is.False);
            Assert.That(error, Is.EqualTo("disk full"));
            Assert.That(session.Journey.ActiveAttempt.AttemptId, Is.EqualTo(context.AttemptId));
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(notifications, Is.Zero);

            failSave = false;
            Assert.That(coordinator.TryCommit(result, out _, out _), Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.PendingFrogJump, Is.Not.Null);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(coordinator.TryCommit(result, out _, out _), Is.False);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(session.TryApplyFrogJump(session.PendingFrogJump.Id, false), Is.True);
            Assert.That(session.Lives, Is.EqualTo(4));
        }
    }
}
