using TMPro;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballHud : MonoBehaviour
    {
        public const float FeedbackSeconds = .8f;
        public const string HintText = "Cần gạt trái: di chuyển · Nút phải: chạm bóng đúng lúc";

        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text feedbackLabel;
        [SerializeField] TMP_Text hintLabel;

        float feedbackLeft;

        public void Configure(TMP_Text score, TMP_Text feedback, TMP_Text hint)
        {
            scoreLabel = score;
            feedbackLabel = feedback;
            hintLabel = hint;
            if (hintLabel)
                hintLabel.text = HintText;
            if (feedbackLabel)
                feedbackLabel.enabled = false;
        }

        public static string ScoreText(int playerPoints, int opponentPoints) =>
            $"BẠN {playerPoints} – {opponentPoints} MÁY";

        public static string FeedbackText(TimingGrade grade, float offset) => grade switch
        {
            TimingGrade.Perfect => "PERFECT",
            TimingGrade.Good => "GOOD",
            TimingGrade.Late => offset < 0f ? "EARLY" : "LATE",
            _ => string.Empty
        };

        public void ShowFeedback(ActionDecision decision)
        {
            if (!decision.IsTimed || !feedbackLabel)
                return;

            feedbackLabel.text = FeedbackText(decision.Grade, decision.Offset);
            feedbackLabel.enabled = true;
            feedbackLeft = FeedbackSeconds;
        }

        public void Render(VolleyballMatch match, MinigamePhase phase, float deltaTime)
        {
            if (match != null && scoreLabel)
                scoreLabel.text = ScoreText(match.PlayerPoints, match.OpponentPoints);
            if (hintLabel)
            {
                hintLabel.text = HintText;
                hintLabel.enabled = phase == MinigamePhase.Tutorial;
            }

            if (!feedbackLabel || !feedbackLabel.enabled)
                return;

            feedbackLeft -= Mathf.Max(0f, deltaTime);
            if (feedbackLeft <= 0f)
                feedbackLabel.enabled = false;
        }
    }
}
