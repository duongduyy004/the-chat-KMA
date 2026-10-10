using System.Collections.Generic;
using KMA.Gameplay.UI;

namespace KMA.Gameplay.Volleyball
{
    public static class VolleyballGuide
    {
        public const string Key = nameof(SubjectId.Volleyball);
        public const string Move = "Kéo joystick để chạy tới chỗ bóng rơi.";
        public const string HitAndJump =
            "Nút ĐÁNH tự chọn giao, đỡ, chuyền, đập hoặc chắn tuỳ tình huống. " +
            "NHẢY rồi kéo joystick để nhắm hướng đập. Nút NHẢY sáng lên là lúc nên nhảy.";
        public const string Rules =
            "Bấm đúng nhịp: HOÀN HẢO, rồi TỐT, rồi SỚM/MUỘN. Mỗi bên chạm tối đa 3 lần. " +
            "Bóng không qua lưới là mất điểm.";

        public static IReadOnlyList<TutorialStep> Build(ChallengeKind? kind, int learnTarget, int pointsToWin,
            float clockLimit)
        {
            string goal;
            if (kind == ChallengeKind.Learn)
            {
                goal = $"Đỡ bóng thành công {learnTarget} lần. Đối thủ luôn giao bóng.";
            }
            else
            {
                goal = $"Ghi {pointsToWin} điểm trước đối thủ" +
                       (clockLimit > 0f ? $" trong {MinigameGuidePages.Number(clockLimit)} giây." : ".");
                // Practice keeps the serve with the opponent (VolleyballChallengeRules).
                goal += kind == ChallengeKind.Practice
                    ? " Đối thủ luôn giao bóng."
                    : " Hai bên luân phiên giao bóng.";
            }

            return MinigameGuidePages.Pages(
                new TutorialStep("MỤC TIÊU", goal),
                new TutorialStep("DI CHUYỂN", Move),
                new TutorialStep("ĐÁNH & NHẢY", HitAndJump),
                new TutorialStep("LUẬT", Rules),
                MinigameGuidePages.FailurePage(kind));
        }
    }
}
