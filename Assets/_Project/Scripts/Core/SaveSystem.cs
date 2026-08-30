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
                if (!IsLoadable(data))
                {
                    return SaveData.CreateDefault();
                }

                return Migrate(data);
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
                return data;
            }

            SaveData defaults = SaveData.CreateDefault();
            var migrated = new SaveData
            {
                version = SaveData.CurrentVersion,
                lives = data.lives,
                subjects = MigrateSubjects(data.subjects, defaults.subjects),
                bossUnlocked = data.bossUnlocked,
                gameCompleted = data.gameCompleted,
                tutorialSeen = data.tutorialSeen ?? defaults.tutorialSeen,
                settings = data.settings ?? defaults.settings
            };

            return migrated;
        }

        private static bool IsLoadable(SaveData data)
        {
            if (data == null || data.version < 0 || data.version > SaveData.CurrentVersion)
            {
                return false;
            }

            if (data.version < SaveData.CurrentVersion)
            {
                return data.subjects != null;
            }

            return data.subjects != null &&
                   data.subjects.Length == Enum.GetValues(typeof(SubjectId)).Length &&
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
    }
}
