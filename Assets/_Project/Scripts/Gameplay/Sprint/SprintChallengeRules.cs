using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintChallengeRules
    {
        readonly ChallengeDefinition definition;

        public SprintChallengeRules(ChallengeDefinition definition, SprintBalanceParameters balance,
            RivalPaceProfile[] rivals)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.Subject != SubjectId.Sprint)
                throw new ArgumentException("Sprint challenge rules require a Sprint definition.", nameof(definition));
            Race = new SprintRules(definition.TimeLimit, rivals,
                new[] { Side.Left, Side.Right }, balance,
                definition.Distance > 0f ? definition.Distance : SprintRules.RaceDistance);
        }

        public SprintRules Race { get; }
        public bool IsComplete => definition.Kind == ChallengeKind.Learn
            ? Race.CorrectStreak >= definition.TargetCount
            : Race.Distance >= definition.Distance || Race.Elapsed >= definition.TimeLimit;

        public void Tap(Side side)
        {
            if (!IsComplete) Race.Tap(side);
        }

        public void Tick(float dt)
        {
            if (Race.IsFinished) { Race.Tick(dt); return; }
            if (IsComplete || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            float bounded = definition.TimeLimit > 0f
                ? Mathf.Min(dt, Mathf.Max(0f, definition.TimeLimit - Race.Elapsed)) : dt;
            if (bounded > 0f) Race.Tick(bounded);
        }

        public ChallengeAttemptResult BuildResult(ChallengeAttemptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!string.Equals(context.ChallengeId, definition.Id, StringComparison.Ordinal))
                throw new ArgumentException("Challenge context does not match the Sprint rules.", nameof(context));

            bool passed;
            MinigameResult examResult = null;
            int completedTargets;
            if (definition.Kind == ChallengeKind.Learn)
            {
                completedTargets = Mathf.Min(Race.CorrectStreak, definition.TargetCount);
                passed = completedTargets >= definition.TargetCount;
            }
            else
            {
                examResult = Race.BuildResult(definition.Distance, definition.TimeLimit);
                passed = examResult.Pass;
                completedTargets = passed ? definition.TargetCount : 0;
                // Practice saves no score, but a win still shows the score it earned.
                bool showScore = definition.Kind == ChallengeKind.Exam ||
                    (definition.Kind == ChallengeKind.Practice && passed);
                if (!showScore) examResult = null;
            }

            return new ChallengeAttemptResult(context, passed, new ChallengeMetrics(
                distance: Race.Distance, elapsed: Race.Elapsed, completedTargets: completedTargets,
                placement: Race.Rank), examResult);
        }
    }
}
