using System;
using System.Collections.Generic;

namespace KMA.Gameplay
{
    [Serializable]
    public sealed class JourneyStateData
    {
        public List<string> completedChallengeIds = new List<string>();
        public JourneyAttemptData activeAttempt;
        public string lastCommittedAttemptId;
        public JourneyResultData lastCommittedResult;
        public string awaitingSupplementaryChallengeId;
        public int supplementaryRounds;
        public List<string> seenDialogueIds = new List<string>();
    }

    [Serializable]
    public sealed class JourneyAttemptData
    {
        public string attemptId;
        public string challengeId;
        public ChallengeAttemptMode mode;
        public ChallengeDifficulty difficulty;

        public static JourneyAttemptData FromContext(ChallengeAttemptContext context) => context == null
            ? null
            : new JourneyAttemptData
            {
                attemptId = context.AttemptId,
                challengeId = context.ChallengeId,
                mode = context.Mode,
                difficulty = context.Difficulty
            };

        public ChallengeAttemptContext ToContext() => string.IsNullOrWhiteSpace(attemptId)
            ? null
            : new ChallengeAttemptContext(attemptId, challengeId, mode, difficulty);
    }

    [Serializable]
    public sealed class JourneyResultData
    {
        public JourneyAttemptData context;
        public bool pass;
        public float distance;
        public float elapsed;
        public int completedTargets;
        public int kicks;
        public float stamina;
        public int placement;
        public MinigameResult examResult;

        public static JourneyResultData FromResult(ChallengeAttemptResult result)
        {
            if (result == null)
                return null;

            ChallengeMetrics metrics = result.Metrics ?? new ChallengeMetrics();
            return new JourneyResultData
            {
                context = JourneyAttemptData.FromContext(result.Context),
                pass = result.Pass,
                distance = metrics.Distance,
                elapsed = metrics.Elapsed,
                completedTargets = metrics.CompletedTargets,
                kicks = metrics.Kicks,
                stamina = metrics.Stamina,
                placement = metrics.Placement,
                examResult = Copy(result.ExamResult)
            };
        }

        public ChallengeAttemptResult ToResult() => new ChallengeAttemptResult(context?.ToContext(), pass,
            new ChallengeMetrics(distance, elapsed, completedTargets, kicks, stamina, placement), Copy(examResult));

        internal JourneyResultData Copy() => new JourneyResultData
        {
            context = context == null ? null : new JourneyAttemptData
            {
                attemptId = context.attemptId,
                challengeId = context.challengeId,
                mode = context.mode,
                difficulty = context.difficulty
            },
            pass = pass,
            distance = distance,
            elapsed = elapsed,
            completedTargets = completedTargets,
            kicks = kicks,
            stamina = stamina,
            placement = placement,
            examResult = Copy(examResult)
        };

        static MinigameResult Copy(MinigameResult result) => result == null
            ? null
            : new MinigameResult(result.Pass, result.Score, result.Rank);
    }
}
