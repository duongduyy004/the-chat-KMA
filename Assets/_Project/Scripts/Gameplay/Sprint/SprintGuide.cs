using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay
{
    public static class SprintGuide
    {
        public const string Key = nameof(SubjectId.Sprint);
        public const string Controls =
            "Bấm TRÁI rồi PHẢI luân phiên để chạy. Giữ nhịp đều để lên CHUỖI và BỨT TỐC. Ngừng bấm là chậm lại.";
        public const string RaceRules =
            "Bấm sai bên thì mất chuỗi và gần như không tăng tốc. Hết giờ chưa về đích là trượt. " +
            "Điểm tính theo độ chính xác, thứ hạng và thời gian còn dư.";
        // The learn lesson has no clock: a wrong side only resets the streak it counts.
        public const string LearnRules = "Bấm sai bên thì chuỗi về 0 và phải đếm lại từ đầu.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int targetCount, float distance,
            float timeLimit, int rivalCount)
        {
            bool learn = kind == ChallengeKind.Learn;
            string goal = learn
                ? $"Bấm TRÁI, PHẢI luân phiên đúng {targetCount} nhịp liên tiếp. Không tính giờ."
                : $"Chạy {MinigameGuidePages.Number(distance)} m trong {MinigameGuidePages.Number(timeLimit)} giây." +
                  (rivalCount > 0 ? $" Có {rivalCount} bạn chạy cùng." : string.Empty);
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", learn ? LearnRules : RaceRules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
