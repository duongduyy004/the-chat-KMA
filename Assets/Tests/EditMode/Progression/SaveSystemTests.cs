using System;
using System.IO;
using NUnit.Framework;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class SaveSystemTests
    {
        private string temporaryDirectory;
        private SaveSystem saveSystem;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "KMA-SaveSystemTests", Guid.NewGuid().ToString("N"));
            saveSystem = new SaveSystem(() => temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsCampaignAndPreferenceData()
        {
            var expected = SaveData.CreateDefault();
            expected.lives = 2;
            expected.subjects[0].passed = true;
            expected.subjects[0].bestScore = 89.5f;
            expected.subjects[0].bestRank = Rank.A;
            expected.subjects[0].failedVisits = 3;
            expected.tutorialSeen[1] = true;
            expected.settings.musicVol = 0.25f;
            expected.settings.sfxVol = 0.75f;
            expected.settings.vibration = false;
            expected.settings.rhythmOffsetMs = -16f;

            saveSystem.Save(expected);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(2));
            Assert.That(actual.subjects[0].passed, Is.True);
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(89.5f));
            Assert.That(actual.subjects[0].bestRank, Is.EqualTo(Rank.A));
            Assert.That(actual.subjects[0].failedVisits, Is.EqualTo(3));
            Assert.That(actual.tutorialSeen[1], Is.True);
            Assert.That(actual.settings.musicVol, Is.EqualTo(0.25f));
            Assert.That(actual.settings.sfxVol, Is.EqualTo(0.75f));
            Assert.That(actual.settings.vibration, Is.False);
            Assert.That(actual.settings.rhythmOffsetMs, Is.EqualTo(-16f));
        }

        [Test]
        public void Migrate_CurrentVersion_ReturnsTheSameDataWithoutChanges()
        {
            var data = SaveData.CreateDefault();
            data.lives = 1;
            data.tutorialSeen[2] = true;

            SaveData migrated = saveSystem.Migrate(data);

            Assert.That(migrated, Is.SameAs(data));
            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.lives, Is.EqualTo(1));
            Assert.That(migrated.tutorialSeen[2], Is.True);
        }

        [Test]
        public void Migrate_OlderVersion_FillsMissingFieldsAndUpdatesVersion()
        {
            var olderData = new SaveData
            {
                version = 0,
                lives = 3,
                subjects = new[]
                {
                    new SubjectRecordData
                    {
                        id = SubjectId.Sprint,
                        passed = true,
                        bestScore = 71f,
                        bestRank = Rank.B,
                        failedVisits = 2
                    }
                },
            };

            SaveData migrated = saveSystem.Migrate(olderData);

            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.lives, Is.EqualTo(3));
            Assert.That(migrated.subjects, Has.Length.EqualTo(3));
            Assert.That(migrated.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(migrated.subjects[0].passed, Is.True);
            Assert.That(migrated.subjects[0].bestScore, Is.EqualTo(71f));
            Assert.That(migrated.subjects[0].bestRank, Is.EqualTo(Rank.B));
            Assert.That(migrated.subjects[0].failedVisits, Is.EqualTo(2));
            Assert.That(migrated.tutorialSeen, Has.Length.EqualTo(3));
            Assert.That(migrated.tutorialSeen, Is.All.False);
            Assert.That(migrated.settings.musicVol, Is.EqualTo(1f));
            Assert.That(migrated.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(migrated.settings.vibration, Is.True);
            Assert.That(migrated.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }

        [Test]
        public void Load_VersionOneSave_MigratesToNoActiveAttempt()
        {
            var versionOne = SaveData.CreateDefault();
            versionOne.version = 1;
            versionOne.lives = 4;
            versionOne.subjects[0].passed = true;
            versionOne.tutorialSeen = new[] { false, false, true, false, false, false, false };
            versionOne.settings.musicVol = 0.4f;
            versionOne.hasActiveSubject = true;
            versionOne.activeSubject = SubjectId.Football;
            versionOne.visitAttempt = 2;
            versionOne.awaitingPunishment = true;

            WriteRawSave(versionOne);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(4));
            Assert.That(actual.subjects[0].passed, Is.True);
            Assert.That(actual.tutorialSeen[1], Is.True);
            Assert.That(actual.settings.musicVol, Is.EqualTo(0.4f));
            Assert.That(actual.hasActiveSubject, Is.False);
            Assert.That(actual.visitAttempt, Is.EqualTo(1));
            Assert.That(actual.awaitingPunishment, Is.False);

            var restored = new GameSession();
            restored.Restore(actual);
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Map));
            Assert.That(restored.ActiveSubject, Is.Null);
        }

        [Test]
        public void Load_VersionTwoSave_RetainsTheActiveAttempt()
        {
            var current = SaveData.CreateDefault();
            current.version = 2;
            current.subjects = new[]
            {
                new SubjectRecordData { id = (SubjectId)0 },
                new SubjectRecordData { id = (SubjectId)1, passed = true, bestScore = 99f },
                new SubjectRecordData { id = (SubjectId)2, passed = true, bestScore = 8f, bestRank = Rank.A },
                new SubjectRecordData { id = (SubjectId)3, passed = true, bestScore = 98f },
                new SubjectRecordData { id = (SubjectId)4, passed = true, bestScore = 97f },
                new SubjectRecordData { id = (SubjectId)5, passed = true, bestScore = 7f, bestRank = Rank.B },
                new SubjectRecordData { id = (SubjectId)6, passed = true, bestScore = 6f, bestRank = Rank.C }
            };
            current.tutorialSeen = new[] { false, true, true, true, true, true, true };
            current.lives = 3;
            current.hasActiveSubject = true;
            current.activeSubject = SubjectId.Badminton;
            current.visitAttempt = 2;
            current.awaitingPunishment = true;

            saveSystem.Save(current);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(3));
            Assert.That(actual.hasActiveSubject, Is.True);
            Assert.That(actual.activeSubject, Is.EqualTo(SubjectId.Badminton));
            Assert.That(actual.visitAttempt, Is.EqualTo(2));
            Assert.That(actual.awaitingPunishment, Is.True);
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[1].id, Is.EqualTo(SubjectId.Badminton));
            Assert.That(actual.subjects[1].bestScore, Is.EqualTo(7f));
            Assert.That(actual.subjects[2].id, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.subjects[2].bestScore, Is.EqualTo(6f));
            Assert.That(actual.tutorialSeen, Is.EqualTo(new[] { false, true, true }));

            var restored = new GameSession();
            restored.Restore(actual);
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Badminton));
        }

        [Test]
        public void Load_VersionOneSaveWithoutRequiredFields_ReturnsDefaultData()
        {
            WriteRawJson("{\"version\":1}");

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_MissingFile_ReturnsDefaultData()
        {
            SaveData actual = saveSystem.Load();

            AssertDefaultData(actual);
        }

        [Test]
        public void Load_MalformedJson_ReturnsDefaultData()
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(saveSystem.SavePath, "{ this is not json");

            SaveData actual = saveSystem.Load();

            AssertDefaultData(actual);
        }

        [Test]
        public void Load_EmptyOrStructurallyInvalidFile_ReturnsDefaultData()
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(saveSystem.SavePath, "");

            AssertDefaultData(saveSystem.Load());

            File.WriteAllText(saveSystem.SavePath, "{\"version\":1}");

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_CurrentVersionWithDuplicateSubjectIds_ReturnsDefaultData()
        {
            var invalidData = SaveData.CreateDefault();
            invalidData.subjects[3].id = SubjectId.Sprint;

            WriteRawSave(invalidData);

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_CurrentVersionWithNullSubjectRecord_ReturnsDefaultData()
        {
            var invalidData = SaveData.CreateDefault();
            invalidData.subjects[3] = null;

            WriteRawSave(invalidData);

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_CurrentVersionWithUndefinedSubjectId_ReturnsDefaultData()
        {
            var invalidData = SaveData.CreateDefault();
            invalidData.subjects[2].id = (SubjectId)999;

            WriteRawSave(invalidData);

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_LegacyDataWithMissingSubjectsAndShortTutorials_MigratesToCompleteDefaults()
        {
            var legacyData = new SaveData
            {
                version = 0,
                lives = 3,
                subjects = null,
                tutorialSeen = new[] { true, false },
                settings = null
            };

            WriteRawSave(legacyData);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(3));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[0].passed, Is.False);
            Assert.That(actual.tutorialSeen, Has.Length.EqualTo(3));
            Assert.That(actual.tutorialSeen[0], Is.True);
            Assert.That(actual.tutorialSeen[1], Is.False);
            for (int i = 2; i < actual.tutorialSeen.Length; i++)
            {
                Assert.That(actual.tutorialSeen[i], Is.False);
            }

            Assert.That(actual.settings.musicVol, Is.EqualTo(1f));
            Assert.That(actual.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(actual.settings.vibration, Is.True);
            Assert.That(actual.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }

        [Test]
        public void Load_ValidLegacyJsonWithOmittedVersionOneFields_UsesCurrentDefaults()
        {
            WriteRawJson("{\"version\":0,\"lives\":3}");

            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(3));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.tutorialSeen, Has.Length.EqualTo(3));
            Assert.That(actual.tutorialSeen, Is.All.False);
            Assert.That(actual.settings.musicVol, Is.EqualTo(1f));
            Assert.That(actual.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(actual.settings.vibration, Is.True);
            Assert.That(actual.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }

        [Test]
        public void Load_EmptyLegacyObject_ReturnsDefaultData()
        {
            WriteRawJson("{}");

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_UnknownLegacyShape_ReturnsDefaultData()
        {
            WriteRawJson("{\"legacyVersion\":0,\"lives\":1}");

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_FutureVersion_ReturnsDefaultData()
        {
            var futureData = SaveData.CreateDefault();
            futureData.version = SaveData.CurrentVersion + 1;
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(saveSystem.SavePath, UnityEngine.JsonUtility.ToJson(futureData));

            SaveData actual = saveSystem.Load();

            AssertDefaultData(actual);
        }

        [Test]
        public void Save_LeavesReadableSaveAndNoTemporaryFile()
        {
            saveSystem.Save(SaveData.CreateDefault());

            Assert.That(File.Exists(saveSystem.SavePath), Is.True);
            Assert.That(File.Exists(Path.Combine(temporaryDirectory, "save.tmp")), Is.False);
            Assert.That(File.ReadAllText(saveSystem.SavePath), Does.Contain("\"version\""));
        }

        [Test]
        public void DeleteSave_RemovesOnlyThisInstanceSaveAndTemporaryFiles()
        {
            saveSystem.Save(SaveData.CreateDefault());
            File.WriteAllText(Path.Combine(temporaryDirectory, "save.tmp"), "incomplete");
            string unrelatedPath = Path.Combine(temporaryDirectory, "keep.txt");
            File.WriteAllText(unrelatedPath, "keep");

            saveSystem.DeleteSave();

            Assert.That(File.Exists(saveSystem.SavePath), Is.False);
            Assert.That(File.Exists(Path.Combine(temporaryDirectory, "save.tmp")), Is.False);
            Assert.That(File.Exists(unrelatedPath), Is.True);
        }

        [Test]
        public void ResetSave_UsesDefaultsWhileCarryingForwardOnlySettingsAndTutorialFlags()
        {
            var existing = SaveData.CreateDefault();
            existing.lives = 1;
            existing.subjects[0].passed = true;
            existing.tutorialSeen[2] = true;
            existing.settings.musicVol = 0.4f;
            existing.settings.sfxVol = 0.6f;
            existing.settings.vibration = false;
            existing.settings.rhythmOffsetMs = 20f;
            saveSystem.Save(existing);

            SaveData reset = SaveData.CreateDefault();
            reset.settings = existing.settings;
            reset.tutorialSeen = existing.tutorialSeen;
            saveSystem.Save(reset);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.lives, Is.EqualTo(5));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].passed, Is.False);
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(0f));
            Assert.That(actual.tutorialSeen[2], Is.True);
            Assert.That(actual.settings.musicVol, Is.EqualTo(0.4f));
            Assert.That(actual.settings.sfxVol, Is.EqualTo(0.6f));
            Assert.That(actual.settings.vibration, Is.False);
            Assert.That(actual.settings.rhythmOffsetMs, Is.EqualTo(20f));
        }

        private static void AssertDefaultData(SaveData data)
        {
            Assert.That(data, Is.Not.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.lives, Is.EqualTo(5));
            SubjectId[] subjectIds = (SubjectId[])Enum.GetValues(typeof(SubjectId));
            Assert.That(data.subjects, Has.Length.EqualTo(subjectIds.Length));
            for (int i = 0; i < subjectIds.Length; i++)
            {
                Assert.That(data.subjects[i], Is.Not.Null);
                Assert.That(data.subjects[i].id, Is.EqualTo(subjectIds[i]));
                Assert.That(data.subjects[i].passed, Is.False);
                Assert.That(data.subjects[i].bestScore, Is.EqualTo(0f));
                Assert.That(data.subjects[i].bestRank, Is.EqualTo(Rank.F));
                Assert.That(data.subjects[i].failedVisits, Is.EqualTo(0));
            }
            Assert.That(data.hasActiveSubject, Is.False);
            Assert.That(data.visitAttempt, Is.EqualTo(1));
            Assert.That(data.awaitingPunishment, Is.False);
            Assert.That(data.tutorialSeen, Has.Length.EqualTo(3));
            Assert.That(data.tutorialSeen, Is.All.False);
            Assert.That(data.settings.musicVol, Is.EqualTo(1f));
            Assert.That(data.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(data.settings.vibration, Is.True);
            Assert.That(data.settings.rhythmOffsetMs, Is.EqualTo(0f));
        }

        private void WriteRawSave(SaveData data)
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(saveSystem.SavePath, UnityEngine.JsonUtility.ToJson(data));
        }

        private void WriteRawJson(string json)
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(saveSystem.SavePath, json);
        }
    }
}
