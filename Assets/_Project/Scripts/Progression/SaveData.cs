using System;

namespace KMA.Gameplay
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version;
        public int lives;
        public SubjectRecordData[] subjects;
        public bool bossUnlocked;
        public bool gameCompleted;
        public bool[] tutorialSeen;
        public Settings settings;

        public static SaveData CreateDefault()
        {
            Array subjectValues = Enum.GetValues(typeof(SubjectId));
            var subjects = new SubjectRecordData[subjectValues.Length];
            for (int i = 0; i < subjectValues.Length; i++)
            {
                subjects[i] = new SubjectRecordData
                {
                    id = (SubjectId)subjectValues.GetValue(i),
                    passed = false,
                    bestScore = 0f,
                    bestRank = Rank.F,
                    failedVisits = 0
                };
            }

            return new SaveData
            {
                version = CurrentVersion,
                lives = 5,
                subjects = subjects,
                bossUnlocked = false,
                gameCompleted = false,
                tutorialSeen = new bool[subjectValues.Length],
                settings = Settings.CreateDefault()
            };
        }
    }

    [Serializable]
    public sealed class SubjectRecordData
    {
        public SubjectId id;
        public bool passed;
        public float bestScore;
        public Rank bestRank;
        public int failedVisits;
    }

    [Serializable]
    public sealed class Settings
    {
        public float musicVol;
        public float sfxVol;
        public bool vibration;
        public float rhythmOffsetMs;

        public static Settings CreateDefault() => new Settings
        {
            musicVol = 1f,
            sfxVol = 1f,
            vibration = true,
            rhythmOffsetMs = 0f
        };
    }
}
