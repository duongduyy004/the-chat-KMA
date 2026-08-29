using System;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class TutorialOverlayTests
    {
        [Test]
        public void TutorialCanAdvanceBackAndSkipAndMarksSubjectSeen()
        {
            var root = new GameObject("tutorial-overlay");
            try
            {
                var store = new MemoryTutorialSeenStore();
                var overlay = root.AddComponent<TutorialOverlay>();
                overlay.ConfigureForTest(store, "Sprint", new[]
                {
                    new TutorialStep("LEFT / RIGHT", "Tap the matching side."),
                    new TutorialStep("WIND", "Counter the cue before it expires.")
                });

                Assert.That(overlay.ShouldShow, Is.True);
                Assert.That(overlay.CurrentIndex, Is.EqualTo(0));
                Assert.That(overlay.CanGoBack, Is.False);
                Assert.That(overlay.CanGoNext, Is.True);

                overlay.Next();
                Assert.That(overlay.CurrentIndex, Is.EqualTo(1));
                Assert.That(overlay.CanGoBack, Is.True);
                Assert.That(overlay.CanGoNext, Is.False);

                overlay.Back();
                Assert.That(overlay.CurrentIndex, Is.EqualTo(0));
                overlay.Skip();

                Assert.That(overlay.ShouldShow, Is.False);
                Assert.That(store.HasSeen("Sprint"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AlreadySeenSubjectStartsWithoutTutorial()
        {
            var root = new GameObject("tutorial-overlay");
            try
            {
                var store = new MemoryTutorialSeenStore();
                store.MarkSeen("Sprint");
                var overlay = root.AddComponent<TutorialOverlay>();

                overlay.ConfigureForTest(store, "Sprint", new[] { new TutorialStep("x", "y") });

                Assert.That(overlay.ShouldShow, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EmptyTutorialClosesWithoutMarkingSubjectSeen()
        {
            var root = new GameObject("tutorial-overlay");
            try
            {
                var store = new MemoryTutorialSeenStore();
                var overlay = root.AddComponent<TutorialOverlay>();

                overlay.ConfigureForTest(store, "Sprint", Array.Empty<TutorialStep>());

                Assert.That(overlay.ShouldShow, Is.False);
                Assert.That(store.HasSeen("Sprint"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ClosingFinalStepMarksSeenAndResultActionOnlyReturnsPreviewRoute()
        {
            var tutorialRoot = new GameObject("tutorial-overlay");
            var resultRoot = new GameObject("result-panel");
            try
            {
                var store = new MemoryTutorialSeenStore();
                var overlay = tutorialRoot.AddComponent<TutorialOverlay>();
                overlay.ConfigureForTest(store, "Sprint", new[] { new TutorialStep("GO", "Run.") });
                overlay.Close();

                Assert.That(store.HasSeen("Sprint"), Is.True);
                Assert.That(overlay.ShouldShow, Is.False);

                var result = new MinigameResult(true, 1234.6f, Rank.A);
                var panel = resultRoot.AddComponent<ResultPanel>();
                string requestedRoute = null;
                panel.ActionRequested += route => requestedRoute = route;

                panel.Show(result, "MG_SprintPreview");
                panel.Continue();

                Assert.That(panel.CurrentResult, Is.SameAs(result));
                Assert.That(panel.PreviewRoute, Is.EqualTo("MG_SprintPreview"));
                Assert.That(requestedRoute, Is.EqualTo("MG_SprintPreview"));
                Assert.That(result.Pass, Is.True);
                Assert.That(result.Score, Is.EqualTo(1234.6f));
                Assert.That(result.Rank, Is.EqualTo(Rank.A));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tutorialRoot);
                UnityEngine.Object.DestroyImmediate(resultRoot);
            }
        }
    }
}
