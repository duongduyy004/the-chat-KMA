using System;
using System.Linq;

namespace KMA.Gameplay.Chess
{
    public enum GradeKind { Illegal, Wrong, Accepted, Solved }

    public readonly struct GradeResult
    {
        public GradeKind Kind { get; }
        public ChessMove Move { get; }
        public ChessPosition After { get; }
        public ChessMove? BossReply { get; }
        public int NextNode { get; }

        public GradeResult(GradeKind kind, ChessMove move, ChessPosition after, ChessMove? bossReply, int nextNode)
        {
            Kind = kind;
            Move = move;
            After = after;
            BossReply = bossReply;
            NextNode = nextNode;
        }
    }

    public sealed class PuzzleGrader
    {
        readonly PuzzleDefinition puzzle;

        public PuzzleGrader(PuzzleDefinition puzzle) =>
            this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));

        public GradeResult Grade(ChessPosition position, int node, int playerMovesMade, ChessMove move)
        {
            if (!MoveGenerator.LegalMoves(position).Contains(move))
                return new GradeResult(GradeKind.Illegal, move, position, null, node);

            ChessPosition after = position.Apply(move);
            if (MoveGenerator.IsCheckmate(after))
                return new GradeResult(GradeKind.Solved, move, after, null, node);

            string uci = move.ToUci();
            PuzzleMove listed = puzzle.nodes[node].moves.FirstOrDefault(m => m.uci == uci);
            if (listed == null)
                return new GradeResult(GradeKind.Wrong, move, position, null, node);

            if (listed.mate)
                throw new InvalidOperationException($"Puzzle {puzzle.id}: {uci} is flagged mate but is not.");
            if (playerMovesMade + 1 >= puzzle.maxPlayerMoves)
                throw new InvalidOperationException($"Puzzle {puzzle.id}: {uci} uses the last move without mate.");

            PuzzleReply first = listed.replies[0];
            if (!ChessMove.TryParseUci(first.uci, out ChessMove reply) ||
                !MoveGenerator.LegalMoves(after).Contains(reply))
                throw new InvalidOperationException($"Puzzle {puzzle.id}: reply {first.uci} is not legal.");
            return new GradeResult(GradeKind.Accepted, move, after, reply, first.next);
        }
    }
}
