using System;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class GameSessionPersistenceTests
    {
        [Test]
        public void RoundTrip_AfterFirstFailure_KeepsActiveAttemptAndPendingPunishment()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Sprint);
            original.SubmitResult(SubjectId.Sprint, Failed());

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(restored.PendingPunishmentSubject, Is.EqualTo(SubjectId.Sprint));
            Assert.That(restored.Lives, Is.EqualTo(5));
        }

        [Test]
        public void RoundTrip_AfterPunishment_KeepsTheRetryAttemptActive()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Endurance);
            original.SubmitResult(SubjectId.Endurance, Failed());
            original.CompletePunishment();

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Endurance));
            Assert.That(restored.PendingPunishmentSubject, Is.Null);
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
        public void RoundTrip_DuringAttemptOne_ResumesTheSubjectWithRecordsAndLives()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Sprint);
            original.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
            original.StartSubject(SubjectId.Volleyball);

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(restored.VisitAttempt, Is.EqualTo(1));
            Assert.That(restored.AwaitingPunishment, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(5));
            Assert.That(restored.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8f));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
        }

        [Test]
        public void RoundTrip_AwaitingPunishment_ResumesPunishmentWithRecordsAndLives()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Sprint);
            original.SubmitResult(SubjectId.Sprint, Failed());
            original.CompletePunishment();
            original.SubmitResult(SubjectId.Sprint, Failed());
            original.StartSubject(SubjectId.Endurance);
            original.SubmitResult(SubjectId.Endurance, Failed());

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Punishment));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Endurance));
            Assert.That(restored.PendingPunishmentSubject, Is.EqualTo(SubjectId.Endurance));
            Assert.That(restored.VisitAttempt, Is.EqualTo(2));
            Assert.That(restored.AwaitingPunishment, Is.True);
            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
        }

        [Test]
        public void RoundTrip_DuringAttemptTwo_ResumesRetryAndStillCostsALifeOnFailure()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Badminton);
            original.SubmitResult(SubjectId.Badminton, Failed());
            original.CompletePunishment();

            GameSession restored = RoundTrip(original);

            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.RetrySubject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Badminton));
            Assert.That(restored.VisitAttempt, Is.EqualTo(2));
            Assert.That(restored.AwaitingPunishment, Is.False);
            Assert.That(restored.Lives, Is.EqualTo(5));

            Assert.That(restored.SubmitResult(SubjectId.Badminton, Failed()), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.Lives, Is.EqualTo(4));
            Assert.That(restored.GetRecord(SubjectId.Badminton).FailedVisits, Is.EqualTo(1));
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void RoundTrip_WithoutAnAttempt_ResumesMap()
        {
            var original = new GameSession();
            original.StartSubject(SubjectId.Football);
            original.SubmitResult(SubjectId.Football, new MinigameResult(true, 7f, Rank.B));

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
            session.StartSubject(SubjectId.PingPong);
            session.SubmitResult(SubjectId.PingPong, Failed());

            SaveData exported = session.ToSaveData();

            Assert.That(exported.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(exported.hasActiveSubject, Is.True);
            Assert.That(exported.activeSubject, Is.EqualTo(SubjectId.PingPong));
            Assert.That(exported.visitAttempt, Is.EqualTo(2));
            Assert.That(exported.awaitingPunishment, Is.True);

            session.CompletePunishment();
            session.SubmitResult(SubjectId.PingPong, new MinigameResult(true, 6f, Rank.C));
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

            AssertNoActiveAttempt(data);
        }

        [Test]
        public void Restore_ReplacesAPreviouslyRestoredAttempt()
        {
            SaveData active = SaveData.CreateDefault();
            active.hasActiveSubject = true;
            active.activeSubject = SubjectId.Basketball;
            active.visitAttempt = 2;
            active.awaitingPunishment = true;

            var session = new GameSession();
            session.Restore(active);
            Assert.That(session.ResumeRoute(), Is.EqualTo(SessionRoute.Punishment));

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
            original.StartSubject(SubjectId.Sprint);
            original.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
            original.StartSubject(SubjectId.Endurance);
            original.SubmitResult(SubjectId.Endurance, Failed());
            original.CompletePunishment();
            original.SubmitResult(SubjectId.Endurance, Failed());

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
            data.tutorialSeen[3] = true;

            var restored = new GameSession();
            restored.Restore(data);

            Assert.That(restored.Lives, Is.EqualTo(3));
            Assert.That(restored.Records, Has.Count.EqualTo(7));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
            Assert.That(restored.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(8f));
            Assert.That(restored.GetRecord(SubjectId.Endurance).FailedVisits, Is.EqualTo(1));

            Assert.That(data.settings.musicVol, Is.EqualTo(0.25f));
            Assert.That(data.settings.sfxVol, Is.EqualTo(0.75f));
            Assert.That(data.settings.vibration, Is.False);
            Assert.That(data.settings.rhythmOffsetMs, Is.EqualTo(-42f));
            Assert.That(data.tutorialSeen[0], Is.True);
            Assert.That(data.tutorialSeen[3], Is.True);

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
                Assert.That(actual.Passed, Is.EqualTo(expected.passed), expected.id.ToString());
                Assert.That(actual.BestScore, Is.EqualTo(expected.bestScore), expected.id.ToString());
                Assert.That(actual.BestRank, Is.EqualTo(expected.bestRank), expected.id.ToString());
                Assert.That(actual.FailedVisits, Is.EqualTo(expected.failedVisits), expected.id.ToString());
            }
        }

        [Test]
        public void ToSaveData_CopiesRecordsAndDoesNotExposeSessionOwnedState()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 9f, Rank.S));

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
            Assert.That(restored.GetRecord(SubjectId.Endurance).Passed, Is.False);
            Assert.That(restored.GetRecord(SubjectId.Endurance).FailedVisits, Is.Zero);

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

        static MinigameResult Failed() => new MinigameResult(false, 0f, Rank.F);

        static void AssertNoActiveAttempt(SaveData data)
        {
            var session = new GameSession();
            session.Restore(data);

            Assert.That(session.ActiveSubject, Is.Null);
            Assert.That(session.PendingPunishmentSubject, Is.Null);
            Assert.That(session.AwaitingPunishment, Is.False);
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
