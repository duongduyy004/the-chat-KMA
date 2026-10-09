using KMA.Gameplay;
using KMA.Gameplay.Celebration;
using KMA.Gameplay.Chess;
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
    ///   map-chess-locked   Map with the course done through soccer_practice (final stop locked)
    ///   map-chess-open     Map with the course done through soccer_exam (final exam is the checkpoint)
    ///   map-complete       Map with the whole course done (summary and replay button)
    ///   map-lessons        Map with the Sprint stop tapped, so the HỌC/LUYỆN/THI popup is open
    ///   chess-select       MG_ChessFinal: start the attempt and select the first main-line piece
    ///   chess-wrong        MG_ChessFinal: one wrong move (Sai: 1/2, whistle)
    ///   chess-boss         MG_ChessFinal: first correct move played, boss turn
    ///   chess-hint         MG_ChessFinal: hint level 2 shown
    ///   chess-promotion    MG_ChessFinal: promotion picker opened directly (the demo puzzle has no promotion)
    ///   chess-win          MG_ChessFinal: whole main line played, result panel (use a long wait)
    ///   chess-timeout      MG_ChessFinal: clock set to 89.9 s and ticked out, result panel (use a long wait)
    ///   celebration-cheer  Celebration: wait until the timeline reaches 3 s (cheer beat)
    ///   celebration-teacher Celebration: wait until the timeline reaches 6 s (teacher and bubble)
    ///   celebration-skip   Celebration: press Skip and wait for the summary to fade in
    public static class PlayModeScreenshotQaStates
    {
        public static bool CaptureSoonAfterApply { get; private set; }

        public static bool IsKnown(string state) => state == "" || state == "frog-fall" ||
            state == "frog-win-keep" || state == "frog-lose-zero" || state == "exam-fail-1" ||
            state == "exam-fail-2" || state == "map-lives-3" || state == "map-lives-0" || state == "chess-select" ||
            state == "chess-wrong" || state == "chess-boss" || state == "chess-hint" ||
            state == "chess-promotion" || state == "chess-win" || state == "chess-timeout" ||
            state == "map-chess-locked" || state == "map-chess-open" || state == "map-complete" || state == "map-lessons" ||
            state == "celebration-cheer" || state == "celebration-teacher" || state == "celebration-skip";

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
                case "map-chess-locked": return SetMapProgress("soccer_practice");
                case "map-chess-open": return SetMapProgress("soccer_exam");
                case "map-complete": return SetMapProgress("chess_final");
                case "map-lessons": return OpenMapLessons();
                case "chess-select": return CaptureSoonAfterApply = ChessSelect();
                case "chess-wrong": return CaptureSoonAfterApply = ChessWrong();
                case "chess-boss": return CaptureSoonAfterApply = ChessBoss();
                case "chess-hint": return ChessHint();
                case "chess-promotion": return ChessPromotion();
                case "chess-win": return CaptureSoonAfterApply = ChessWin();
                case "chess-timeout": return CaptureSoonAfterApply = ChessTimeout();
                case "celebration-cheer": return CaptureSoonAfterApply = CelebrationAt(3f);
                case "celebration-teacher": return CaptureSoonAfterApply = CelebrationAt(6f);
                case "celebration-skip": return CaptureSoonAfterApply = CelebrationSkip();
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

        static bool ChessSelect()
        {
            var controller = Object.FindFirstObjectByType<ChessFinalController>();
            var board = Object.FindFirstObjectByType<ChessBoardView>();
            if (controller == null || board == null || controller.Machine == null) return false;
            if (controller.Machine.Phase == ChessFinalPhase.Intro) controller.BeginAttempt();
            if (!board.Interactable) return false;
            ChessMove.TryParseUci(controller.Machine.Puzzle.nodes[0].moves[0].uci, out ChessMove move);
            board.ClickSquare(move.From);
            return board.SelectedSquare == move.From;
        }

        static ChessFinalController ReadyChess()
        {
            var controller = Object.FindFirstObjectByType<ChessFinalController>();
            if (controller == null || controller.Machine == null) return null;
            if (controller.Machine.Phase == ChessFinalPhase.Intro) controller.BeginAttempt();
            return controller;
        }

        static void PlayTreeMove(ChessFinalController controller)
        {
            ChessFinalStateMachine machine = controller.Machine;
            ChessMove.TryParseUci(machine.Puzzle.nodes[machine.Node].moves[0].uci, out ChessMove move);
            controller.RequestMove(move.From, move.To);
        }

        static double finishSeenAt;

        // The standalone scene has no SceneRouter, so show the card the router would: the final-mode challenge card.
        // Waits (wall clock) past the controller's result delay so the card replaces the phase banner as in play.
        static bool ShowChessResult(ChessFinalController controller, bool solved)
        {
            if (finishSeenAt == 0) finishSeenAt = UnityEditor.EditorApplication.timeSinceStartup;
            if (UnityEditor.EditorApplication.timeSinceStartup - finishSeenAt < 2.5) return false;
            var panel = Object.FindFirstObjectByType<ResultPanel>(FindObjectsInactive.Include);
            if (panel == null) return false;
            ChallengeDefinition definition = new GameSession().Journey.Catalog.Get("chess_final");
            var context = new ChallengeAttemptContext("qa", "chess_final", ChallengeAttemptMode.Journey,
                ChallengeDifficulty.Normal);
            MinigameResult mini = controller.BuildResult(solved);
            var result = new ChallengeAttemptResult(context, solved, controller.BuildMetrics(definition, mini), mini);
            var outcome = new JourneyCommitOutcome(true, null, 0, false, false, solved, true);
            panel.ShowChallenge(context, result, outcome, null);
            return true;
        }

        static bool ChessWrong()
        {
            var controller = ReadyChess();
            if (controller == null) return false;
            ChessFinalStateMachine machine = controller.Machine;
            if (machine.Phase == ChessFinalPhase.PlayerTurn && machine.Mistakes == 0)
            {
                ChessMove.TryParseUci(machine.Puzzle.nodes[0].moves[0].uci, out ChessMove right);
                foreach (ChessMove move in MoveGenerator.LegalMoves(machine.Position))
                    if (move.From != right.From || move.To != right.To)
                    {
                        controller.RequestMove(move.From, move.To);
                        break;
                    }
            }
            return machine.Mistakes == 1;
        }

        static bool ChessBoss()
        {
            var controller = ReadyChess();
            if (controller == null) return false;
            if (controller.Machine.Phase == ChessFinalPhase.PlayerTurn && controller.Machine.PlayerMovesMade == 0)
                PlayTreeMove(controller);
            return controller.Machine.Phase == ChessFinalPhase.BossTurn;
        }

        static bool ChessHint()
        {
            var controller = ReadyChess();
            if (controller == null || controller.Machine.Phase != ChessFinalPhase.PlayerTurn) return false;
            if (controller.Machine.HintLevel < 2) controller.RevealHint();
            return controller.Machine.HintLevel == 2;
        }

        static bool ChessPromotion()
        {
            var controller = ReadyChess();
            var picker = Object.FindFirstObjectByType<PromotionPicker>(FindObjectsInactive.Include);
            if (controller == null || picker == null) return false;
            if (!picker.IsOpen) picker.Open(_ => { });
            return picker.IsOpen;
        }

        static bool ChessWin()
        {
            var controller = ReadyChess();
            if (controller == null) return false;
            if (controller.Machine.Phase == ChessFinalPhase.PlayerTurn) PlayTreeMove(controller);
            return controller.Machine.Phase == ChessFinalPhase.Completed && ShowChessResult(controller, true);
        }

        static bool ChessTimeout()
        {
            var controller = ReadyChess();
            if (controller == null) return false;
            ChessFinalStateMachine machine = controller.Machine;
            if (machine.Phase == ChessFinalPhase.PlayerTurn && machine.Clock.Elapsed < 89f) machine.Clock.Tick(89.9f);
            return machine.Phase == ChessFinalPhase.Failed && ShowChessResult(controller, false);
        }

        // Game time lags wall time in a background Editor, so the beats are reached on the timeline clock.
        static bool CelebrationAt(float seconds)
        {
            var controller = Object.FindFirstObjectByType<CelebrationSceneController>();
            return controller != null && controller.Timeline.Time >= seconds;
        }

        static bool CelebrationSkip()
        {
            var controller = Object.FindFirstObjectByType<CelebrationSceneController>();
            if (controller == null || controller.Summary == null) return false;
            controller.Skip();
            return controller.SummaryVisible;
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

        static bool OpenMapLessons()
        {
            var map = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            if (map == null || map.LessonList == null) return false;
            map.SelectSubject(SubjectId.Sprint);
            return true;
        }

        // A throwaway session (never persisted) with every challenge passed up to and including lastId.
        static bool SetMapProgress(string lastId)
        {
            var map = Object.FindFirstObjectByType<MapScreen>(FindObjectsInactive.Include);
            if (map == null || map.Hearts == null) return false;
            var session = new GameSession();
            foreach (ChallengeDefinition definition in session.Journey.Catalog.Ordered)
            {
                if (session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey, definition.Difficulty,
                        out var context))
                    session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                        new ChallengeMetrics(completedTargets: definition.TargetCount),
                        ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null));
                if (definition.Id == lastId) break;
            }
            session.RefreshLives();
            map.BindPresentation(map.Nodes, map.Hearts, session, map.LessonList);
            return true;
        }
    }
}
