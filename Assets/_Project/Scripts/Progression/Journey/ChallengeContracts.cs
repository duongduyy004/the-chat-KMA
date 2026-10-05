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

    public readonly struct FrogJumpResultView
    {
        public bool ReachedFinish { get; }
        public bool SavesLife { get; }
        public int LivesRemaining { get; }
        public string Error { get; }
        /// The failed challenge the frog jump retries is a practice (not an exam).
        public bool RetryIsPractice { get; }

        public FrogJumpResultView(bool reachedFinish, bool savesLife, int livesRemaining, string error,
            bool retryIsPractice = false)
        {
            ReachedFinish = reachedFinish;
            SavesLife = savesLife;
            LivesRemaining = livesRemaining;
            Error = error;
            RetryIsPractice = retryIsPractice;
        }
    }

    public interface IFrogJumpResultPanel
    {
        event Action FrogJumpContinueRequested;
        void ShowFrogJump(FrogJumpResultView view);
    }
}
