using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    internal static class JourneyTestData
    {
        public static void CompleteThrough(GameSession session, string challengeId)
        {
            foreach (ChallengeDefinition definition in ChallengeCatalog.LoadDefault().Ordered)
            {
                if (session.Journey.IsChallengeComplete(definition.Id))
                {
                    if (definition.Id == challengeId)
                        return;
                    continue;
                }
                Play(session, definition.Id, true);
                if (definition.Id == challengeId)
                {
                    return;
                }
            }
        }

        public static JourneyCommitOutcome Play(GameSession session, string challengeId, bool pass)
        {
            ChallengeDefinition definition = ChallengeCatalog.LoadDefault().Get(challengeId);
            if (!session.TryStartChallenge(challengeId, ChallengeAttemptMode.Journey,
                definition.Difficulty, out ChallengeAttemptContext context))
            {
                throw new System.InvalidOperationException($"Could not start journey challenge {challengeId}.");
            }

            var result = new ChallengeAttemptResult(context, pass,
                new ChallengeMetrics(distance: definition.Distance, elapsed: definition.TimeLimit,
                    completedTargets: definition.TargetCount),
                definition.Kind == ChallengeKind.Exam
                    ? new MinigameResult(pass, pass ? 8f : 0f, pass ? Rank.A : Rank.F)
                    : null);
            return session.SubmitChallengeResult(result);
        }
    }
}
