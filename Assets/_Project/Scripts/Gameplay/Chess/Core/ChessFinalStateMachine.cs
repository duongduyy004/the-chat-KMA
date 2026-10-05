using System;

namespace KMA.Gameplay.Chess
{
    public enum ChessFinalPhase { Intro, PlayerTurn, Validating, BossTurn, Paused, Completed, Failed }

    public enum ChessFailReason { None, TimeUp, TooManyMistakes }

    /// Gameplay state of the final exam. Presentation listens to the events and calls
    /// CompleteBossMove once the boss move has been animated.
    public sealed class ChessFinalStateMachine
    {
        readonly PuzzleGrader grader;
        readonly ChessPosition start;
        ChessFinalPhase pausedFrom;

        public ChessFinalStateMachine(PuzzleDefinition puzzle)
        {
            Puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
            if (!puzzle.TryValidateShape(out string error)) throw new ArgumentException(error, nameof(puzzle));
            grader = new PuzzleGrader(puzzle);
            start = ChessPosition.FromFen(puzzle.startFen);
            Clock = new ThinkClock(puzzle.timeLimitSeconds);
            Reset();
        }

        public event Action<ChessFinalPhase> PhaseChanged;
        public event Action<ChessMove, bool> MoveCommitted;
        public event Action<ChessMove> IllegalMoveRejected;
        public event Action<int> MistakeMade;
        public event Action<bool> Finished;
        public event Action<string> GradingFailed;

        public PuzzleDefinition Puzzle { get; }
        public ThinkClock Clock { get; }
        public ChessFinalPhase Phase { get; private set; }
        public ChessPosition Position { get; private set; }
        public int Node { get; private set; }
        public int PlayerMovesMade { get; private set; }
        public int Mistakes { get; private set; }
        public int MaxMistakes => Puzzle.maxRecoverableMistakes;
        public int MaxPlayerMoves => Puzzle.maxPlayerMoves;
        public ChessFailReason FailReason { get; private set; }
        public ChessMove? LastMove { get; private set; }
        public ChessMove? PendingBossReply { get; private set; }
        public int HintLevel { get; private set; }
        public bool HintUsed => HintLevel > 0;
        public bool CanMove => Phase == ChessFinalPhase.PlayerTurn;
        public string CurrentHint => HintLevel == 0 ? null : PuzzleHints.For(Puzzle, Node, Position, HintLevel);

        public void Begin()
        {
            if (Phase != ChessFinalPhase.Intro) return;
            ReturnToPlayer();
        }

        /// Null when the move was not considered (wrong phase or a data error).
        public GradeKind? Submit(ChessMove move)
        {
            if (Phase != ChessFinalPhase.PlayerTurn) return null;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Validating);

            GradeResult grade;
            try
            {
                grade = grader.Grade(Position, Node, PlayerMovesMade, move);
            }
            catch (Exception exception)
            {
                GradingFailed?.Invoke(exception.Message);
                ReturnToPlayer();
                return null;
            }

            switch (grade.Kind)
            {
                case GradeKind.Illegal:
                    IllegalMoveRejected?.Invoke(move);
                    ReturnToPlayer();
                    break;
                case GradeKind.Wrong:
                    Mistakes++;
                    MistakeMade?.Invoke(Mistakes);
                    if (Mistakes > MaxMistakes) Fail(ChessFailReason.TooManyMistakes);
                    else ReturnToPlayer();
                    break;
                case GradeKind.Solved:
                    PlayerMovesMade++;
                    Commit(grade.After, move, true);
                    SetPhase(ChessFinalPhase.Completed);
                    Finished?.Invoke(true);
                    break;
                case GradeKind.Accepted:
                    PlayerMovesMade++;
                    Node = grade.NextNode;
                    PendingBossReply = grade.BossReply;
                    Commit(grade.After, move, true);
                    SetPhase(ChessFinalPhase.BossTurn);
                    break;
            }
            return grade.Kind;
        }

        public bool CompleteBossMove()
        {
            if (Phase != ChessFinalPhase.BossTurn || !PendingBossReply.HasValue) return false;
            ChessMove reply = PendingBossReply.Value;
            PendingBossReply = null;
            Commit(Position.Apply(reply), reply, false);
            ReturnToPlayer();
            return true;
        }

        public void Pause()
        {
            if (Phase == ChessFinalPhase.Paused || Phase == ChessFinalPhase.Completed ||
                Phase == ChessFinalPhase.Failed) return;
            pausedFrom = Phase;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Paused);
        }

        public void Resume()
        {
            if (Phase != ChessFinalPhase.Paused) return;
            if (pausedFrom == ChessFinalPhase.PlayerTurn) Clock.Start();
            SetPhase(pausedFrom);
        }

        public void Tick(float dt)
        {
            if (Phase == ChessFinalPhase.PlayerTurn && Clock.Tick(dt)) Fail(ChessFailReason.TimeUp);
        }

        public string RevealNextHint()
        {
            if (Phase != ChessFinalPhase.PlayerTurn) return null;
            HintLevel = Math.Min(PuzzleHints.MaxLevel, HintLevel + 1);
            return CurrentHint;
        }

        public void Restart() => Reset();

        void Reset()
        {
            Position = start;
            Node = 0;
            PlayerMovesMade = 0;
            Mistakes = 0;
            HintLevel = 0;
            FailReason = ChessFailReason.None;
            LastMove = null;
            PendingBossReply = null;
            Clock.Reset();
            Phase = ChessFinalPhase.Intro;
            PhaseChanged?.Invoke(Phase);
        }

        void ReturnToPlayer()
        {
            Clock.Start();
            SetPhase(ChessFinalPhase.PlayerTurn);
        }

        void Fail(ChessFailReason reason)
        {
            FailReason = reason;
            Clock.Stop();
            SetPhase(ChessFinalPhase.Failed);
            Finished?.Invoke(false);
        }

        void Commit(ChessPosition next, ChessMove move, bool byPlayer)
        {
            Position = next;
            LastMove = move;
            MoveCommitted?.Invoke(move, byPlayer);
        }

        void SetPhase(ChessFinalPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
