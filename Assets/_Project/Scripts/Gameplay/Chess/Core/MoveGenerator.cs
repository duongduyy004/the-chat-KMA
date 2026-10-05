using System.Collections.Generic;

namespace KMA.Gameplay.Chess
{
    public static class MoveGenerator
    {
        static readonly int[] KnightDf = { 1, 2, 2, 1, -1, -2, -2, -1 };
        static readonly int[] KnightDr = { 2, 1, -1, -2, -2, -1, 1, 2 };
        static readonly int[] KingDf = { 1, 1, 0, -1, -1, -1, 0, 1 };
        static readonly int[] KingDr = { 0, 1, 1, 1, 0, -1, -1, -1 };
        static readonly int[] DiagDf = { 1, 1, -1, -1 };
        static readonly int[] DiagDr = { 1, -1, 1, -1 };
        static readonly int[] OrthoDf = { 1, -1, 0, 0 };
        static readonly int[] OrthoDr = { 0, 0, 1, -1 };
        static readonly int[] Promotions = { Piece.Queen, Piece.Rook, Piece.Bishop, Piece.Knight };

        public static List<ChessMove> LegalMoves(ChessPosition position)
        {
            var pseudo = new List<ChessMove>(64);
            AddPseudoMoves(position, pseudo);
            var legal = new List<ChessMove>(pseudo.Count);
            PieceColor mover = position.SideToMove;
            foreach (ChessMove move in pseudo)
            {
                ChessPosition next = position.Apply(move);
                if (!IsSquareAttacked(next, next.KingSquare(mover), Square.Opposite(mover)))
                    legal.Add(move);
            }
            return legal;
        }

        public static bool IsInCheck(ChessPosition position) => IsSquareAttacked(position,
            position.KingSquare(position.SideToMove), Square.Opposite(position.SideToMove));

        public static bool IsCheckmate(ChessPosition position) =>
            IsInCheck(position) && LegalMoves(position).Count == 0;

        public static bool IsStalemate(ChessPosition position) =>
            !IsInCheck(position) && LegalMoves(position).Count == 0;

        public static long Perft(ChessPosition position, int depth)
        {
            if (depth == 0) return 1;
            List<ChessMove> moves = LegalMoves(position);
            if (depth == 1) return moves.Count;
            long nodes = 0;
            foreach (ChessMove move in moves) nodes += Perft(position.Apply(move), depth - 1);
            return nodes;
        }

        public static bool IsSquareAttacked(ChessPosition p, int square, PieceColor by)
        {
            int f = Square.File(square), r = Square.Rank(square);
            // A white pawn attacks diagonally upward, so it sits one rank below the target.
            int pawnRank = by == PieceColor.White ? r - 1 : r + 1;
            sbyte pawn = Piece.Make(Piece.Pawn, by);
            if (Has(p, f - 1, pawnRank, pawn) || Has(p, f + 1, pawnRank, pawn)) return true;
            sbyte knight = Piece.Make(Piece.Knight, by);
            for (int i = 0; i < 8; i++)
                if (Has(p, f + KnightDf[i], r + KnightDr[i], knight)) return true;
            sbyte king = Piece.Make(Piece.King, by);
            for (int i = 0; i < 8; i++)
                if (Has(p, f + KingDf[i], r + KingDr[i], king)) return true;
            return SlidingAttack(p, f, r, DiagDf, DiagDr, by, Piece.Bishop) ||
                   SlidingAttack(p, f, r, OrthoDf, OrthoDr, by, Piece.Rook);
        }

        static bool SlidingAttack(ChessPosition p, int f, int r, int[] df, int[] dr, PieceColor by, int slider)
        {
            for (int d = 0; d < df.Length; d++)
            {
                int x = f + df[d], y = r + dr[d];
                while (OnBoard(x, y))
                {
                    sbyte piece = p[Square.At(x, y)];
                    if (piece != 0)
                    {
                        int type = Piece.TypeOf(piece);
                        if (Piece.ColorOf(piece) == by && (type == slider || type == Piece.Queen)) return true;
                        break;
                    }
                    x += df[d];
                    y += dr[d];
                }
            }
            return false;
        }

