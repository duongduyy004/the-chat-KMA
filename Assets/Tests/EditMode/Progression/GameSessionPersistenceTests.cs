using System;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameSessionPersistenceTests
    {
        [Test]
        public void RoundTrip_AfterFailedLearn_RestartsAtTheSameChallengeWithoutSpendingALife()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Sprint);
            original.SubmitResult(SubjectId.Sprint, Failed());

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ActiveSubject, Is.Null);
            Assert.That(restored.PendingPunishmentSubject, Is.Null);
            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("sprint_learn"));
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void ResumeRoute_WithoutAnAttempt_IsMap()
        {
            var session = new GameSession();

            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.AwaitingPunishment, Is.False);
        }

        [Test]
        public void RoundTrip_DuringUnresolvedChallenge_RestartsAtThatCheckpoint()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "sprint_exam");
            Assert.That(original.TryStartChallenge("volleyball_learn", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Easy, out _), Is.True);

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.ActiveSubject, Is.Null);
            Assert.That(restored.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_learn"));
            Assert.That(restored.VisitAttempt, Is.EqualTo(1));
            Assert.That(restored.AwaitingPunishment, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8f));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
        }

        [Test]
        public void RoundTrip_AfterFailingTwoExams_KeepsRecordsAndLives()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "sprint_practice");
            JourneyTestData.Play(original, "sprint_exam", false);
            JourneyTestData.CompleteThrough(original, "volleyball_practice");
            JourneyTestData.Play(original, "volleyball_exam", false);

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.ActiveSubject, Is.Null);
            Assert.That(restored.PendingPunishmentSubject, Is.Null);
            Assert.That(restored.AwaitingPunishment, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(3));
            Assert.That(restored.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
            Assert.That(restored.GetRecord(SubjectId.Volleyball).FailedVisits, Is.EqualTo(1));
        }

        [Test]
        public void RoundTrip_WithoutAnAttempt_ResumesMap()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "soccer_exam");

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.ActiveSubject, Is.Null);
            Assert.That(restored.AwaitingPunishment, Is.False);
            Assert.That(restored.GetRecord(SubjectId.Football).Passed, Is.True);
        }

        [Test]
        public void ToSaveData_ExportsTheActiveAttemptFields()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);

            SaveData exported = session.ToSaveData();

            Assert.That(exported.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(exported.hasActiveSubject, Is.True);
            Assert.That(exported.activeSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(exported.journey.activeAttempt.challengeId, Is.EqualTo("sprint_learn"));
            Assert.That(exported.visitAttempt, Is.EqualTo(1));
            Assert.That(exported.awaitingPunishment, Is.False);

            session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 6f, Rank.C));
            SaveData cleared = session.ToSaveData();

            Assert.That(cleared.hasActiveSubject, Is.False);
            Assert.That(cleared.visitAttempt, Is.EqualTo(1));
            Assert.That(cleared.awaitingPunishment, Is.False);
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(3)]
        public void Restore_OutOfRangeVisitAttempt_FallsBackToNoActiveAttempt(int visitAttempt)
        {
            SaveData data = SaveData.CreateDefault();
            data.hasActiveSubject = true;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = visitAttempt;

            AssertNoActiveAttempt(data);
        }

        [Test]
        public void Restore_PunishmentStateWithoutAnActiveSubject_FallsBackToNoActiveAttempt()
        {
            SaveData data = SaveData.CreateDefault();
            data.hasActiveSubject = false;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = 2;
            data.awaitingPunishment = true;

            AssertNoActiveAttempt(data);
        }

        [Test]
        public void Restore_UndefinedActiveSubject_FallsBackToNoActiveAttempt()
        {
            SaveData data = SaveData.CreateDefault();
            data.hasActiveSubject = true;
            data.activeSubject = (SubjectId)999;
            data.visitAttempt = 1;

            AssertNoActiveAttempt(data);
        }

        [Test]
        public void Restore_AwaitingPunishmentOnAttemptOne_FallsBackToNoActiveAttempt()
        {
            SaveData data = SaveData.CreateDefault();
            data.hasActiveSubject = true;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = 1;
            data.awaitingPunishment = true;

            AssertNoActiveAttempt(data);
        }

        [Test]
        public void Restore_ActiveAttemptWithoutLives_FallsBackToNoActiveAttempt()
        {
            SaveData data = SaveData.CreateDefault();
            data.lives = 0;
            data.hasActiveSubject = true;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = 1;

            var session = new GameSession();
            session.Restore(data);
            Assert.That(session.ActiveSubject, Is.Null);
            Assert.That(session.AwaitingPunishment, Is.True);
            Assert.That(session.PendingPunishmentSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("sprint_practice"));
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void Restore_ReplacesAPreviouslyRestoredAttempt()
        {
            SaveData active = SaveData.CreateDefault();
            active.hasActiveSubject = true;
            active.activeSubject = SubjectId.Football;
            active.visitAttempt = 2;
            active.awaitingPunishment = true;

            var session = new GameSession();
            session.Restore(active);
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));

            session.Restore(SaveData.CreateDefault());

            Assert.That(session.ActiveSubject, Is.Null);
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.AwaitingPunishment, Is.False);
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void ToSaveDataAndRestore_PreserveCampaignState()
        {
            var original = new GameSession();
            JourneyTestData.CompleteThrough(original, "volleyball_exam");
            JourneyTestData.CompleteThrough(original, "soccer_practice");
            JourneyTestData.Play(original, "soccer_exam", false);

            var data = original.ToSaveData();
            data.lives = 3;
            data.settings = new Settings
            {
                musicVol = 0.25f,
                sfxVol = 0.75f,
                vibration = false,
                rhythmOffsetMs = -42f
            };
            data.tutorialSeen[0] = true;
            data.tutorialSeen[1] = true;

            var restored = new GameSession();
            restored.Restore(data);

            Assert.That(restored.Lives, Is.EqualTo(3));
            Assert.That(restored.Records, Has.Count.EqualTo(3));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8f));
            Assert.That(restored.GetRecord(SubjectId.Football).FailedVisits, Is.EqualTo(1));

            Assert.That(data.settings.musicVol, Is.EqualTo(0.25f));
            Assert.That(data.settings.sfxVol, Is.EqualTo(0.75f));
            Assert.That(data.settings.vibration, Is.False);
            Assert.That(data.settings.rhythmOffsetMs, Is.EqualTo(-42f));
            Assert.That(data.tutorialSeen[0], Is.True);
            Assert.That(data.tutorialSeen[1], Is.True);

            var restoredData = restored.ToSaveData();
            Assert.That(restoredData.settings.musicVol, Is.EqualTo(1f));
            Assert.That(restoredData.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(restoredData.settings.vibration, Is.True);
            Assert.That(restoredData.settings.rhythmOffsetMs, Is.Zero);
            Assert.That(restoredData.tutorialSeen, Is.All.False);
        }

        [Test]
        public void Restore_RebuildsEverySubjectRecordFromMatchingData()
        {
            var subjectIds = (SubjectId[])Enum.GetValues(typeof(SubjectId));
            var data = SaveData.CreateDefault();
            data.journey.completedChallengeIds.AddRange(new[]
                { "sprint_learn", "sprint_practice", "sprint_exam" });
            data.subjects = new SubjectRecordData[subjectIds.Length];
            for (int index = 0; index < subjectIds.Length; index++)
            {
                data.subjects[index] = new SubjectRecordData
                {
                    id = subjectIds[index],
                    passed = index % 2 == 0,
                    bestScore = 10f - index,
                    bestRank = (Rank)(index % 6),
                    failedVisits = index + 1
                };
            }

            var restored = new GameSession();
            restored.Restore(data);

            foreach (SubjectRecordData expected in data.subjects)
            {
                SubjectRecord actual = restored.GetRecord(expected.id);
                bool passed = expected.id == SubjectId.Sprint;
                Assert.That(actual.Passed, Is.EqualTo(passed), expected.id.ToString());
                Assert.That(actual.BestScore, Is.EqualTo(expected.bestScore), expected.id.ToString());
                Assert.That(actual.BestRank, Is.EqualTo(expected.bestRank), expected.id.ToString());
                Assert.That(actual.FailedVisits, Is.EqualTo(expected.failedVisits), expected.id.ToString());
            }
        }

        [Test]
        public void ToSaveData_CopiesRecordsAndDoesNotExposeSessionOwnedState()
        {
            var session = new GameSession();
            JourneyTestData.CompleteThrough(session, "sprint_exam");
            Assert.That(session.TryStartChallenge("sprint_exam", ChallengeAttemptMode.Review,
                ChallengeDifficulty.Normal, out ChallengeAttemptContext context), Is.True);
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                new ChallengeMetrics(), new MinigameResult(true, 9f, Rank.S)));

            var exported = session.ToSaveData();
            exported.lives = 0;
            exported.subjects[0].passed = false;
            exported.settings.musicVol = 0f;
            exported.tutorialSeen[0] = true;

            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(session.GetRecord(SubjectId.Sprint).BestResult.Score, Is.EqualTo(9f));

            var exportedAgain = session.ToSaveData();
            Assert.That(exportedAgain.settings.musicVol, Is.EqualTo(1f));
            Assert.That(exportedAgain.tutorialSeen, Is.All.False);
        }

        [Test]
        public void Restore_ClampsLivesAndDefaultsMissingRecords()
        {
            var data = SaveData.CreateDefault();
            data.lives = 42;
            data.journey.completedChallengeIds.AddRange(new[]
                { "sprint_learn", "sprint_practice", "sprint_exam" });
            data.subjects = new[]
            {
                new SubjectRecordData
                {
                    id = SubjectId.Sprint,
                    passed = true,
                    bestScore = 7f,
                    bestRank = Rank.B,
                    failedVisits = 2
                }
            };

            var restored = new GameSession();
            restored.Restore(data);

            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestResult, Is.Not.Null);
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestResult.Score, Is.EqualTo(7f));
            Assert.That(restored.GetRecord(SubjectId.Football).Passed, Is.False);
            Assert.That(restored.GetRecord(SubjectId.Football).FailedVisits, Is.Zero);

            data.lives = -1;
            restored.Restore(data);
            Assert.That(restored.Lives, Is.Zero);
        }

        [Test]
        public void Restore_RejectsNullData()
        {
            Assert.Throws<ArgumentNullException>(() => new GameSession().Restore(null));
        }

        [Test]
        public void SubjectRecordFromData_RebuildsSnapshotWithoutPublicSetters()
        {
            var record = SubjectRecord.FromData(new SubjectRecordData
            {
                id = SubjectId.Sprint,
                passed = true,
                bestScore = 8f,
                bestRank = Rank.A,
                failedVisits = 3
            });

            Assert.That(record.Passed, Is.True);
            Assert.That(record.BestScore, Is.EqualTo(8f));
            Assert.That(record.BestRank, Is.EqualTo(Rank.A));
            Assert.That(record.FailedVisits, Is.EqualTo(3));
            Assert.That(record.BestResult, Is.Not.Null);
            Assert.That(record.BestResult.Pass, Is.True);
        }

        [Test]
        public void Restore_LegacyPunishmentSave_NeverResumesIntoPunishment()
        {
            SaveData data = SaveData.CreateDefault();
            data.lives = 3;
            data.hasActiveSubject = true;
            data.activeSubject = SubjectId.Sprint;
            data.visitAttempt = 2;
            data.awaitingPunishment = true;

            var session = new GameSession();
            session.Restore(data);

            Assert.That(session.AwaitingPunishment, Is.False);
            Assert.That(session.PendingPunishmentSubject, Is.Null);
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.ActiveSubject, Is.Null);
            Assert.That(session.Lives, Is.EqualTo(3));
            Assert.That(session.Journey.AwaitingSupplementary, Is.False);
        }

        static MinigameResult Failed() => new MinigameResult(false, 0f, Rank.F);

        static void AssertNoActiveAttempt(SaveData data)
        {
            var session = new GameSession();
            session.Restore(data);

            Assert.That(session.ActiveSubject, Is.Null);
            bool exhausted = data.lives == 0;
            Assert.That(session.PendingPunishmentSubject,
                Is.EqualTo(exhausted ? SubjectId.Sprint : (SubjectId?)null));
            Assert.That(session.AwaitingPunishment, Is.EqualTo(exhausted));
            Assert.That(session.VisitAttempt, Is.EqualTo(1));
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
        }

        static GameSession RoundTrip(GameSession source)
        {
            SaveData persisted = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(source.ToSaveData()));
            var restored = new GameSession();
            restored.Restore(persisted);
            return restored;
        }
    }
}
