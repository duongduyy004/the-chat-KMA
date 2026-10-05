using System;

namespace KMA.Gameplay
{
    public sealed class ChallengeAttemptResult
    {
        public ChallengeAttemptContext Context { get; }
        public bool Pass { get; }
        public ChallengeMetrics Metrics { get; }
        public MinigameResult ExamResult { get; }

        public ChallengeAttemptResult(ChallengeAttemptContext context, bool pass,
            ChallengeMetrics metrics, MinigameResult examResult = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Pass = pass;
            Metrics = metrics ?? new ChallengeMetrics();
            ExamResult = examResult;
        }
    }

    public readonly struct JourneyCommitOutcome
    {
        public bool Accepted { get; }
        public string NextChallengeId { get; }
        public int AttemptsRemaining { get; }
        public bool FrogJumpRequired { get; }
        public bool FrogJumpSavesLife { get; }
        public bool CourseComplete { get; }

        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool frogJumpRequired, bool frogJumpSavesLife, bool courseComplete)
        {
            Accepted = accepted;
            NextChallengeId = nextChallengeId;
            AttemptsRemaining = attemptsRemaining;
            FrogJumpRequired = frogJumpRequired;
            FrogJumpSavesLife = frogJumpSavesLife;
            CourseComplete = courseComplete;
        }

        [Obsolete("Supplementary rounds were removed; deleted in Task 8.")]
        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool awaitingSupplementary, bool courseComplete)
            : this(accepted, nextChallengeId, attemptsRemaining, false, false, courseComplete) { }

        [Obsolete("Supplementary rounds were removed; deleted in Task 8.")]
        public bool AwaitingSupplementary => false;
    }

    public interface IChallengeController
    {
        SubjectId Subject { get; }
        event Action<ChallengeAttemptResult> ChallengeCompleted;
        void ConfigureChallenge(ChallengeDefinition definition, ChallengeAttemptContext context);
    }

    public enum JourneyResultAction
    {
        Continue,
        Retry,
        // Retained for serialized values; the supplementary practice route was removed.
        Practice,
        RetrySave,
        FrogJump
    }

    public interface IChallengeResultPanel
    {
        event Action<JourneyResultAction> JourneyActionRequested;
        void ShowChallenge(ChallengeAttemptContext context, ChallengeAttemptResult result,
            JourneyCommitOutcome? outcome, string saveError);
    }
}