        static void AddPseudoMoves(ChessPosition p, List<ChessMove> moves)
        {
            PieceColor us = p.SideToMove;
            for (int sq = 0; sq < 64; sq++)
            {
                sbyte piece = p[sq];
                if (piece == 0 || Piece.ColorOf(piece) != us) continue;
                int f = Square.File(sq), r = Square.Rank(sq);
                switch (Piece.TypeOf(piece))
                {
                    case Piece.Pawn: AddPawnMoves(p, sq, f, r, us, moves); break;
                    case Piece.Knight: AddSteps(p, sq, f, r, KnightDf, KnightDr, us, moves); break;
                    case Piece.Bishop: AddSlides(p, sq, f, r, DiagDf, DiagDr, us, moves); break;
                    case Piece.Rook: AddSlides(p, sq, f, r, OrthoDf, OrthoDr, us, moves); break;
                    case Piece.Queen:
                        AddSlides(p, sq, f, r, DiagDf, DiagDr, us, moves);
                        AddSlides(p, sq, f, r, OrthoDf, OrthoDr, us, moves);
                        break;
                    case Piece.King:
                        AddSteps(p, sq, f, r, KingDf, KingDr, us, moves);
                        AddCastling(p, sq, us, moves);
                        break;
                }
            }
        }

        static void AddPawnMoves(ChessPosition p, int sq, int f, int r, PieceColor us, List<ChessMove> moves)
        {
            int dir = us == PieceColor.White ? 1 : -1;
            int startRank = us == PieceColor.White ? 1 : 6;
            int lastRank = us == PieceColor.White ? 7 : 0;
            int forward = r + dir;
            if (!OnBoard(f, forward)) return;

            int one = Square.At(f, forward);
            if (p[one] == 0)
            {
                AddPawnMove(sq, one, forward == lastRank, moves);
                if (r == startRank)
                {
                    int two = Square.At(f, r + 2 * dir);
                    if (p[two] == 0) moves.Add(new ChessMove(sq, two));
                }
            }

            for (int df = -1; df <= 1; df += 2)
            {
                int x = f + df;
                if (!OnBoard(x, forward)) continue;
                int target = Square.At(x, forward);
                sbyte victim = p[target];
                if ((victim != 0 && Piece.ColorOf(victim) != us) || (victim == 0 && target == p.EnPassant))
                    AddPawnMove(sq, target, forward == lastRank, moves);
            }
        }

        static void AddPawnMove(int from, int to, bool promotes, List<ChessMove> moves)
        {
            if (!promotes)
            {
                moves.Add(new ChessMove(from, to));
                return;
            }
            foreach (int type in Promotions) moves.Add(new ChessMove(from, to, type));
        }

        static void AddSteps(ChessPosition p, int sq, int f, int r, int[] df, int[] dr, PieceColor us,
            List<ChessMove> moves)
        {
            for (int i = 0; i < df.Length; i++)
            {
                int x = f + df[i], y = r + dr[i];
                if (!OnBoard(x, y)) continue;
                int target = Square.At(x, y);
                sbyte victim = p[target];
                if (victim == 0 || Piece.ColorOf(victim) != us) moves.Add(new ChessMove(sq, target));
            }
        }

        static void AddSlides(ChessPosition p, int sq, int f, int r, int[] df, int[] dr, PieceColor us,
            List<ChessMove> moves)
        {
            for (int d = 0; d < df.Length; d++)
            {
                int x = f + df[d], y = r + dr[d];
                while (OnBoard(x, y))
                {
                    int target = Square.At(x, y);
                    sbyte victim = p[target];
                    if (victim == 0) moves.Add(new ChessMove(sq, target));
                    else
                    {
                        if (Piece.ColorOf(victim) != us) moves.Add(new ChessMove(sq, target));
                        break;
                    }
                    x += df[d];
                    y += dr[d];
                }
            }
        }

        static void AddCastling(ChessPosition p, int kingSquare, PieceColor us, List<ChessMove> moves)
        {
            int home = us == PieceColor.White ? 4 : 60;
            if (kingSquare != home) return;
            PieceColor them = Square.Opposite(us);
            if (IsSquareAttacked(p, home, them)) return;
            sbyte rook = Piece.Make(Piece.Rook, us);
            CastlingRights kingSide = us == PieceColor.White ? CastlingRights.WhiteKing : CastlingRights.BlackKing;
            CastlingRights queenSide = us == PieceColor.White ? CastlingRights.WhiteQueen : CastlingRights.BlackQueen;

            if ((p.Castling & kingSide) != 0 && p[home + 3] == rook && p[home + 1] == 0 && p[home + 2] == 0 &&
                !IsSquareAttacked(p, home + 1, them) && !IsSquareAttacked(p, home + 2, them))
                moves.Add(new ChessMove(home, home + 2));
            if ((p.Castling & queenSide) != 0 && p[home - 4] == rook && p[home - 1] == 0 && p[home - 2] == 0 &&
                p[home - 3] == 0 && !IsSquareAttacked(p, home - 1, them) && !IsSquareAttacked(p, home - 2, them))
                moves.Add(new ChessMove(home, home - 2));
        }

        static bool OnBoard(int f, int r) => f >= 0 && f < 8 && r >= 0 && r < 8;

        static bool Has(ChessPosition p, int f, int r, sbyte piece) => OnBoard(f, r) && p[Square.At(f, r)] == piece;
    }
}
