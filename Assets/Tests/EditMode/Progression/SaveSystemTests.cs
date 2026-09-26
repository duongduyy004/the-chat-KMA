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
        public void SettingsOnlySave_RoundTripsWithoutBecomingACampaign()
        {
            var data = SaveData.CreateDefault();
            data.settingsOnly = true;
            data.settings.musicVol = .25f;
            saveSystem.Save(data);
            var loaded = saveSystem.Load();
            Assert.That(saveSystem.HasLoadedValidSave, Is.True);
            Assert.That(loaded.settingsOnly, Is.True);
            Assert.That(loaded.settings.musicVol, Is.EqualTo(.25f));
        }

        [Test]
        public void ExistingSaveWithoutSettingsOnlyMarker_RemainsACampaign()
        {
            saveSystem.Save(SaveData.CreateDefault());
            var json = File.ReadAllText(saveSystem.SavePath)
                .Replace("\"settingsOnly\": false,", "");
            File.WriteAllText(saveSystem.SavePath, json);
            var loaded = saveSystem.Load();
            Assert.That(saveSystem.HasLoadedValidSave, Is.True);
            Assert.That(loaded.settingsOnly, Is.False);
        }

        [Test]
        public void CorruptedSave_AfterValidLoad_DoesNotQualifyForContinue()
        {
            saveSystem.Save(SaveData.CreateDefault());
            saveSystem.Load();
            Assert.That(saveSystem.HasLoadedValidSave, Is.True);
            File.WriteAllText(saveSystem.SavePath, "broken json");
            saveSystem.Load();
            Assert.That(saveSystem.HasLoadedValidSave, Is.False);
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
        public void SaveAndLoad_PreservesExistingFootballBestScoreAndRank()
        {
            var expected = SaveData.CreateDefault();
            var football = Array.Find(expected.subjects, record => record.id == SubjectId.Football);
            Assert.That(football, Is.Not.Null);
            football.passed = true;
            football.bestScore = 8f;
            football.bestRank = Rank.A;
            football.failedVisits = 2;

            saveSystem.Save(expected);
            var actual = saveSystem.Load();
            var restored = Array.Find(actual.subjects, record => record.id == SubjectId.Football);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.passed, Is.True);
            Assert.That(restored.bestScore, Is.EqualTo(8f));
            Assert.That(restored.bestRank, Is.EqualTo(Rank.A));
            Assert.That(restored.failedVisits, Is.EqualTo(2));
        }

        [Test]
        public void Migrate_CurrentVersion_ReturnsTheSameDataWithoutChanges()
        {
            var data = SaveData.CreateDefault();
            data.lives = 1;
            data.tutorialSeen[1] = true;

            SaveData migrated = saveSystem.Migrate(data);

            Assert.That(migrated, Is.SameAs(data));
            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.lives, Is.EqualTo(1));
            Assert.That(migrated.tutorialSeen[1], Is.True);
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
            versionOne.tutorialSeen = new[] { false, false, false, false, false, false, true };
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
            current.activeSubject = SubjectId.Football;
            current.visitAttempt = 2;
            current.awaitingPunishment = true;

            saveSystem.Save(current);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(3));
            Assert.That(actual.hasActiveSubject, Is.True);
            Assert.That(actual.activeSubject, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.visitAttempt, Is.EqualTo(2));
            Assert.That(actual.awaitingPunishment, Is.True);
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[1].id, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.subjects[1].bestScore, Is.EqualTo(6f));
            Assert.That(actual.subjects[2].id, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(actual.subjects[2].passed, Is.False);
            Assert.That(actual.tutorialSeen, Is.EqualTo(new[] { false, true, false }));

            var restored = new GameSession();
            restored.Restore(actual);
            Assert.That(restored.ResumeRoute(), Is.EqualTo(SessionRoute.Subject));
            Assert.That(restored.ActiveSubject, Is.EqualTo(SubjectId.Football));
        }

        [Test]
        public void Load_VersionFourSave_DropsBadmintonAndKeepsRemainingProgress()
        {
            var versionFour = SaveData.CreateDefault();
            versionFour.version = 4;
            versionFour.subjects = new[]
            {
                new SubjectRecordData { id = (SubjectId)0, passed = true, bestScore = 9f, bestRank = Rank.S },
                new SubjectRecordData { id = (SubjectId)5, passed = true, bestScore = 7f, bestRank = Rank.B },
                new SubjectRecordData { id = (SubjectId)6, failedVisits = 2 }
            };
            versionFour.tutorialSeen = new[] { false, true, true };
            versionFour.lives = 2;
            versionFour.hasActiveSubject = true;
            versionFour.activeSubject = (SubjectId)5;
            versionFour.visitAttempt = 2;
            versionFour.awaitingPunishment = true;

            WriteRawSave(versionFour);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(2));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(9f));
            Assert.That(actual.subjects[1].id, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.subjects[1].failedVisits, Is.EqualTo(2));
            Assert.That(actual.tutorialSeen, Is.EqualTo(new[] { false, true, false }));
            Assert.That(actual.hasActiveSubject, Is.False);
            Assert.That(actual.visitAttempt, Is.EqualTo(1));
            Assert.That(actual.awaitingPunishment, Is.False);
        }

        [Test]
        public void Load_VersionFiveSave_KeepsProgressAndAddsAFreshVolleyballRecord()
        {
            var versionFive = SaveData.CreateDefault();
            versionFive.version = 5;
            versionFive.subjects = new[]
            {
                new SubjectRecordData { id = SubjectId.Sprint, passed = true, bestScore = 8.5f, bestRank = Rank.A },
                new SubjectRecordData { id = SubjectId.Football, failedVisits = 1 }
            };
            versionFive.tutorialSeen = new[] { true, false };
            versionFive.lives = 4;
            versionFive.hasActiveSubject = true;
            versionFive.activeSubject = SubjectId.Sprint;
            versionFive.visitAttempt = 1;

            WriteRawSave(versionFive);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(6));
            Assert.That(actual.lives, Is.EqualTo(4));
            Assert.That(actual.subjects, Has.Length.EqualTo(3));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(8.5f));
            Assert.That(actual.subjects[1].id, Is.EqualTo(SubjectId.Football));
            Assert.That(actual.subjects[1].failedVisits, Is.EqualTo(1));
            Assert.That(actual.subjects[2].id, Is.EqualTo(SubjectId.Volleyball));
            Assert.That(actual.subjects[2].passed, Is.False);
            Assert.That(actual.tutorialSeen, Is.EqualTo(new[] { true, false, false }));
            Assert.That(actual.hasActiveSubject, Is.True);
            Assert.That(actual.activeSubject, Is.EqualTo(SubjectId.Sprint));
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
            invalidData.subjects[1].id = SubjectId.Sprint;

            WriteRawSave(invalidData);

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_CurrentVersionWithNullSubjectRecord_ReturnsDefaultData()
        {
            var invalidData = SaveData.CreateDefault();
            invalidData.subjects[1] = null;

            WriteRawSave(invalidData);

            AssertDefaultData(saveSystem.Load());
        }

        [Test]
        public void Load_CurrentVersionWithUndefinedSubjectId_ReturnsDefaultData()
        {
            var invalidData = SaveData.CreateDefault();
            invalidData.subjects[1].id = (SubjectId)999;

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
            existing.tutorialSeen[1] = true;
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
            Assert.That(actual.tutorialSeen[1], Is.True);
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
            Assert.That(data.tutorialSeen, Has.Length.EqualTo(subjectIds.Length));
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
