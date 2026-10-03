using System;

namespace KMA.Gameplay.Core
{
    public delegate bool JourneyPersistHandler(out string error);

    /// <summary>Applies a result in memory, persists it, then notifies session observers.</summary>
    public sealed class JourneySaveCoordinator
    {
        readonly GameSession session;
        readonly JourneyPersistHandler persist;
        ChallengeAttemptResult pendingResult;

        public JourneySaveCoordinator(GameSession session, JourneyPersistHandler persist)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        public ChallengeAttemptResult PendingResult => pendingResult;

        public bool TryCommit(ChallengeAttemptResult result, out JourneyCommitOutcome outcome,
            out string error)
        {
            outcome = default;
            error = null;
            if (result == null)
            {
                error = "Challenge result is required.";
                return false;
            }

            SaveData snapshot = session.ToSaveData();
            outcome = session.SubmitChallengeResult(result, notify: false);
            if (!outcome.Accepted)
                return false;

            pendingResult = result;
            try
            {
                if (!persist(out error))
                {
                    session.RestoreSnapshot(snapshot);
                    return false;
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                session.RestoreSnapshot(snapshot);
                return false;
            }

            pendingResult = null;
            session.NotifyJourneyChanged();
            return true;
        }

        public bool RetrySave(out JourneyCommitOutcome outcome, out string error)
        {
            outcome = default;
            error = null;
            if (pendingResult == null)
                return false;
            return TryCommit(pendingResult, out outcome, out error);
        }
    }
}
