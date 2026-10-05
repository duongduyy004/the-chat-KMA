using System;

namespace KMA.Gameplay.Chess
{
    public readonly struct ChessMove : IEquatable<ChessMove>
    {
        const string PromotionLetters = "nbrq"; // Knight = 2 ... Queen = 5

        public int From { get; }
        public int To { get; }
        /// Piece type to promote to, 0 when the move is not a promotion.
        public int Promotion { get; }

        public ChessMove(int from, int to, int promotion = 0)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }

        public string ToUci() => Square.Name(From) + Square.Name(To) +
            (Promotion == 0 ? string.Empty : PromotionLetters[Promotion - Piece.Knight].ToString());

        public static bool TryParseUci(string text, out ChessMove move)
        {
            move = default;
            if (text == null || (text.Length != 4 && text.Length != 5)) return false;
            int from = Square.Parse(text.Substring(0, 2));
            int to = Square.Parse(text.Substring(2, 2));
            if (from < 0 || to < 0 || from == to) return false;
            int promotion = 0;
            if (text.Length == 5)
            {
                int index = PromotionLetters.IndexOf(text[4]);
                if (index < 0) return false;
                promotion = Piece.Knight + index;
            }
            move = new ChessMove(from, to, promotion);
            return true;
        }

        public bool Equals(ChessMove other) => From == other.From && To == other.To && Promotion == other.Promotion;
        public override bool Equals(object obj) => obj is ChessMove other && Equals(other);
        public override int GetHashCode() => (From * 64 + To) * 8 + Promotion;
        public override string ToString() => ToUci();
    }
}
