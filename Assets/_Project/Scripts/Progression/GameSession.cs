using System;
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay
{
    public enum SessionRoute
    {
        Subject,
        Punishment,
        RetrySubject,
        Map,
        GameOver,
        Boss
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

        const int FinalVisit = 2;

        readonly Dictionary<SubjectId, SubjectRecord> records =
            new Dictionary<SubjectId, SubjectRecord>();
        SubjectId? active;
        int visitAttempt = FirstVisit;
        bool awaitingPunishment;

        public GameSession()
        {
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                records.Add(id, new SubjectRecord());
            }
        }

        public int Lives { get; private set; } = MaxLives;
        public IReadOnlyDictionary<SubjectId, SubjectRecord> Records => records;
        public bool BossUnlocked => records.Values.All(record => record.Passed);
        public SubjectId? PendingPunishmentSubject => awaitingPunishment && active.HasValue ? active : (SubjectId?)null;
        public SubjectId? ActiveSubject => active;
        public int VisitAttempt => visitAttempt;
        public bool AwaitingPunishment => awaitingPunishment;

        public SubjectRecord GetRecord(SubjectId id) => records[id];

        public SessionRoute ResumeRoute()
        {
            if (!active.HasValue)
                return SessionRoute.Map;
            if (awaitingPunishment)
                return SessionRoute.Punishment;
            return visitAttempt == FirstVisit ? SessionRoute.Subject : SessionRoute.RetrySubject;
        }

        public void ResetCampaign()
        {
            Lives = MaxLives;
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
                records[id] = new SubjectRecord();
            ClearActiveSubject();
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
            data.bossUnlocked = BossUnlocked;
            data.hasActiveSubject = active.HasValue;
            data.activeSubject = active ?? default;
            data.visitAttempt = visitAttempt;
            data.awaitingPunishment = awaitingPunishment;

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
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Lives = Math.Max(0, Math.Min(MaxLives, data.lives));
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                SubjectRecordData recordData = FindRecordData(data.subjects, id);
                records[id] = recordData == null ? new SubjectRecord() : SubjectRecord.FromData(recordData);
            }

            RestoreActiveAttempt(data);
        }

        void RestoreActiveAttempt(SaveData data)
        {
            ClearActiveSubject();

            if (!data.hasActiveSubject || Lives <= 0)
                return;
            if (!Enum.IsDefined(typeof(SubjectId), data.activeSubject))
                return;
            if (data.visitAttempt != FirstVisit && data.visitAttempt != FinalVisit)
                return;
            if (data.awaitingPunishment && data.visitAttempt != FinalVisit)
                return;

            active = data.activeSubject;
            visitAttempt = data.visitAttempt;
            awaitingPunishment = data.awaitingPunishment;
        }

        public SessionRoute StartSubject(SubjectId id)
        {
            if (active.HasValue)
            {
                throw new InvalidOperationException("A subject attempt is already active.");
            }

            if (Lives <= 0)
            {
                return SessionRoute.GameOver;
            }

            active = id;
            visitAttempt = FirstVisit;
            awaitingPunishment = false;
            return SessionRoute.Subject;
        }

        public SessionRoute CompletePunishment()
        {
            if (!awaitingPunishment || !active.HasValue)
            {
                throw new InvalidOperationException("No punishment is active.");
            }

            awaitingPunishment = false;
            return SessionRoute.RetrySubject;
        }

        public SubjectId AbandonActiveSubject()
        {
            if (!active.HasValue)
                throw new InvalidOperationException("No subject attempt is active.");
            var subject = active.Value;
            ClearActiveSubject();
            return subject;
        }

        public SessionRoute SubmitResult(SubjectId id, MinigameResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            RequireActive(id);

            SessionRoute route = RouteForResult(result);

            if (result.Pass)
            {
                records[id].Accept(result);
                ClearActiveSubject();
                return route;
            }

            if (visitAttempt == FirstVisit)
            {
                visitAttempt = FinalVisit;
                awaitingPunishment = true;
                return route;
            }

            Lives--;
            records[id].RecordFailedVisit();
            ClearActiveSubject();
            return route;
        }

        SessionRoute RouteForResult(MinigameResult result) => result.Pass
            ? SessionRoute.Map
            : visitAttempt == FirstVisit
                ? SessionRoute.Punishment
                : Lives <= 1 ? SessionRoute.GameOver : SessionRoute.Map;

        void RequireActive(SubjectId id)
        {
            if (!active.HasValue || active.Value != id)
            {
                throw new InvalidOperationException($"Subject {id} is not active.");
            }

            if (awaitingPunishment)
            {
                throw new InvalidOperationException("Complete punishment before submitting attempt two.");
            }
        }

        void ClearActiveSubject()
        {
            active = null;
            visitAttempt = FirstVisit;
            awaitingPunishment = false;
        }

        static SubjectRecordData FindRecordData(SubjectRecordData[] subjectData, SubjectId id)
        {
            if (subjectData == null)
            {
                return null;
            }

            foreach (SubjectRecordData data in subjectData)
            {
                if (data != null && data.id == id)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
