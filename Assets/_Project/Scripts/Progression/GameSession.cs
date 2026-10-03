using System;
using System.Collections.Generic;

namespace KMA.Gameplay
{
    public enum SessionRoute
    {
        Subject,
        // Retained for old serialized route identifiers; the supplementary route uses Map.
        Punishment,
        RetrySubject,
        Map,
        GameOver
    }

    public interface IResultPreviewPanel
    {
        event Action<string> ActionRequested;
        void Show(MinigameResult result, string previewRoute);
    }

    public sealed class GameSession
    {
        public const int MaxLives = 5;
        public const int FirstVisit = 1;

        readonly Dictionary<SubjectId, SubjectRecord> records = new Dictionary<SubjectId, SubjectRecord>();
        readonly ChallengeCatalog catalog;
        SubjectId? active;
        ChallengeAttemptContext activeChallenge;

        public GameSession(ChallengeCatalog catalog = null)
        {
            this.catalog = catalog == null ? ChallengeCatalog.LoadDefault() : catalog;
            Journey = new JourneyProgress(this.catalog);
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
                records.Add(id, new SubjectRecord());
        }

        public event Action JourneyChanged;

        public int Lives => Journey.AttemptsRemaining;
        public IReadOnlyDictionary<SubjectId, SubjectRecord> Records => records;
        public JourneyProgress Journey { get; private set; }
        public SubjectId? PendingPunishmentSubject => Journey.AwaitingSupplementary
            ? (SubjectId?)catalog.Get(Journey.CheckpointChallengeId).Subject
            : null;
        public SubjectId? ActiveSubject => active;
        public int VisitAttempt => FirstVisit;
        public bool AwaitingPunishment => Journey.AwaitingSupplementary;

        public SubjectRecord GetRecord(SubjectId id) => records[id];

        public bool TryStartChallenge(string id, ChallengeAttemptMode mode,
            ChallengeDifficulty difficulty, out ChallengeAttemptContext context)
        {
            context = null;
            if (active.HasValue || !Journey.TryBegin(id, mode, difficulty, out context))
                return false;
            activeChallenge = context;
            active = catalog.Get(context.ChallengeId).Subject;
            JourneyChanged?.Invoke();
            return true;
        }

        public JourneyCommitOutcome SubmitChallengeResult(ChallengeAttemptResult result, bool notify = true)
        {
            JourneyCommitOutcome outcome = Journey.Apply(result);
            if (!outcome.Accepted)
                return outcome;

            ChallengeDefinition definition = catalog.Get(result.Context.ChallengeId);
            if (definition.Kind == ChallengeKind.Exam && result.ExamResult != null &&
                result.Context.Difficulty == ChallengeDifficulty.Normal)
            {
                if (result.Pass && result.ExamResult.Pass)
                    records[definition.Subject].Accept(result.ExamResult);
                else
                    records[definition.Subject].RecordFailedVisit();
            }

            active = null;
            activeChallenge = null;
            if (notify)
                NotifyJourneyChanged();
            return outcome;
        }

        public void AbandonActiveChallenge()
        {
            if (activeChallenge == null)
                return;
            Journey.AbandonAttempt();
            activeChallenge = null;
            active = null;
            NotifyJourneyChanged();
        }

        public void NotifyJourneyChanged() => JourneyChanged?.Invoke();

        public SessionRoute ResumeRoute()
        {
            if (active.HasValue)
                return SessionRoute.Subject;
            return Journey.AttemptsRemaining == 0 && !Journey.AwaitingSupplementary
                ? SessionRoute.GameOver
                : SessionRoute.Map;
        }

        public void ResetCampaign()
        {
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
                records[id] = new SubjectRecord();
            Journey = new JourneyProgress(catalog);
            active = null;
            activeChallenge = null;
            NotifyJourneyChanged();
        }

        public SessionRoute PreviewRoute(SubjectId id, MinigameResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            RequireActive(id);
            return RouteForResult(result);
        }

        public SaveData ToSaveData()
        {
            var data = SaveData.CreateDefault();
            data.lives = Lives;
            data.hasActiveSubject = active.HasValue;
            data.activeSubject = active ?? default;
            data.visitAttempt = FirstVisit;
            data.awaitingPunishment = false;
            data.journey = Journey.ToData();

            int index = 0;
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                SubjectRecord record = records[id];
                data.subjects[index++] = new SubjectRecordData
                {
                    id = id,
                    passed = record.Passed,
                    bestScore = record.BestScore,
                    bestRank = record.BestRank,
                    failedVisits = record.FailedVisits
                };
            }

