using System.Collections.Generic;
using KMA.Gameplay;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public readonly struct JourneyScoreRow
    {
        public SubjectId Subject { get; }
        public float Score { get; }
        public Rank Rank { get; }

        public JourneyScoreRow(SubjectId subject, float score, Rank rank)
        {
            Subject = subject;
            Score = score;
            Rank = rank;
        }
    }

    public sealed class JourneyCourseSummary : MonoBehaviour
    {
        static readonly SubjectId[] CourseOrder = { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football };
        static readonly string[] Labels = { "Chạy nước rút", "Bóng chuyền", "Bóng đá" };
        [SerializeField] TMP_Text summaryText;
        readonly List<JourneyScoreRow> scoreRows = new List<JourneyScoreRow>();

        public IReadOnlyList<JourneyScoreRow> ScoreRows => scoreRows;
        public int SupplementaryRounds { get; private set; }

        public void Configure(TMP_Text text) => summaryText = text;

        public void Hide() => gameObject.SetActive(false);

        public void Show(GameSession session)
        {
            if (session == null) return;
            scoreRows.Clear();
            var lines = new List<string> { "HÀNH TRÌNH TÂN SINH VIÊN HOÀN TẤT" };
            for (int index = 0; index < CourseOrder.Length; index++)
            {
                SubjectRecord record = session.GetRecord(CourseOrder[index]);
                MinigameResult best = record.BestResult;
                float score = best == null ? record.BestScore : best.Score;
                Rank rank = best == null ? record.BestRank : best.Rank;
                scoreRows.Add(new JourneyScoreRow(CourseOrder[index], score, rank));
                lines.Add($"{Labels[index]}  ·  {score:0}  ·  {rank}");
            }
            SupplementaryRounds = session.Journey.SupplementaryRounds;
            lines.Add($"Lượt thi bổ sung: {SupplementaryRounds}");
            lines.Add("Chọn một môn bên dưới để ôn tập hoặc chơi lại.");
            if (summaryText != null) summaryText.text = VietText.Fix(string.Join("\n", lines));
            gameObject.SetActive(true);
        }
    }
}
