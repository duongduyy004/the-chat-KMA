using KMA.Gameplay;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballChallengeRules
    {
        readonly ChallengeDefinition definition;
        int completedTargets;

        public VolleyballMatch Match { get; }
        public int CompletedTargets => definition.Kind == ChallengeKind.Practice ? Match.PlayerPoints : completedTargets;
        public bool IsComplete => definition.Kind == ChallengeKind.Learn
            ? completedTargets >= definition.TargetCount
            : Match.IsOver;

        public VolleyballChallengeRules(ChallengeDefinition definition)
        {
            this.definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            VolleyballMatchOptions options = definition.Kind switch
            {
                ChallengeKind.Learn => new VolleyballMatchOptions(0, 0f, true),
                // Practice is a race to five against the clock; the opponent keeps the serve.
                ChallengeKind.Practice => new VolleyballMatchOptions(VolleyballMatch.PointsToWin, definition.TimeLimit,
                    true, requirePointsToWin: true),
                _ => new VolleyballMatchOptions(VolleyballMatch.PointsToWin, definition.TimeLimit,
                    requirePointsToWin: true)
            };
            Match = new VolleyballMatch(options: options);
            Match.PlayerActed += OnPlayerActed;
        }

        public void SetMove(UnityEngine.Vector2 move) => Match.SetMove(move);
        public ActionDecision PressAction() => Match.PressAction();
        public bool PressJump() => Match.PressJump();
        public void Tick(float dt) => Match.Tick(dt);

        public ChallengeAttemptResult BuildResult(ChallengeAttemptContext context)
        {
            if (context == null || context.ChallengeId != definition.Id)
                throw new System.ArgumentException("Volleyball challenge context does not match its definition.");
            bool pass = definition.Kind == ChallengeKind.Learn
                ? completedTargets >= definition.TargetCount
                : Match.IsOver && Match.PlayerPoints >= VolleyballMatch.PointsToWin;
            MinigameResult score = definition.Kind == ChallengeKind.Exam ? Match.BuildResult() : null;
            return new ChallengeAttemptResult(context, pass, new ChallengeMetrics(elapsed: Match.Elapsed,
                completedTargets: CompletedTargets), score);
        }

        void OnPlayerActed(ActionDecision decision)
        {
            if (definition.Kind == ChallengeKind.Learn && decision.Kind == ActionKind.Receive)
            {
                completedTargets++;
                Match.ResetRally(CourtSide.Opponent);
            }
        }
    }
}
