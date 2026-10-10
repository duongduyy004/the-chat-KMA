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
        public List<JourneyFailCountData> failCounts = new List<JourneyFailCountData>();
        public JourneyFrogJumpData pendingFrogJump;
        public string lastAppliedFrogJumpId;
        public JourneyChessRecordData chessBest = new JourneyChessRecordData();
        public bool celebrationSeen;
    }

    [Serializable]
    public sealed class JourneyChessRecordData
    {
        public bool recorded;
        public float score;
        public float thinkSeconds;
        public int mistakes;
        public bool hintUsed;

        public JourneyChessRecordData Copy() => new JourneyChessRecordData
        {
            recorded = recorded, score = score, thinkSeconds = thinkSeconds, mistakes = mistakes, hintUsed = hintUsed
        };
    }

    [Serializable]
    public sealed class JourneyFailCountData
    {
        public string challengeId;
        public int count;
    }

    [Serializable]
    public sealed class JourneyFrogJumpData
    {
        public string id;
        public string failedAttemptId;
        public string failedChallengeId;
        public bool savesLife;

        public static JourneyFrogJumpData FromPending(FrogJumpPending pending) => pending == null
            ? null
            : new JourneyFrogJumpData
            {
                id = pending.Id,
                failedAttemptId = pending.FailedAttemptId,
                failedChallengeId = pending.FailedChallengeId,
                savesLife = pending.SavesLife
            };

        // JsonUtility writes an empty object for null fields, so an empty id means "none".
        public FrogJumpPending ToPending() => string.IsNullOrWhiteSpace(id)
            ? null
            : new FrogJumpPending(id, failedAttemptId, failedChallengeId, savesLife);
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
                placement = metrics.Placement,
                examResult = Copy(result.ExamResult)
            };
        }

        public ChallengeAttemptResult ToResult() => new ChallengeAttemptResult(context?.ToContext(), pass,
            new ChallengeMetrics(distance, elapsed, completedTargets, kicks, placement), Copy(examResult));

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
            placement = placement,
            examResult = Copy(examResult)
        };

        static MinigameResult Copy(MinigameResult result) => result == null
            ? null
            : new MinigameResult(result.Pass, result.Score, result.Rank);
    }
}
