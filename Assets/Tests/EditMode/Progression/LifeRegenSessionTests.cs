using System;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class LifeRegenSessionTests
    {
        static GameSession FailOnceAndLose(FakeClock clock)
        {
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            session.TryApplyFrogJump(session.PendingFrogJump.Id, false);
            return session;
        }

        [Test]
        public void SpendingFromFullStartsTheCountdown()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            Assert.That(session.Lives, Is.EqualTo(4));
            Assert.That(session.TimeUntilNextLife, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void RefreshGrantsLivesAfterTimePasses()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            clock.Advance(TimeSpan.FromMinutes(5));
            Assert.That(session.RefreshLives(), Is.True);
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.TimeUntilNextLife, Is.Null);
        }

        [Test]
        public void StartingAChallengeAppliesPendingRegenFirst()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            session.Journey.SetAttemptsRemaining(0);
            session.RefreshLives();
            clock.Advance(TimeSpan.FromMinutes(5));
            Assert.That(session.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal, out _), Is.True);
            Assert.That(session.Lives, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTripKeepsTheMarkAndRegensWhileClosed()
        {
            var clock = new FakeClock();
            GameSession session = FailOnceAndLose(clock);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData()));
            Assert.That(saved.version, Is.EqualTo(8));
            Assert.That(saved.nextLifeAtUtcTicks, Is.EqualTo(session.NextLifeAtUtcTicks));

            clock.Advance(TimeSpan.FromMinutes(7));
            var restored = new GameSession(null, clock);
            restored.Restore(saved);
            Assert.That(restored.Lives, Is.EqualTo(5));
        }

        [Test]
        public void RestoreForfeitsAnUnfinishedFirstFailureFrogJumpOnce()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            SaveData saved = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(session.ToSaveData()));

            var restored = new GameSession(null, clock);
            restored.Restore(saved);
            Assert.That(restored.ForfeitedFrogJumpOnRestore, Is.True);
            Assert.That(restored.PendingFrogJump, Is.Null);
            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));

            SaveData after = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(restored.ToSaveData()));
            var again = new GameSession(null, clock);
            again.Restore(after);
            Assert.That(again.ForfeitedFrogJumpOnRestore, Is.False);
            Assert.That(again.Lives, Is.EqualTo(4));
        }

        [Test]
        public void RestoreSnapshotDoesNotForfeit()
        {
            var clock = new FakeClock();
            var session = new GameSession(null, clock);
            JourneyTestData.CompleteThrough(session, "sprint_practice");
            JourneyTestData.Play(session, "sprint_exam", false);
            SaveData snapshot = session.ToSaveData();
            session.RestoreSnapshot(snapshot);
            Assert.That(session.PendingFrogJump, Is.Not.Null);
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.FrogJump));
            Assert.That(session.Lives, Is.EqualTo(5));
        }

        [Test]
        public void Version7SaveKeepsJourneyDropsSupplementaryAndStartsRegen()
        {
            var clock = new FakeClock();
            SaveData data = SaveData.CreateDefault();
            data.version = 7;
            data.lives = 0;
            data.journey.completedChallengeIds.AddRange(new[] { "sprint_learn", "sprint_practice" });
            data.journey.awaitingSupplementaryChallengeId = "sprint_practice";
            data.journey.supplementaryRounds = 2;

            var restored = new GameSession(null, clock);
            restored.Restore(data);
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("sprint_exam"));
            Assert.That(restored.Lives, Is.Zero);
            Assert.That(restored.Journey.SupplementaryRounds, Is.EqualTo(2));
            Assert.That(restored.TimeUntilNextLife, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }
    }
}
