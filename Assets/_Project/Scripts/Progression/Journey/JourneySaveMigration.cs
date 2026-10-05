using System;
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay
{
    public static class JourneySaveMigration
    {
        static readonly SubjectId[] LegacyCourseSubjects =
        {
            SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football
        };

        static readonly SubjectId[] CourseSubjects =
        {
            SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess
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
                foreach (SubjectId subject in LegacyCourseSubjects)
                {
                    if (!FindSubject(result.subjects, subject).passed)
                        break;
                    passedPrefix++;
                }
            }

            result.journey = new JourneyStateData();
            for (int i = 0; i < passedPrefix * 3; i++)
                result.journey.completedChallengeIds.Add(catalog.Ordered[i].Id);

            for (int i = 0; i < LegacyCourseSubjects.Length; i++)
                FindSubject(result.subjects, LegacyCourseSubjects[i]).passed = i < passedPrefix;

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
            if (data.version < 7)
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

                journey.chessBest = source.chessBest?.Copy() ?? new JourneyChessRecordData();
                journey.celebrationSeen = source.celebrationSeen;

                journey.failCounts = NormalizeFailCounts(source.failCounts, catalog);
                journey.lastAppliedFrogJumpId = source.lastAppliedFrogJumpId;
                FrogJumpPending pending = source.pendingFrogJump?.ToPending();
                string checkpoint = journey.completedChallengeIds.Count < catalog.Ordered.Count
                    ? catalog.Ordered[journey.completedChallengeIds.Count].Id : null;
                journey.pendingFrogJump = pending != null && checkpoint != null &&
                    pending.FailedChallengeId == checkpoint &&
                    JourneyProgress.IsPenalizedKind(catalog.Get(checkpoint).Kind)
                        ? JourneyFrogJumpData.FromPending(pending) : null;

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
                nextLifeAtUtcTicks = Math.Max(0L, source.nextLifeAtUtcTicks),
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
                string examId = catalog.Ordered.First(x => x.Subject == subject && ChallengeDefinition.IsScored(x.Kind)).Id;
                FindSubject(records, subject).passed = completed.Contains(examId);
            }
        }

        static SubjectRecordData FindSubject(SubjectRecordData[] records, SubjectId subject) =>
            records.FirstOrDefault(x => x != null && x.id == subject) ?? new SubjectRecordData { id = subject };

        static List<JourneyFailCountData> NormalizeFailCounts(List<JourneyFailCountData> source, ChallengeCatalog catalog)
        {
            var result = new List<JourneyFailCountData>();
            if (source == null)
                return result;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JourneyFailCountData entry in source)
            {
                if (entry == null || entry.count <= 0 || !seen.Add(entry.challengeId ?? string.Empty) ||
                    !catalog.Ordered.Any(x => x.Id == entry.challengeId && JourneyProgress.IsPenalizedKind(x.Kind)))
                    continue;
                result.Add(new JourneyFailCountData { challengeId = entry.challengeId, count = entry.count });
            }
            return result;
        }
    }
}
