using System;
using System.Text;

namespace KMA.Gameplay.Chess
{
    /// Immutable: Apply returns a new position, so a snapshot is just a kept reference.
    public sealed class ChessPosition
    {
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        readonly sbyte[] board;

        public PieceColor SideToMove { get; }
        public CastlingRights Castling { get; }
        public int EnPassant { get; }
        public int HalfmoveClock { get; }
        public int FullmoveNumber { get; }

        ChessPosition(sbyte[] board, PieceColor side, CastlingRights castling, int enPassant, int halfmove,
            int fullmove)
        {
            this.board = board;
            SideToMove = side;
            Castling = castling;
            EnPassant = enPassant;
            HalfmoveClock = halfmove;
            FullmoveNumber = fullmove;
        }

        public sbyte this[int square] => board[square];

        public int KingSquare(PieceColor color)
        {
            sbyte king = Piece.Make(Piece.King, color);
            for (int i = 0; i < 64; i++)
                if (board[i] == king) return i;
            throw new InvalidOperationException($"No {color} king on the board.");
        }

        public static ChessPosition FromFen(string fen)
        {
            string[] parts = (fen ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts.Length > 6) throw new FormatException("A FEN needs 4 to 6 fields.");

            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8) throw new FormatException("A FEN board needs 8 ranks.");
            var board = new sbyte[64];
            for (int i = 0; i < 8; i++)
            {
                int rank = 7 - i, file = 0;
                foreach (char c in ranks[i])
                {
                    if (c >= '1' && c <= '8') file += c - '0';
                    else
                    {
                        if (file > 7) throw new FormatException("A FEN rank is too long.");
                        board[Square.At(file++, rank)] = Piece.FromFen(c);
                    }
                    if (file > 8) throw new FormatException("A FEN rank is too long.");
                }
                if (file != 8) throw new FormatException("A FEN rank must cover 8 files.");
            }
            if (Count(board, Piece.Make(Piece.King, PieceColor.White)) != 1 ||
                Count(board, Piece.Make(Piece.King, PieceColor.Black)) != 1)
                throw new FormatException("A FEN needs exactly one king per side.");

            PieceColor side = parts[1] == "w" ? PieceColor.White
                : parts[1] == "b" ? PieceColor.Black
                : throw new FormatException("Side to move must be w or b.");

            CastlingRights castling = CastlingRights.None;
            if (parts[2] != "-")
            {
                foreach (char c in parts[2])
                {
                    castling |= c switch
                    {
                        'K' => CastlingRights.WhiteKing,
                        'Q' => CastlingRights.WhiteQueen,
                        'k' => CastlingRights.BlackKing,
                        'q' => CastlingRights.BlackQueen,
                        _ => throw new FormatException($"Unknown castling flag '{c}'.")
                    };
                }
            }

            int enPassant = -1;
            if (parts[3] != "-")
            {
                enPassant = Square.Parse(parts[3]);
                if (enPassant < 0 || (Square.Rank(enPassant) != 2 && Square.Rank(enPassant) != 5))
                    throw new FormatException("Invalid en passant square.");
            }

            int halfmove = parts.Length > 4 ? ParseCounter(parts[4], 0) : 0;
            int fullmove = parts.Length > 5 ? ParseCounter(parts[5], 1) : 1;
            return new ChessPosition(board, side, castling, enPassant, halfmove, fullmove);
        }

        public string ToFen()
        {
            var text = new StringBuilder(90);
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    sbyte piece = board[Square.At(file, rank)];
                    if (piece == 0) { empty++; continue; }
                    if (empty > 0) { text.Append(empty); empty = 0; }
                    text.Append(Piece.ToFen(piece));
                }
                if (empty > 0) text.Append(empty);
                if (rank > 0) text.Append('/');
            }
            text.Append(SideToMove == PieceColor.White ? " w " : " b ");
            if (Castling == CastlingRights.None) text.Append('-');
            else
            {
                if ((Castling & CastlingRights.WhiteKing) != 0) text.Append('K');
                if ((Castling & CastlingRights.WhiteQueen) != 0) text.Append('Q');
                if ((Castling & CastlingRights.BlackKing) != 0) text.Append('k');
                if ((Castling & CastlingRights.BlackQueen) != 0) text.Append('q');
            }
            text.Append(' ').Append(EnPassant < 0 ? "-" : Square.Name(EnPassant));
            text.Append(' ').Append(HalfmoveClock).Append(' ').Append(FullmoveNumber);
            return text.ToString();
        }

        /// Applies a move that the generator produced. It does not check legality.
        public ChessPosition Apply(ChessMove move)
        {
            sbyte piece = board[move.From];
            if (piece == 0 || Piece.ColorOf(piece) != SideToMove)
                throw new InvalidOperationException($"{move} does not move a {SideToMove} piece.");
            int type = Piece.TypeOf(piece);
            sbyte captured = board[move.To];
            var next = (sbyte[])board.Clone();
            next[move.From] = 0;
            next[move.To] = move.Promotion != 0 ? Piece.Make(move.Promotion, SideToMove) : piece;

            bool enPassantCapture = type == Piece.Pawn && move.To == EnPassant && captured == 0;
            if (enPassantCapture)
                next[move.To + (SideToMove == PieceColor.White ? -8 : 8)] = 0;

            if (type == Piece.King && Math.Abs(move.To - move.From) == 2)
            {
                bool kingSide = move.To > move.From;
                int rookFrom = kingSide ? move.From + 3 : move.From - 4;
                int rookTo = kingSide ? move.From + 1 : move.From - 1;
                next[rookTo] = next[rookFrom];
                next[rookFrom] = 0;
            }

            int enPassant = type == Piece.Pawn && Math.Abs(move.To - move.From) == 16
                ? (move.From + move.To) / 2 : -1;
            CastlingRights rights = Castling & ~(LostRights(move.From) | LostRights(move.To));
            int halfmove = type == Piece.Pawn || captured != 0 || enPassantCapture ? 0 : HalfmoveClock + 1;
            int fullmove = SideToMove == PieceColor.Black ? FullmoveNumber + 1 : FullmoveNumber;
            return new ChessPosition(next, Square.Opposite(SideToMove), rights, enPassant, halfmove, fullmove);
        }

        static CastlingRights LostRights(int square) => square switch
        {
            4 => CastlingRights.WhiteKing | CastlingRights.WhiteQueen,
            7 => CastlingRights.WhiteKing,
            0 => CastlingRights.WhiteQueen,
            60 => CastlingRights.BlackKing | CastlingRights.BlackQueen,
            63 => CastlingRights.BlackKing,
            56 => CastlingRights.BlackQueen,
            _ => CastlingRights.None
        };

        static int Count(sbyte[] board, sbyte piece)
        {
            int count = 0;
            foreach (sbyte p in board) if (p == piece) count++;
            return count;
        }

        static int ParseCounter(string text, int minimum) =>
            int.TryParse(text, out int value) && value >= minimum
                ? value
                : throw new FormatException($"Invalid move counter '{text}'.");
    }
}
