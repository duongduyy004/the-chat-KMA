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
        public bool AwaitingSupplementary { get; }
        public bool CourseComplete { get; }

        public JourneyCommitOutcome(bool accepted, string nextChallengeId, int attemptsRemaining,
            bool awaitingSupplementary, bool courseComplete)
        {
            Accepted = accepted;
            NextChallengeId = nextChallengeId;
            AttemptsRemaining = attemptsRemaining;
            AwaitingSupplementary = awaitingSupplementary;
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
        Practice,
        RetrySave
    }

    public interface IChallengeResultPanel
    {
        event Action<JourneyResultAction> JourneyActionRequested;
        void ShowChallenge(ChallengeAttemptContext context, ChallengeAttemptResult result,
            JourneyCommitOutcome? outcome, string saveError);
    }
}
