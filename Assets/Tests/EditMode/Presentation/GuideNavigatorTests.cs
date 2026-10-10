using System;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class GuideNavigatorTests
    {
        static readonly TutorialStep[] ThreePages =
        {
            new TutorialStep("A", "a"), new TutorialStep("B", "b"), new TutorialStep("C", "c")
        };

        [Test]
        public void FirstRunWalksForwardAndFinishesOnBatDau()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.FirstRun);
            Assert.That(navigator.Index, Is.EqualTo(0));
            Assert.That(navigator.CanGoBack, Is.False);
            Assert.That(navigator.ShowsSkip, Is.True);
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("TIẾP"));
            Assert.That(navigator.Progress, Is.EqualTo("1 / 3"));

            Assert.That(navigator.Primary(), Is.False);
            Assert.That(navigator.Primary(), Is.False);
            Assert.That(navigator.IsLast, Is.True);
            Assert.That(navigator.Current.Title, Is.EqualTo("C"));
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("BẮT ĐẦU"));
            Assert.That(navigator.ShowsSkip, Is.False, "the last page already starts the game");
            Assert.That(navigator.Primary(), Is.True, "primary on the last page closes the guide");
            Assert.That(navigator.Index, Is.EqualTo(2));
        }

        [Test]
        public void ReviewNeverSkipsAndClosesOnDong()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.Review);
            Assert.That(navigator.ShowsSkip, Is.False);
            navigator.Primary();
            navigator.Primary();
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("ĐÓNG"));
        }

        [Test]
        public void BackStopsAtTheFirstPage()
        {
            var navigator = new GuideNavigator(ThreePages, GuideMode.FirstRun);
            navigator.Back();
            Assert.That(navigator.Index, Is.EqualTo(0));
            navigator.Primary();
            Assert.That(navigator.CanGoBack, Is.True);
            navigator.Back();
            Assert.That(navigator.Index, Is.EqualTo(0));
        }

        [Test]
        public void SinglePageGuideStartsOnItsOnlyPage()
        {
            var navigator = new GuideNavigator(new[] { new TutorialStep("A", "a") }, GuideMode.FirstRun);
            Assert.That(navigator.IsLast, Is.True);
            Assert.That(navigator.PrimaryLabel, Is.EqualTo("BẮT ĐẦU"));
            Assert.That(navigator.Primary(), Is.True);
        }

        [Test]
        public void EmptyGuideIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new GuideNavigator(Array.Empty<TutorialStep>(), GuideMode.Review));
            Assert.Throws<ArgumentException>(() => new GuideNavigator(null, GuideMode.Review));
        }
    }
}
