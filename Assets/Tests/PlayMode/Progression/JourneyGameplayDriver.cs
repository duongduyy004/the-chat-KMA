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
            ChallengeAttemptMode mode = session.Journey.AwaitingSupplementary
                ? ChallengeAttemptMode.Supplementary : ChallengeAttemptMode.Journey;
            if (!session.TryStartChallenge(id, mode, definition.Difficulty,
                    out ChallengeAttemptContext context))
                return new JourneyCommitOutcome(false, id, session.Lives, session.Journey.AwaitingSupplementary,
                    session.Journey.CourseComplete);

            MinigameResult exam = definition.Kind == ChallengeKind.Exam
                ? new MinigameResult(pass, pass ? score : 0f, pass ? ScoreUtil.ToRank(score) : Rank.F)
                : null;
            return session.SubmitChallengeResult(new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(completedTargets: pass ? definition.TargetCount : 0), exam));
        }
    }
}
