using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FootballGuideTests
    {
        [Test]
        public void LearnHasNoKeeperAndNoKickLimit()
        {
            var pages = FootballGuide.Build(ChallengeKind.Learn, 3, null, false);
            Assert.That(pages.Count, Is.EqualTo(4));
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Ghi 3 bàn. Không có thủ môn, sút bao nhiêu lượt cũng được."));
            Assert.That(pages[1].Title, Is.EqualTo("NGẮM"));
            Assert.That(pages[2].Title, Is.EqualTo("LỰC"));
            Assert.That(pages[3].Title, Is.EqualTo("LUẬT"));
        }

        [Test]
        public void PracticeCountsKicksAndAddsTheFailurePage()
        {
            var pages = FootballGuide.Build(ChallengeKind.Practice, 2, 6, true);
            Assert.That(pages.Count, Is.EqualTo(5));
            Assert.That(pages[0].Instruction, Is.EqualTo("Ghi 2 bàn trong 6 lượt sút. Có thủ môn."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Kéo thanh hướng sang TRÁI hoặc PHẢI để chọn góc. Đường bay dự kiến hiện ra khi bạn lấy lực."));
            Assert.That(pages[2].Instruction, Is.EqualTo(
                "Giữ nút SÚT thì lực tăng rồi giảm liên tục. Thả tay đúng lúc để sút. " +
                "Quá mạnh dễ vọt xà, quá yếu bóng dừng trước khung thành."));
            Assert.That(pages[3].Instruction,
                Is.EqualTo("Trúng cột, trúng xà, chệch khung hay bị thủ môn cản phá đều mất lượt."));
            Assert.That(pages[4].Title, Is.EqualTo("NẾU TRƯỢT"));
        }

        [Test]
        public void FinalHasNoFailurePage() =>
            Assert.That(FootballGuide.Build(ChallengeKind.Final, 3, 5, true).Count, Is.EqualTo(4));
    }
}
