using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("KMA.Gameplay.Progression.EditMode.Tests")]

namespace KMA.Gameplay
{
    public sealed class SaveSystem
    {
        private const string SaveFileName = "save.json";
        private const string TemporaryFileName = "save.tmp";
        private const int MissingLegacyInteger = int.MinValue;

        private readonly string saveDirectory;
        private readonly string temporaryPath;

        public SaveSystem()
            : this(() => Application.persistentDataPath)
        {
        }

        internal SaveSystem(Func<string> persistentDataPathProvider)
        {
            if (persistentDataPathProvider == null)
            {
                throw new ArgumentNullException(nameof(persistentDataPathProvider));
            }

            saveDirectory = GetValidatedDirectory(persistentDataPathProvider());
            SavePath = GetValidatedFilePath(saveDirectory, SaveFileName);
            temporaryPath = GetValidatedFilePath(saveDirectory, TemporaryFileName);
        }

        public string SavePath { get; }

        public bool HasSave => File.Exists(SavePath);

        public SaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                return SaveData.CreateDefault();
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return SaveData.CreateDefault();
                }

                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data != null && data.version >= 0 && data.version < SaveData.CurrentVersion)
                {
                    LegacyData legacy = ParseLegacy(json);
                    if (!legacy.HasRequiredFields(data.version))
                    {
                        return SaveData.CreateDefault();
                    }

                    if (data.version == 0)
                    {
                        data = legacy.ToSaveData();
                    }
                }

                if (!IsLoadable(data))
                {
                    return SaveData.CreateDefault();
                }

                SaveData migrated = Migrate(data);
                return IsCurrentVersionStructureValid(migrated) ? migrated : SaveData.CreateDefault();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                return SaveData.CreateDefault();
            }
        }

        public void Save(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Directory.CreateDirectory(saveDirectory);
            string json = JsonUtility.ToJson(data, true);

            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(true);
            }

            if (File.Exists(SavePath))
            {
                File.Replace(temporaryPath, SavePath, null);
            }
            else
            {
                File.Move(temporaryPath, SavePath);
            }
        }

        public void DeleteSave()
        {
            DeleteIfPresent(SavePath);
            DeleteIfPresent(temporaryPath);
        }

        public SaveData Migrate(SaveData data)
        {
            if (data == null || data.version < 0 || data.version > SaveData.CurrentVersion)
            {
                return SaveData.CreateDefault();
            }

            if (data.version == SaveData.CurrentVersion)
            {
                return IsCurrentVersionStructureValid(data) ? data : SaveData.CreateDefault();
            }

            SaveData defaults = SaveData.CreateDefault();
            var migrated = new SaveData
            {
                version = SaveData.CurrentVersion,
                lives = data.lives,
                subjects = MigrateSubjects(data.subjects, defaults.subjects),
                bossUnlocked = data.bossUnlocked,
                gameCompleted = data.gameCompleted,
                hasActiveSubject = defaults.hasActiveSubject,
                activeSubject = defaults.activeSubject,
                visitAttempt = defaults.visitAttempt,
                awaitingPunishment = defaults.awaitingPunishment,
                tutorialSeen = MigrateTutorialSeen(data.tutorialSeen, defaults.tutorialSeen.Length),
                settings = data.settings ?? defaults.settings
            };

            return IsCurrentVersionStructureValid(migrated) ? migrated : defaults;
        }

        private static LegacyData ParseLegacy(string json)
        {
            var legacy = new LegacyData();
            JsonUtility.FromJsonOverwrite(json, legacy);
            return legacy;
        }

        private static bool IsLoadable(SaveData data)
        {
            if (data == null || data.version < 0 || data.version > SaveData.CurrentVersion)
            {
                return false;
            }

            if (data.version < SaveData.CurrentVersion)
            {
                return true;
            }

            return IsCurrentVersionStructureValid(data);
        }

        private static bool IsCurrentVersionStructureValid(SaveData data)
        {
            return HasCompleteSubjectCoverage(data.subjects) &&
                   data.tutorialSeen != null &&
                   data.tutorialSeen.Length == Enum.GetValues(typeof(SubjectId)).Length &&
                   data.settings != null;
        }

        private static SubjectRecordData[] MigrateSubjects(SubjectRecordData[] subjects, SubjectRecordData[] defaults)
        {
            if (subjects == null)
            {
                return defaults;
            }

            var migrated = new SubjectRecordData[defaults.Length];
            for (int i = 0; i < defaults.Length; i++)
            {
                SubjectRecordData source = FindSubject(subjects, defaults[i].id);
                migrated[i] = source ?? defaults[i];
            }

            return migrated;
        }

        private static bool[] MigrateTutorialSeen(bool[] tutorialSeen, int expectedLength)
        {
            var migrated = new bool[expectedLength];
            if (tutorialSeen != null)
            {
                Array.Copy(tutorialSeen, migrated, Math.Min(tutorialSeen.Length, migrated.Length));
            }

            return migrated;
        }

        private static bool HasCompleteSubjectCoverage(SubjectRecordData[] subjects)
        {
            SubjectId[] subjectIds = (SubjectId[])Enum.GetValues(typeof(SubjectId));
            if (subjects == null || subjects.Length != subjectIds.Length)
            {
                return false;
            }

            var seen = new bool[subjectIds.Length];
            for (int i = 0; i < subjects.Length; i++)
            {
                if (subjects[i] == null)
                {
                    return false;
                }

                int index = Array.IndexOf(subjectIds, subjects[i].id);
                if (index < 0 || seen[index])
                {
                    return false;
                }

                seen[index] = true;
            }

            return true;
        }

        private static SubjectRecordData FindSubject(SubjectRecordData[] subjects, SubjectId id)
        {
            for (int i = 0; i < subjects.Length; i++)
            {
                if (subjects[i] != null && subjects[i].id == id)
                {
                    return subjects[i];
                }
            }

            return null;
        }

        private static string GetValidatedDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("The save directory must be provided.", nameof(directory));
            }

            return Path.GetFullPath(directory);
        }

        private static string GetValidatedFilePath(string directory, string fileName)
        {
            string path = Path.GetFullPath(Path.Combine(directory, fileName));
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.Ordinal) ||
                !string.Equals(Path.GetFileName(path), fileName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The save path is outside the configured save directory.");
            }

            return path;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        [Serializable]
        private sealed class LegacyData
        {
            public int version = MissingLegacyInteger;
            public int lives = MissingLegacyInteger;
            public SubjectRecordData[] subjects;
            public bool bossUnlocked;
            public bool gameCompleted;
            public bool[] tutorialSeen;

            public bool HasRequiredFields(int expectedVersion) =>
                version == expectedVersion && lives != MissingLegacyInteger;

            public SaveData ToSaveData() => new SaveData
            {
                version = version,
                lives = lives,
                subjects = subjects,
                bossUnlocked = bossUnlocked,
                gameCompleted = gameCompleted,
                tutorialSeen = tutorialSeen,
                settings = null
            };
        }
    }
}
