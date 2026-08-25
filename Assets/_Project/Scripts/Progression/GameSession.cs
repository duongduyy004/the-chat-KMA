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

    public sealed class GameSession
    {
        readonly Dictionary<SubjectId, SubjectRecord> records =
            new Dictionary<SubjectId, SubjectRecord>();
        SubjectId? active;
        int visitAttempt;
        bool awaitingPunishment;

        public GameSession()
        {
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                records.Add(id, new SubjectRecord());
            }
        }

        public int Lives { get; private set; } = 5;
        public IReadOnlyDictionary<SubjectId, SubjectRecord> Records => records;
        public bool BossUnlocked => records.Values.All(record => record.Passed);
        public SubjectId? PendingPunishmentSubject => awaitingPunishment && active.HasValue ? active : (SubjectId?)null;

        public SubjectRecord GetRecord(SubjectId id) => records[id];

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
            visitAttempt = 1;
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

        public SessionRoute SubmitResult(SubjectId id, MinigameResult result)
        {
            RequireActive(id);

            if (result.Pass)
            {
                records[id].Accept(result);
                ClearActiveSubject();
                return SessionRoute.Map;
            }

            if (visitAttempt == 1)
            {
                visitAttempt = 2;
                awaitingPunishment = true;
                return SessionRoute.Punishment;
            }

            Lives--;
            records[id].RecordFailedVisit();
            ClearActiveSubject();
            return Lives == 0 ? SessionRoute.GameOver : SessionRoute.Map;
        }

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
            visitAttempt = 1;
            awaitingPunishment = false;
        }
    }
}
