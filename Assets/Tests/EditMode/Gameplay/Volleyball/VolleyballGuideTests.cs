using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballGuideTests
    {
        [Test]
        public void LearnCountsReceives()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Learn, 3, 0, 0f);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction, Is.EqualTo("Đỡ bóng thành công 3 lần. Đối thủ luôn giao bóng."));
            Assert.That(pages[1].Title, Is.EqualTo("DI CHUYỂN"));
            Assert.That(pages[1].Instruction, Is.EqualTo("Kéo joystick để chạy tới chỗ bóng rơi."));
            Assert.That(pages[2].Title, Is.EqualTo("ĐÁNH & NHẢY"));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Nút ĐÁNH tự chọn giao, đỡ, chuyền, đập hoặc chắn tuỳ tình huống. " +
                "NHẢY rồi kéo joystick để nhắm hướng đập. Nút NHẢY sáng lên là lúc nên nhảy."));
            Assert.That(pages[3].Instruction, Is.EqualTo(
                "Bấm đúng nhịp: HOÀN HẢO, rồi TỐT, rồi SỚM/MUỘN. Mỗi bên chạm tối đa 3 lần. " +
                "Bóng không qua lưới là mất điểm."));
        }

        [Test]
        public void PracticeIsARaceAgainstTheClock()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Practice, 5, 5, 120f);
            Assert.That(pages.Count, Is.EqualTo(5));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 5 điểm trước đối thủ trong 120 giây. Đối thủ luôn giao bóng."));
            Assert.That(pages[4].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void ExamHasNoClockAndAlternatesServes()
        {
            var pages = VolleyballGuide.Build(ChallengeKind.Exam, 10, 10, 0f);
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 10 điểm trước đối thủ. Hai bên luân phiên giao bóng."));
        }
    }
}
