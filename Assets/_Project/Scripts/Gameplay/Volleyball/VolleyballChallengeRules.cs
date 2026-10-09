using KMA.Gameplay;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballChallengeRules
    {
        readonly ChallengeDefinition definition;
        int completedTargets;
        int sequence;

        public VolleyballMatch Match { get; }
        public int CompletedTargets => completedTargets;
        public bool IsComplete => definition.Kind == ChallengeKind.Learn
            ? completedTargets >= definition.TargetCount
            : definition.Kind == ChallengeKind.Practice
                ? completedTargets >= definition.TargetCount || Match.IsOver : Match.IsOver;

        public VolleyballChallengeRules(ChallengeDefinition definition)
        {
            this.definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            VolleyballMatchOptions options = definition.Kind switch
            {
                ChallengeKind.Learn => new VolleyballMatchOptions(0, 0f, true),
                // Practice is won on combo points; letting the opponent reach five ends it as a fail.
                ChallengeKind.Practice => new VolleyballMatchOptions(0, definition.TimeLimit, true,
                    opponentPointLimit: VolleyballMatch.PointsToWin),
                _ => new VolleyballMatchOptions(VolleyballMatch.PointsToWin, definition.TimeLimit,
                    requirePointsToWin: true)
            };
            Match = new VolleyballMatch(options: options);
            Match.PlayerActed += OnPlayerActed;
            Match.TouchRegistered += OnTouchRegistered;
            Match.PointScored += OnPointScored;
        }

        public void SetMove(UnityEngine.Vector2 move) => Match.SetMove(move);
        public ActionDecision PressAction() => Match.PressAction();
        public void Tick(float dt) => Match.Tick(dt);

        public ChallengeAttemptResult BuildResult(ChallengeAttemptContext context)
        {
            if (context == null || context.ChallengeId != definition.Id)
                throw new System.ArgumentException("Volleyball challenge context does not match its definition.");
            bool pass = definition.Kind switch
            {
                ChallengeKind.Learn => completedTargets >= definition.TargetCount,
                ChallengeKind.Practice => completedTargets >= definition.TargetCount,
                _ => Match.IsOver && Match.PlayerPoints >= VolleyballMatch.PointsToWin
            };
            MinigameResult score = definition.Kind == ChallengeKind.Exam ? Match.BuildResult() : null;
            return new ChallengeAttemptResult(context, pass, new ChallengeMetrics(elapsed: Match.Elapsed,
                completedTargets: completedTargets), score);
        }

        void OnPlayerActed(ActionDecision decision)
        {
            if (definition.Kind == ChallengeKind.Learn && decision.Kind == ActionKind.Receive)
            {
                completedTargets++;
                Match.ResetRally(CourtSide.Opponent);
            }
        }

        void OnTouchRegistered(CourtSide side, ActionDecision decision, int touchesBefore)
        {
            if (definition.Kind != ChallengeKind.Practice) return;
            if (side != CourtSide.Player) { sequence = 0; return; }
            sequence = decision.Kind switch
            {
                ActionKind.Receive when touchesBefore == 0 && sequence == 0 => 1,
                ActionKind.Receive when touchesBefore == 1 && sequence == 1 => 2,
                ActionKind.Smash when touchesBefore == 2 && sequence == 2 => 3,
                _ => 0
            };
        }

        void OnPointScored(CourtSide winner)
        {
            if (definition.Kind == ChallengeKind.Practice && winner == CourtSide.Player && sequence == 3)
                completedTargets++;
            if (definition.Kind == ChallengeKind.Practice) sequence = 0;
        }
    }
}
