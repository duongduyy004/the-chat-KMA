using System.Collections.Generic;
using System.Globalization;

namespace KMA.Gameplay.UI
{
    public static class MinigameGuidePages
    {
        public const string FailureTitle = "NẾU TRƯỢT";
        public const string FailureBody =
            "Trượt lần đầu: phải qua Nhảy ếch để giữ mạng. Trượt từ lần hai: mất 1 mạng và vẫn phải nhảy ếch.";

        static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        /// Practice and exam failures send the player to the frog jump (JourneyProgress.IsPenalizedKind).
        public static TutorialStep FailurePage(ChallengeKind? kind) =>
            kind == ChallengeKind.Practice || kind == ChallengeKind.Exam
                ? new TutorialStep(FailureTitle, FailureBody)
                : null;

        public static IReadOnlyList<TutorialStep> Pages(params TutorialStep[] pages)
        {
            var list = new List<TutorialStep>(pages.Length);
            foreach (TutorialStep page in pages)
                if (page != null)
                    list.Add(page);
            return list;
        }

        /// Whole numbers print bare ("150"); fractions use the Vietnamese comma ("2,5").
        public static string Number(float value) => value.ToString("0.#", Vi);
    }
}
