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

        public ChallengeMetrics(float distance = 0f, float elapsed = 0f, int completedTargets = 0,
            int kicks = 0, float stamina = 0f, int placement = 0)
        {
            Distance = distance;
            Elapsed = elapsed;
            CompletedTargets = completedTargets;
            Kicks = kicks;
            Stamina = stamina;
            Placement = placement;
        }
    }
}
