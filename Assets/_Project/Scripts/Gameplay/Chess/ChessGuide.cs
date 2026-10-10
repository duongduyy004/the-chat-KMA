using System.Collections.Generic;
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Chess
{
    public static class ChessGuide
    {
        public const string Key = nameof(SubjectId.Chess);
        // ChessBoardView.ClickSquare: tap a piece, then tap its destination.
        public const string Controls =
            "Chạm quân của bạn, rồi chạm ô muốn đi tới. Tốt phong cấp thì chọn quân muốn đổi. " +
            "Nút GỢI Ý có 3 mức, từ ý tưởng tới nước đi cụ thể.";

        public static IReadOnlyList<TutorialStep> Build(int maxPlayerMoves, float timeLimitSeconds, int maxMistakes)
        {
            string goal = $"Chiếu hết trong {maxPlayerMoves} nước. Thời gian suy nghĩ {TimeWords(timeLimitSeconds)}. " +
                          "Bài thi cuối không mất mạng.";
            string rules = "Nước không hợp lệ thì không tính. Đi sai bị tính 1 lỗi, bàn cờ giữ nguyên. " +
                           $"Sai quá {maxMistakes} lần hoặc hết giờ là trượt. Đồng hồ chỉ chạy trong lượt của bạn. " +
                           "Dùng gợi ý, đi sai và đi chậm đều bị trừ điểm.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", rules));
        }

        public static string TimeWords(float seconds) => Mathf.RoundToInt(seconds) switch
        {
            60 => "một phút",
            90 => "một phút rưỡi",
            120 => "hai phút",
            int s => $"{s} giây"
        };
    }
}
