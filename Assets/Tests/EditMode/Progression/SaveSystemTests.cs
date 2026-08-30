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
            expected.bossUnlocked = true;
            expected.gameCompleted = true;
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
            Assert.That(actual.bossUnlocked, Is.True);
            Assert.That(actual.gameCompleted, Is.True);
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
            data.tutorialSeen[3] = true;

            SaveData migrated = saveSystem.Migrate(data);

            Assert.That(migrated, Is.SameAs(data));
            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.lives, Is.EqualTo(1));
            Assert.That(migrated.tutorialSeen[3], Is.True);
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
                bossUnlocked = true,
                gameCompleted = true
            };

            SaveData migrated = saveSystem.Migrate(olderData);

            Assert.That(migrated.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.lives, Is.EqualTo(3));
            Assert.That(migrated.subjects, Has.Length.EqualTo(7));
            Assert.That(migrated.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(migrated.subjects[0].passed, Is.True);
            Assert.That(migrated.subjects[0].bestScore, Is.EqualTo(71f));
            Assert.That(migrated.subjects[0].bestRank, Is.EqualTo(Rank.B));
            Assert.That(migrated.subjects[0].failedVisits, Is.EqualTo(2));
            Assert.That(migrated.tutorialSeen, Has.Length.EqualTo(7));
            Assert.That(migrated.tutorialSeen, Is.All.False);
            Assert.That(migrated.settings.musicVol, Is.EqualTo(1f));
            Assert.That(migrated.settings.sfxVol, Is.EqualTo(1f));
            Assert.That(migrated.settings.vibration, Is.True);
            Assert.That(migrated.settings.rhythmOffsetMs, Is.EqualTo(0f));
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
            invalidData.subjects[6].id = SubjectId.Sprint;

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
                bossUnlocked = true,
                gameCompleted = true,
                tutorialSeen = new[] { true, false },
                settings = null
            };

            WriteRawSave(legacyData);
            SaveData actual = saveSystem.Load();

            Assert.That(actual.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(actual.lives, Is.EqualTo(3));
            Assert.That(actual.subjects, Has.Length.EqualTo(7));
            Assert.That(actual.subjects[0].id, Is.EqualTo(SubjectId.Sprint));
            Assert.That(actual.subjects[0].passed, Is.False);
            Assert.That(actual.bossUnlocked, Is.True);
            Assert.That(actual.gameCompleted, Is.True);
            Assert.That(actual.tutorialSeen, Has.Length.EqualTo(7));
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
            existing.bossUnlocked = true;
            existing.gameCompleted = true;
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
            Assert.That(actual.subjects, Has.Length.EqualTo(7));
            Assert.That(actual.subjects[0].passed, Is.False);
            Assert.That(actual.subjects[0].bestScore, Is.EqualTo(0f));
            Assert.That(actual.bossUnlocked, Is.False);
            Assert.That(actual.gameCompleted, Is.False);
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
            Assert.That(data.bossUnlocked, Is.False);
            Assert.That(data.gameCompleted, Is.False);
            Assert.That(data.tutorialSeen, Has.Length.EqualTo(7));
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
    }
}
