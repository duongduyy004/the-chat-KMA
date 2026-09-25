using TMPro;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballHud : MonoBehaviour
    {
        public const float FeedbackSeconds = .8f;
        public const string HintText = "Di chuyển bằng joystick  ·  Nhấn ĐÁNH để trả bóng";
        public const float HintSeconds = 3.5f;

        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text feedbackLabel;
        [SerializeField] TMP_Text hintLabel;
        [SerializeField] GameObject hintBackdrop;

        float feedbackLeft;
        float hintLeft = HintSeconds;
        MinigamePhase previousPhase = MinigamePhase.Tutorial;

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
            $"{playerPoints}  :  {opponentPoints}";

        public void ConfigureHintBackdrop(GameObject backdrop) => hintBackdrop = backdrop;

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

        public void ShowPoint(CourtSide winner)
        {
            if (!feedbackLabel)
                return;
            feedbackLabel.text = winner == CourtSide.Player ? "GHI ĐIỂM!" : "ĐỐI THỦ GHI ĐIỂM";
            feedbackLabel.enabled = true;
            feedbackLeft = 1.15f;
        }

        public void Render(VolleyballMatch match, MinigamePhase phase, float deltaTime)
        {
            if (match != null && scoreLabel)
                scoreLabel.text = ScoreText(match.PlayerPoints, match.OpponentPoints);
            if (phase != previousPhase)
            {
                if (phase == MinigamePhase.Play)
                    hintLeft = HintSeconds;
                previousPhase = phase;
            }
            if (hintLabel)
            {
                hintLabel.text = HintText;
                hintLabel.enabled = phase == MinigamePhase.Play && hintLeft > 0f;
                if (hintBackdrop)
                    hintBackdrop.SetActive(hintLabel.enabled);
            }

            if (phase == MinigamePhase.Play)
                hintLeft -= Mathf.Max(0f, deltaTime);

            if (!feedbackLabel || !feedbackLabel.enabled)
                return;

            feedbackLeft -= Mathf.Max(0f, deltaTime);
            if (feedbackLeft <= 0f)
                feedbackLabel.enabled = false;
        }
    }
}
