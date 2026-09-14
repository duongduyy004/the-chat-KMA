using System;
using System.Collections.Generic;
using System.Linq;

namespace KMA.Gameplay
{
    public enum SessionRoute
    {
        Subject,
        // Retired in place: the punishment leg is unreachable, and nothing emits this route.
        Punishment,
        // Retired in place: the punishment leg is unreachable, and nothing emits this route.
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
        // Held at FirstVisit / false by every remaining assignment now that the punishment
        // leg is retired; nothing in the repo ever sets visitAttempt to FinalVisit or
        // awaitingPunishment to true. Kept only to hold the SaveData format stable. See
        // docs/superpowers/specs/2026-09-14-remove-punishment-loss-route-design.md.
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
            // Unreachable while punishment is retired: awaitingPunishment is never true.
            if (awaitingPunishment)
                return SessionRoute.Punishment;
            // The RetrySubject half is unreachable while punishment is retired: visitAttempt
            // is never FinalVisit.
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

            // The punishment leg is no longer routable. A save written before it was removed
            // can carry awaitingPunishment and FinalVisit; restoring those verbatim would send
            // the player to a scene nothing can complete. Resume the subject attempt instead.
            // The two guards above still stand so a malformed save keeps falling back to no
            // active attempt.
            active = data.activeSubject;
            visitAttempt = FirstVisit;
            awaitingPunishment = false;
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

        // Unreachable while punishment is retired: awaitingPunishment is never true, so this
        // always throws.
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

            Lives--;
            records[id].RecordFailedVisit();
            ClearActiveSubject();
            return route;
        }

        // Lives is read before SubmitResult decrements it, so "Lives <= 1" means
        // "this loss empties the last life".
        SessionRoute RouteForResult(MinigameResult result) => result.Pass
            ? SessionRoute.Map
            : Lives <= 1 ? SessionRoute.GameOver : SessionRoute.Map;

        void RequireActive(SubjectId id)
        {
            if (!active.HasValue || active.Value != id)
            {
                throw new InvalidOperationException($"Subject {id} is not active.");
            }

            // Unreachable while punishment is retired: awaitingPunishment is never true.
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
