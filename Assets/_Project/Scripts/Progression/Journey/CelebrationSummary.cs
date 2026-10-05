using System;
using System.Collections.Generic;

namespace KMA.Gameplay
{
    public readonly struct CelebrationRow
    {
        public string Title { get; }
        public bool Completed { get; }
        public bool HasScore { get; }
        public float Score { get; }
        public Rank Rank { get; }

        public CelebrationRow(string title, bool completed, bool hasScore, float score, Rank rank)
        {
            Title = title;
            Completed = completed;
            HasScore = hasScore;
            Score = score;
            Rank = rank;
        }
    }

    /// Read-only course recap for the celebration scene. Built from saved data only.
    public sealed class CelebrationSummary
    {
        static readonly (SubjectId Subject, string Title)[] Subjects3 =
        {
            (SubjectId.Sprint, "Chạy nước rút"), (SubjectId.Volleyball, "Bóng chuyền"), (SubjectId.Football, "Bóng đá")
        };

        public const string ChessTitle = "Bài kiểm tra cuối";

        public IReadOnlyList<CelebrationRow> Subjects { get; private set; }
        public bool ChessRecorded { get; private set; }
        public float ChessThinkSeconds { get; private set; }
        public int ChessMistakes { get; private set; }
        public bool ChessHintUsed { get; private set; }
        public int SupplementaryRounds { get; private set; }
        public bool IsSample { get; private set; }

        public static CelebrationSummary From(GameSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var rows = new List<CelebrationRow>();
            foreach ((SubjectId subject, string title) in Subjects3)
            {
                SubjectRecord record = session.GetRecord(subject);
                MinigameResult best = record.BestResult;
                rows.Add(new CelebrationRow(title, record.Passed, best != null,
                    best?.Score ?? 0f, best?.Rank ?? Rank.F));
            }
            JourneyChessRecordData chess = session.Journey.ChessBest;
            return new CelebrationSummary
            {
                Subjects = rows,
                ChessRecorded = chess.recorded,
                ChessThinkSeconds = chess.thinkSeconds,
                ChessMistakes = chess.mistakes,
                ChessHintUsed = chess.hintUsed,
                SupplementaryRounds = session.Journey.SupplementaryRounds
            };
        }

        /// Editor preview data. Scenes must never write a save while showing a sample.
        public static CelebrationSummary Sample() => new CelebrationSummary
        {
            Subjects = new[]
            {
                new CelebrationRow("Chạy nước rút", true, true, 8.4f, Rank.A),
                new CelebrationRow("Bóng chuyền", true, true, 7.1f, Rank.B),
                new CelebrationRow("Bóng đá", true, true, 9.0f, Rank.S)
            },
            ChessRecorded = true,
            ChessThinkSeconds = 42f,
            ChessMistakes = 1,
            ChessHintUsed = false,
            IsSample = true
        };

        public static string FormatClock(float seconds)
        {
            int total = (int)Math.Ceiling(Math.Max(0f, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
