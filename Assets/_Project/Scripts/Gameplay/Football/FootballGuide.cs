using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    public static class FootballGuide
    {
        public const string Key = nameof(SubjectId.Football);
        public const string Aim =
            "Kéo thanh hướng sang TRÁI hoặc PHẢI để chọn góc. Đường bay dự kiến hiện ra khi bạn lấy lực.";
        public const string Power =
            "Giữ nút SÚT thì lực tăng rồi giảm liên tục. Thả tay đúng lúc để sút. " +
            "Quá mạnh dễ vọt xà, quá yếu bóng dừng trước khung thành.";
        public const string Rules = "Trúng cột, trúng xà, chệch khung hay bị thủ môn cản phá đều mất lượt.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int requiredGoals, int? maxKicks,
            bool keeperEnabled)
        {
            string keeper = keeperEnabled ? "Có thủ môn" : "Không có thủ môn";
            string goal = maxKicks.HasValue
                ? $"Ghi {requiredGoals} bàn trong {maxKicks.Value} lượt sút. {keeper}."
                : $"Ghi {requiredGoals} bàn. {keeper}, sút bao nhiêu lượt cũng được.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("NGẮM", Aim),
                new TutorialStep("LỰC", Power),
                new TutorialStep("LUẬT", Rules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
