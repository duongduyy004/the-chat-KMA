using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    /// <summary>Submits deterministic, domain-valid journey outcomes for progression integration tests.</summary>
    public static class JourneyGameplayDriver
    {
        public static JourneyCommitOutcome CompleteActiveChallenge(GameSession session, bool pass = true,
            float score = 6f)
        {
            string id = session.Journey.CheckpointChallengeId;
            ChallengeDefinition definition = session.Journey.Catalog.Get(id);
            if (!session.TryStartChallenge(id, ChallengeAttemptMode.Journey, definition.Difficulty,
                    out ChallengeAttemptContext context))
                return new JourneyCommitOutcome(false, id, session.Lives, session.PendingFrogJump != null,
                    session.PendingFrogJump?.SavesLife ?? false, session.Journey.CourseComplete);

            MinigameResult exam = definition.Kind == ChallengeKind.Exam
                ? new MinigameResult(pass, pass ? score : 0f, pass ? ScoreUtil.ToRank(score) : Rank.F)
                : null;
            return session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(completedTargets: pass ? definition.TargetCount : 0), exam));
        }

        /// Passes every incomplete challenge in catalog order up to and including `challengeId`.
        public static void CompleteThrough(GameSession session, string challengeId)
        {
            foreach (ChallengeDefinition definition in session.Journey.Catalog.Ordered)
            {
                if (!session.Journey.IsChallengeComplete(definition.Id))
                {
                    JourneyCommitOutcome outcome = CompleteActiveChallenge(session);
                    if (!outcome.Accepted)
                        throw new System.InvalidOperationException(
                            $"Could not complete journey challenge {definition.Id}.");
                }
                if (definition.Id == challengeId)
                    return;
            }
        }
    }
}
