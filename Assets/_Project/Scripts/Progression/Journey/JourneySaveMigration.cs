using System;
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay
{
    public static class JourneySaveMigration
    {
        static readonly SubjectId[] CourseSubjects =
        {
            SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football
        };

        public static SaveData MigrateLegacy(SaveData source, ChallengeCatalog catalog)
        {
            if (source == null)
                return SaveData.CreateDefault();
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            SaveData result = CopySave(source, catalog);
            int passedPrefix = 0;
            if (!result.settingsOnly)
            {
                foreach (SubjectId subject in CourseSubjects)
                {
                    if (!FindSubject(result.subjects, subject).passed)
                        break;
                    passedPrefix++;
                }
            }

            result.journey = new JourneyStateData();
            for (int i = 0; i < passedPrefix * 3; i++)
                result.journey.completedChallengeIds.Add(catalog.Ordered[i].Id);

            for (int i = 0; i < CourseSubjects.Length; i++)
                FindSubject(result.subjects, CourseSubjects[i]).passed = i < passedPrefix;

            if (!result.settingsOnly && result.lives == 0 && passedPrefix < CourseSubjects.Length)
            {
                ChallengeDefinition next = catalog.Ordered[passedPrefix * 3];
                result.journey.awaitingSupplementaryChallengeId = PracticeId(next.Subject);
            }

            result.version = SaveData.CurrentVersion;
            result.hasActiveSubject = false;
            result.activeSubject = default;
            result.visitAttempt = GameSession.FirstVisit;
            result.awaitingPunishment = false;
            return Normalize(result, catalog);
        }

        public static SaveData Normalize(SaveData data, ChallengeCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (data == null || data.version < 0 || data.version > SaveData.CurrentVersion)
                return SaveData.CreateDefault();
            if (data.version < SaveData.CurrentVersion)
                return MigrateLegacy(data, catalog);

            SaveData normalized = CopySave(data, catalog);
            normalized.version = SaveData.CurrentVersion;
            normalized.lives = Math.Max(0, Math.Min(GameSession.MaxLives, normalized.lives));
            normalized.hasActiveSubject = false;
            normalized.activeSubject = default;
            normalized.visitAttempt = GameSession.FirstVisit;
            normalized.awaitingPunishment = false;

            JourneyStateData source = data.journey;
            var journey = new JourneyStateData();
            if (!normalized.settingsOnly && source != null)
            {
                journey.completedChallengeIds = NormalizeCompletionPrefix(source.completedChallengeIds, catalog);
                journey.supplementaryRounds = Math.Max(0, source.supplementaryRounds);
                journey.seenDialogueIds = NormalizeDialogueIds(source.seenDialogueIds);
                journey.lastCommittedAttemptId = source.lastCommittedAttemptId;
                journey.lastCommittedResult = source.lastCommittedResult?.Copy();

                if (normalized.lives == 0 && journey.completedChallengeIds.Count < catalog.Ordered.Count)
                {
                    ChallengeDefinition firstIncomplete = catalog.Ordered[journey.completedChallengeIds.Count];
                    journey.awaitingSupplementaryChallengeId = PracticeId(firstIncomplete.Subject);
                }

                if (journey.lastCommittedResult != null &&
                    journey.lastCommittedResult.context != null &&
                    catalog.Ordered.All(x => x.Id != journey.lastCommittedResult.context.challengeId))
                {
                    journey.lastCommittedAttemptId = null;
                    journey.lastCommittedResult = null;
                }
            }

            journey.activeAttempt = null;
            normalized.journey = journey;
            SyncPassedFlags(normalized.subjects, journey.completedChallengeIds, catalog);
            return normalized;
        }

        static SaveData CopySave(SaveData source, ChallengeCatalog catalog)
        {
            SaveData defaults = SaveData.CreateDefault();
            var records = new SubjectRecordData[defaults.subjects.Length];
            for (int i = 0; i < records.Length; i++)
            {
                SubjectId subject = defaults.subjects[i].id;
                SubjectRecordData found = source.subjects?.FirstOrDefault(x => x != null && x.id == subject);
                SubjectRecordData basis = found ?? defaults.subjects[i];
                records[i] = new SubjectRecordData
                {
                    id = subject,
                    passed = basis.passed,
                    bestScore = basis.bestScore,
                    bestRank = basis.bestRank,
                    failedVisits = Math.Max(0, basis.failedVisits)
                };
            }

            bool[] tutorials = new bool[defaults.tutorialSeen.Length];
            if (source.tutorialSeen != null)
                Array.Copy(source.tutorialSeen, tutorials, Math.Min(source.tutorialSeen.Length, tutorials.Length));

            Settings settings = source.settings == null ? Settings.CreateDefault() : new Settings
            {
                musicVol = source.settings.musicVol,
                sfxVol = source.settings.sfxVol,
                vibration = source.settings.vibration,
                rhythmOffsetMs = source.settings.rhythmOffsetMs
            };

            return new SaveData
            {
                version = source.version,
                settingsOnly = source.settingsOnly,
                lives = source.lives,
                subjects = records,
                hasActiveSubject = source.hasActiveSubject,
                activeSubject = source.activeSubject,
                visitAttempt = source.visitAttempt,
                awaitingPunishment = source.awaitingPunishment,
                tutorialSeen = tutorials,
                settings = settings,
                journey = source.journey
            };
        }

        static List<string> NormalizeCompletionPrefix(List<string> source, ChallengeCatalog catalog)
        {
            var result = new List<string>();
            if (source == null)
                return result;

            foreach (string id in source)
            {
                if (result.Count >= catalog.Ordered.Count || string.IsNullOrWhiteSpace(id) ||
                    id != catalog.Ordered[result.Count].Id)
                {
                    break;
                }
                result.Add(id);
            }
            return result;
        }

        static List<string> NormalizeDialogueIds(List<string> source)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();
            if (source == null)
                return result;
            foreach (string id in source)
            {
                if (!string.IsNullOrWhiteSpace(id) && seen.Add(id))
                    result.Add(id);
            }
            return result;
        }

        static void SyncPassedFlags(SubjectRecordData[] records, List<string> completed, ChallengeCatalog catalog)
        {
            foreach (SubjectId subject in CourseSubjects)
            {
                string examId = catalog.Ordered.First(x => x.Subject == subject && x.Kind == ChallengeKind.Exam).Id;
                FindSubject(records, subject).passed = completed.Contains(examId);
            }
        }

        static SubjectRecordData FindSubject(SubjectRecordData[] records, SubjectId subject) =>
            records.FirstOrDefault(x => x != null && x.id == subject) ?? new SubjectRecordData { id = subject };

        static string PracticeId(SubjectId subject) => subject switch
        {
            SubjectId.Sprint => "sprint_practice",
            SubjectId.Volleyball => "volleyball_practice",
            SubjectId.Football => "soccer_practice",
            _ => null
        };
    }
}
