using System.Collections.Generic;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuideHostTests
    {
        sealed class FakeSource : IMinigameGuideSource
        {
            public int Builds;
            public string GuideKey => "Sprint";

            public IReadOnlyList<TutorialStep> BuildGuide()
            {
                Builds++;
                return new[] { new TutorialStep("A", "a"), new TutorialStep("B", "b") };
            }
        }

        GameObject root;
        MinigameGuideHost host;
        FakeSource source;
        MemoryTutorialSeenStore store;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("Host");
            host = root.AddComponent<MinigameGuideHost>();
            source = new FakeSource();
            store = new MemoryTutorialSeenStore();
            host.Configure(source, store);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Time.timeScale = 1f;
        }

        [Test]
        public void FirstRunOpensOnceAndSkipMarksTheGameSeen()
        {
            Assert.That(MinigameGuideHost.Current, Is.SameAs(host));
            Assert.That(host.TryOpenFirstRun(), Is.True);
            Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(Time.timeScale, Is.Zero);

            host.Panel.PressSkip();
            Assert.That(store.HasSeen("Sprint"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(host.TryOpenFirstRun(), Is.False, "a seen game does not open by itself again");
        }

        [Test]
        public void FinishingTheFirstRunMarksTheGameSeen()
        {
            host.TryOpenFirstRun();
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            Assert.That(store.HasSeen("Sprint"), Is.True);
        }

        [Test]
        public void ReviewOpensEvenWhenSeenAndNeverMarks()
        {
            Assert.That(host.OpenReview(), Is.True);
            Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.Review));
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            Assert.That(store.HasSeen("Sprint"), Is.False);
        }

        [Test]
        public void PagesAreBuiltEachTimeTheGuideOpens()
        {
            host.OpenReview();
            host.Panel.PressPrimary();
            host.Panel.PressPrimary();
            host.OpenReview();
            Assert.That(source.Builds, Is.EqualTo(2));
        }

        [Test]
        public void CurrentClearsWhenTheHostIsDestroyed()
        {
            Object.DestroyImmediate(root);
            Assert.That(MinigameGuideHost.Current == null, Is.True);
        }
    }
}
