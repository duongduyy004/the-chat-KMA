using KMA.Gameplay.FrogJump;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.FrogJump
{
    public sealed class FrogJumpGuideTests
    {
        [Test]
        public void ALifeSavingJumpSaysSo()
        {
            var pages = FrogJumpGuide.Build(new FrogJumpTuning(), true);
            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[0].Instruction, Is.EqualTo(
                "Nhảy hết 60 m trong 60 giây. Đây là thử thách bắt buộc sau khi trượt bài. Về đích để giữ mạng."));
            Assert.That(pages[1].Instruction, Is.EqualTo(
                "Kim chạy qua lại trên thanh lực. Chạm màn hình khi kim ở vùng xanh. Càng gần giữa càng nhảy xa."));
            Assert.That(pages[2].Instruction,
                Is.EqualTo("Chạm vùng đỏ là NGÃ: không tiến được và mất 2,5 giây đứng dậy."));
        }

        [Test]
        public void AJumpThatCannotSaveALifeOmitsTheSentence()
        {
            var pages = FrogJumpGuide.Build(new FrogJumpTuning(), false);
            Assert.That(pages[0].Instruction,
                Is.EqualTo("Nhảy hết 60 m trong 60 giây. Đây là thử thách bắt buộc sau khi trượt bài."));
        }

        [Test]
        public void KeyIsTheSavedFrogJumpKey() => Assert.That(FrogJumpGuide.Key, Is.EqualTo("FrogJump"));
    }
}
