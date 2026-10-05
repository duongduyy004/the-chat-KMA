using KMA.Gameplay;
using KMA.Gameplay.FrogJump;
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.EditorTools
{
    /// QA-only: puts a scene into a frog-jump-penalty state for a screenshot through the same public
    /// seams production uses (ResultPanel.ShowFrogJump/ShowChallenge, FrogJumpRules.Stop, GameSession).
    /// Never runs outside a PlayModeScreenshot capture.
    ///   frog-fall          MG_FrogJump: stop the needle on a red end shortly before the capture
    ///   frog-win-keep      frog result card, reached the finish with lives left (THI LẠI)
    ///   frog-lose-zero     frog result card, timed out with 0 lives (VỀ BẢN ĐỒ)
    ///   exam-fail-1        challenge result panel, first failure (saves the life)
    ///   exam-fail-2        challenge result panel, second failure (life already lost)
    ///   map-lives-3        Map with 3 lives (regen countdown running)
    ///   map-lives-0        Map with 0 lives
    public static class PlayModeScreenshotQaStates
    {
        public static bool CaptureSoonAfterApply { get; private set; }

        public static bool IsKnown(string state) => state == "" || state == "frog-fall" ||
            state == "frog-win-keep" || state == "frog-lose-zero" || state == "exam-fail-1" ||
            state == "exam-fail-2" || state == "map-lives-3" || state == "map-lives-0";

        /// Called every editor tick while a capture is active. Returns true once the state is applied.
        public static bool Apply(string state, float secondsToCapture)
        {
            CaptureSoonAfterApply = false;
            switch (state)
            {
                case "frog-fall": return CaptureSoonAfterApply = FrogFall();
                case "frog-win-keep": return ShowFrog(new FrogJumpResultView(true, true, 3, null));
                case "frog-lose-zero": return ShowFrog(new FrogJumpResultView(false, true, 0, null));
                case "exam-fail-1": return ShowExamFailure(3, true);
                case "exam-fail-2": return ShowExamFailure(2, false);
                case "map-lives-3": return SetMapLives(3);
                case "map-lives-0": return SetMapLives(0);
                default: return true;
            }
        }

        static bool FrogFall()
        {
            var controller = Object.FindFirstObjectByType<FrogJumpController>();
            if (controller == null || controller.Rules == null) return false;
            if (controller.PresentationPhase != MinigamePhase.Play) return false;
            float needle = controller.Rules.Needle01;
            if (!FrogJumpRules.IsFall(needle, controller.Rules.Tuning)) return false;
            return controller.Rules.Stop() || controller.Rules.State != FrogJumpState.Aiming;
        }

        static bool ShowFrog(FrogJumpResultView view)
        {
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            if (panel == null) return false;
            panel.ShowFrogJump(view);
            return true;
        }

        static bool ShowExamFailure(int attemptsRemaining, bool savesLife)
        {
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            if (panel == null) return false;
            var context = new ChallengeAttemptContext("qa", "sprint_exam", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal);
            var result = new ChallengeAttemptResult(context, false, new ChallengeMetrics(),
                new MinigameResult(false, 0f, Rank.F));
            var outcome = new JourneyCommitOutcome(true, null, attemptsRemaining, true, savesLife, false);
            panel.ShowChallenge(context, result, outcome, null);
            return true;
        }

        static bool SetMapLives(int lives)
        {
            var map = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            if (map == null || map.Hearts == null) return false;
            // A throwaway session (never persisted) with sprint learn+practice done, so the exam card is the checkpoint.
            var session = new GameSession();
            foreach (string id in new[] { "sprint_learn", "sprint_practice" })
            {
                ChallengeDefinition definition = session.Journey.Catalog.Get(id);
                if (!session.TryStartChallenge(id, ChallengeAttemptMode.Journey, definition.Difficulty, out var context))
                    continue;
                session.SubmitChallengeResult(new ChallengeAttemptResult(context, true, new ChallengeMetrics()));
            }
            session.Journey.SetAttemptsRemaining(lives);
            session.RefreshLives();
            map.BindPresentation(map.Nodes, map.Hearts, session, map.LessonList);
            return true;
        }
    }
}
