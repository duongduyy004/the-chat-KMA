using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay.FrogJump
{
    public static class FrogJumpGuide
    {
        public const string Key = SaveDataTutorialSeenStore.FrogJumpKey;
        public const string Controls =
            "Kim chạy qua lại trên thanh lực. Chạm màn hình khi kim ở vùng xanh. Càng gần giữa càng nhảy xa.";

        /// savesLife: a finish keeps the life (JourneyProgress only charges SavesLife && !reachedFinish).
        public static IReadOnlyList<TutorialStep> Build(FrogJumpTuning tuning, bool savesLife)
        {
            string goal =
                $"Nhảy hết {MinigameGuidePages.Number(tuning.trackMetres)} m trong " +
                $"{MinigameGuidePages.Number(tuning.timeLimitSeconds)} giây. " +
                "Đây là thử thách bắt buộc sau khi trượt bài." + (savesLife ? " Về đích để giữ mạng." : string.Empty);
            string rules =
                $"Chạm vùng đỏ là NGÃ: không tiến được và mất {MinigameGuidePages.Number(tuning.recoverSeconds)} giây đứng dậy.";
            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("ĐIỀU KHIỂN", Controls),
                new TutorialStep("LUẬT", rules));
        }
    }
}
