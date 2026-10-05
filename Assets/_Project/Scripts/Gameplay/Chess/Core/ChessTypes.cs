using System;

namespace KMA.Gameplay.Chess
{
    public enum PieceColor { White, Black }

    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKing = 1,
        WhiteQueen = 2,
        BlackKing = 4,
        BlackQueen = 8,
        All = 15
    }

    /// Pieces are signed bytes: positive White, negative Black, 0 empty.
    public static class Piece
    {
        public const sbyte None = 0;
        public const int Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6;
        const string Letters = " pnbrqk";

        public static int TypeOf(sbyte piece) => piece < 0 ? -piece : piece;
        public static PieceColor ColorOf(sbyte piece) => piece > 0 ? PieceColor.White : PieceColor.Black;
        public static sbyte Make(int type, PieceColor color) => (sbyte)(color == PieceColor.White ? type : -type);

        public static char ToFen(sbyte piece)
        {
            char letter = Letters[TypeOf(piece)];
            return piece > 0 ? char.ToUpperInvariant(letter) : letter;
        }

        public static sbyte FromFen(char c)
        {
            int type = Letters.IndexOf(char.ToLowerInvariant(c));
            if (type <= 0) throw new FormatException($"Unknown piece '{c}'.");
            return Make(type, char.IsUpper(c) ? PieceColor.White : PieceColor.Black);
        }
    }

    /// Square index = rank * 8 + file, a1 = 0, h8 = 63.
    public static class Square
    {
        public static int File(int square) => square & 7;
        public static int Rank(int square) => square >> 3;
        public static int At(int file, int rank) => rank * 8 + file;
        public static string Name(int square) => $"{(char)('a' + File(square))}{Rank(square) + 1}";
        public static PieceColor Opposite(PieceColor color) =>
            color == PieceColor.White ? PieceColor.Black : PieceColor.White;

        public static int Parse(string name)
        {
            if (name == null || name.Length != 2) return -1;
            int file = name[0] - 'a', rank = name[1] - '1';
            return file >= 0 && file < 8 && rank >= 0 && rank < 8 ? At(file, rank) : -1;
        }
    }
}
