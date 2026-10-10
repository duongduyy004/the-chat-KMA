using System.Collections.Generic;
using KMA.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        static readonly SubjectId[] CourseOrder =
            { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football, SubjectId.Chess };
        static readonly string[] Labels = { "Chạy nước rút", "Bóng chuyền", "Bóng đá", "Bài kiểm tra cuối" };
        const float ReplayWidth = 300f;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] Button replayButton;
        readonly List<JourneyScoreRow> scoreRows = new List<JourneyScoreRow>();

        public IReadOnlyList<JourneyScoreRow> ScoreRows => scoreRows;
        public int SupplementaryRounds { get; private set; }
        public Button ReplayButton => EnsureReplayButton();
        public event System.Action ReplayRequested;

        public void Configure(TMP_Text text) => summaryText = text;

        public void Hide() => gameObject.SetActive(false);

        public void Show(GameSession session)
        {
            if (session == null) return;
            scoreRows.Clear();
            var scores = new List<string>();
            for (int index = 0; index < CourseOrder.Length; index++)
            {
                SubjectRecord record = session.GetRecord(CourseOrder[index]);
                MinigameResult best = record.BestResult;
                float score = best == null ? record.BestScore : best.Score;
                Rank rank = best == null ? record.BestRank : best.Rank;
                scoreRows.Add(new JourneyScoreRow(CourseOrder[index], score, rank));
                scores.Add($"{Labels[index]}: {score.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} · {rank}");
            }
            SupplementaryRounds = session.Journey.SupplementaryRounds;
            if (summaryText != null)
            {
                // Two lines in the short strip, kept clear of the replay button on the right.
                summaryText.rectTransform.offsetMax = new Vector2(-(ReplayWidth + 20f), summaryText.rectTransform.offsetMax.y);
                summaryText.fontSize = UITheme.Shared.LessonJourney.captionSize - 8f;
                summaryText.text = VietText.Fix("HOÀN TẤT HỌC PHẦN\n" + string.Join(" | ", scores));
            }
            gameObject.SetActive(true);
            EnsureReplayButton();
        }

        Button EnsureReplayButton()
        {
            if (replayButton == null)
            {
                KMA.UI.Kit.ButtonHandle handle = KMA.UI.Kit.UiKit.Button(transform, "ReplayCelebration",
                    "Xem lại lễ mừng", KMA.UI.Kit.ButtonVariant.Primary);
                // Fill the strip's height (it is shorter than a kit button) so it never covers the map.
                var rect = (RectTransform)handle.Button.transform;
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(1f, .5f);
                rect.anchoredPosition = new Vector2(-8f, 0f);
                rect.sizeDelta = new Vector2(ReplayWidth, -8f);
                replayButton = handle.Button;
            }
            replayButton.onClick.RemoveListener(OnReplay);
            replayButton.onClick.AddListener(OnReplay);
            return replayButton;
        }

        void OnReplay() => ReplayRequested?.Invoke();
    }
}