            return data;
        }

        public void Restore(SaveData data)
        {
            RestoreCore(data, true);
        }

        public void RestoreSnapshot(SaveData snapshot)
        {
            RestoreCore(snapshot, false);
        }

        void RestoreCore(SaveData data, bool normalize)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SaveData normalized = normalize ? JourneySaveMigration.Normalize(data, catalog) : data;
            int lives = normalized.lives;
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                SubjectRecordData recordData = FindRecordData(normalized.subjects, id);
                records[id] = recordData == null ? new SubjectRecord() : SubjectRecord.FromData(recordData);
            }

            Journey = new JourneyProgress(catalog);
            Journey.Restore(normalized.journey, lives);
            activeChallenge = Journey.ActiveAttempt;
            active = activeChallenge == null ? null : (SubjectId?)catalog.Get(activeChallenge.ChallengeId).Subject;
        }

        public SessionRoute StartSubject(SubjectId id)
        {
            if (active.HasValue)
                throw new InvalidOperationException("A challenge attempt is already active.");
            if (Lives <= 0 && !Journey.AwaitingSupplementary)
                return SessionRoute.GameOver;

            string challengeId = Journey.CheckpointChallengeId;
            ChallengeAttemptMode mode;
            if (string.IsNullOrEmpty(challengeId))
            {
                if (!Journey.CourseComplete)
                    return SessionRoute.GameOver;
                challengeId = ExamId(id);
                mode = ChallengeAttemptMode.FreePlay;
            }
            else
            {
                ChallengeDefinition current = catalog.Get(challengeId);
                if (current.Subject != id)
                    throw new InvalidOperationException($"Subject {id} is locked by the course order.");
                mode = Journey.AwaitingSupplementary
                    ? ChallengeAttemptMode.Supplementary
                    : ChallengeAttemptMode.Journey;
            }

            ChallengeDefinition definition = catalog.Get(challengeId);
            return TryStartChallenge(challengeId, mode, definition.Difficulty, out _)
                ? SessionRoute.Subject
                : Lives <= 0 ? SessionRoute.GameOver : SessionRoute.Map;
        }

        // Kept for old callers. The new course has no punishment-only scene.
        public SessionRoute CompletePunishment() => throw new InvalidOperationException(
            "Supplementary exams are resumed from their required practice on the course map.");

        public SubjectId AbandonActiveSubject()
        {
            if (!active.HasValue)
                throw new InvalidOperationException("No subject attempt is active.");
            SubjectId subject = active.Value;
            AbandonActiveChallenge();
            return subject;
        }

        public SessionRoute SubmitResult(SubjectId id, MinigameResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            RequireActive(id);
            ChallengeDefinition definition = catalog.Get(activeChallenge.ChallengeId);
            var attemptResult = new ChallengeAttemptResult(activeChallenge, result.Pass,
                new ChallengeMetrics(completedTargets: definition.TargetCount),
                definition.Kind == ChallengeKind.Exam ? result : null);
            JourneyCommitOutcome outcome = SubmitChallengeResult(attemptResult);
            if (!outcome.Accepted)
                throw new InvalidOperationException("The challenge result did not match its active attempt.");
            return RouteForResult(result);
        }

        SessionRoute RouteForResult(MinigameResult result) =>
            !result.Pass && Lives == 0 && !Journey.AwaitingSupplementary
                ? SessionRoute.GameOver
                : SessionRoute.Map;

        void RequireActive(SubjectId id)
        {
            if (!active.HasValue || active.Value != id || activeChallenge == null)
                throw new InvalidOperationException($"Subject {id} is not active.");
        }

        string ExamId(SubjectId subject) => subject switch
        {
            SubjectId.Sprint => "sprint_exam",
            SubjectId.Volleyball => "volleyball_exam",
            SubjectId.Football => "soccer_exam",
            _ => throw new ArgumentOutOfRangeException(nameof(subject))
        };

        static SubjectRecordData FindRecordData(SubjectRecordData[] subjectData, SubjectId id)
        {
            if (subjectData == null)
                return null;
            foreach (SubjectRecordData data in subjectData)
            {
                if (data != null && data.id == id)
                    return data;
            }
            return null;
        }
    }
}
