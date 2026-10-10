using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuidePagesTests
    {
        [TestCase(ChallengeKind.Practice)]
        [TestCase(ChallengeKind.Exam)]
        public void PenalizedKindsGetTheFailurePage(ChallengeKind kind)
        {
            TutorialStep page = MinigameGuidePages.FailurePage(kind);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.Title, Is.EqualTo("NẾU TRƯỢT"));
            Assert.That(page.Instruction, Is.EqualTo(
                "Trượt lần đầu: phải qua Nhảy ếch để giữ mạng. Trượt từ lần hai: mất 1 mạng và vẫn phải nhảy ếch."));
        }

        [TestCase(ChallengeKind.Learn)]
        [TestCase(ChallengeKind.Final)]
        public void UnpenalizedKindsHaveNoFailurePage(ChallengeKind kind) =>
            Assert.That(MinigameGuidePages.FailurePage(kind), Is.Null);

        [Test]
        public void FreePlayHasNoFailurePage() => Assert.That(MinigameGuidePages.FailurePage(null), Is.Null);

        [Test]
        public void PagesDropsMissingPages()
        {
            var pages = MinigameGuidePages.Pages(new TutorialStep("A", "a"), null, new TutorialStep("B", "b"));
            Assert.That(pages.Count, Is.EqualTo(2));
            Assert.That(pages[1].Title, Is.EqualTo("B"));
        }

        [TestCase(150f, "150")]
        [TestCase(2.5f, "2,5")]
        [TestCase(90f, "90")]
        public void NumbersUseTheVietnameseDecimalComma(float value, string expected) =>
            Assert.That(MinigameGuidePages.Number(value), Is.EqualTo(expected));
    }
}
