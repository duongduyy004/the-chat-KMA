namespace KMA.Gameplay
{
    public enum ChallengeAttemptMode
    {
        Journey,
        Supplementary,
        Review,
        FreePlay
    }

    public sealed class ChallengeAttemptContext
    {
        public string AttemptId { get; }
        public string ChallengeId { get; }
        public ChallengeAttemptMode Mode { get; }
        public ChallengeDifficulty Difficulty { get; }

        public ChallengeAttemptContext(string attemptId, string challengeId,
            ChallengeAttemptMode mode, ChallengeDifficulty difficulty)
        {
            AttemptId = attemptId;
            ChallengeId = challengeId;
            Mode = mode;
            Difficulty = difficulty;
        }
    }

    public sealed class ChallengeMetrics
    {
        public float Distance { get; }
        public float Elapsed { get; }
        public int CompletedTargets { get; }
        public int Kicks { get; }
        public float Stamina { get; }
        public int Placement { get; }
        public int Mistakes { get; }
        public bool HintUsed { get; }
        /// Short Vietnamese result line a controller wants shown, or null.
        public string Detail { get; }

        public ChallengeMetrics(float distance = 0f, float elapsed = 0f, int completedTargets = 0,
            int kicks = 0, float stamina = 0f, int placement = 0, int mistakes = 0, bool hintUsed = false,
            string detail = null)
        {
            Distance = distance;
            Elapsed = elapsed;
            CompletedTargets = completedTargets;
            Kicks = kicks;
            Stamina = stamina;
            Placement = placement;
            Mistakes = mistakes;
            HintUsed = hintUsed;
            Detail = detail;
        }
    }

    public sealed class FrogJumpPending
    {
        public string Id { get; }
        public string FailedAttemptId { get; }
        public string FailedChallengeId { get; }
        public bool SavesLife { get; }

        public FrogJumpPending(string id, string failedAttemptId, string failedChallengeId, bool savesLife)
        {
            Id = id;
            FailedAttemptId = failedAttemptId;
            FailedChallengeId = failedChallengeId;
            SavesLife = savesLife;
        }
    }
}
