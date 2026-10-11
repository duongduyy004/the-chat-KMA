using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class GuideBulletsTests
    {
        [Test]
        public void EverySentenceBecomesItsOwnBullet()
        {
            var items = GuideBullets.Split("Ghi 3 bàn trong 5 lượt sút. Có thủ môn. Sút bao nhiêu lượt cũng được!");
            Assert.That(items, Is.EqualTo(new[]
            {
                "Ghi 3 bàn trong 5 lượt sút.", "Có thủ môn.", "Sút bao nhiêu lượt cũng được!"
            }));
        }

        [Test]
        public void DecimalsAndLowercaseContinuationsDoNotSplit()
        {
            Assert.That(GuideBullets.Split("Chạy 1.5 km trong 20 s. rồi nghỉ."), Has.Count.EqualTo(1));
        }

        [Test]
        public void FormatHangsEachBulletAndSeparatesThemWithNewlines()
        {
            string text = GuideBullets.Format("Một. Hai.");
            Assert.That(text, Does.StartWith("<indent="));
            Assert.That(text, Does.Contain("•<pos="));
            Assert.That(text.Split('\n'), Has.Length.EqualTo(2));
        }

        [Test]
        public void EmptyTextFormatsToNothing()
        {
            Assert.That(GuideBullets.Format(null), Is.Empty);
            Assert.That(GuideBullets.Format("  "), Is.Empty);
        }
    }
}
