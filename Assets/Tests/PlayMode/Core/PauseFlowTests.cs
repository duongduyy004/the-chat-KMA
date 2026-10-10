using System.Collections.Generic;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class PauseFlowTests
    {
        [Test]
        public void PausePanel_RestoresPreviousTimeScaleAndRaisesActionsOnce()
        {
            var root = new GameObject("PausePanel");
            var panel = root.AddComponent<PausePanel>();
            try
            {
                Time.timeScale = .5f;
                var restarts = 0;
                var exits = 0;
                panel.RestartRequested += () => restarts++;
                panel.ExitToMapRequested += () => exits++;
                panel.Open();
                Assert.That(Time.timeScale, Is.Zero);
                panel.Restart();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
                Assert.That(restarts, Is.EqualTo(1));
                panel.Open();
                panel.ExitToMap();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
                Assert.That(exits, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PausePanel_MenuCanvasRendersAboveGameplayHudAndTransitionOverlay()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var hudCanvas = canvasObject.GetComponent<Canvas>();
            hudCanvas.sortingOrder = 500;
            var root = new GameObject("PausePanel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            var panel = root.AddComponent<PausePanel>();
            try
            {
                panel.Open();

                var menu = canvasObject.transform.Find("PauseMenu");
                Assert.That(menu, Is.Not.Null);
                var menuCanvas = menu.GetComponent<Canvas>();
                Assert.That(menuCanvas, Is.Not.Null,
                    "The pause menu needs its own sorting canvas so world runners cannot draw over it.");
                Assert.That(menuCanvas.overrideSorting, Is.True);
                Assert.That(menuCanvas.sortingOrder, Is.GreaterThan(hudCanvas.sortingOrder),
                    "The pause menu must cover the volleyball HUD and controls.");
                Assert.That(menuCanvas.sortingOrder, Is.GreaterThan(800),
                    "The transition overlay currently renders at order 800.");
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(canvasObject);
            }
        }

        sealed class FakeSource : IMinigameGuideSource
        {
            public string GuideKey => "Sprint";
            public IReadOnlyList<TutorialStep> BuildGuide() => new[] { new TutorialStep("A", "a") };
        }

        static (GameObject canvas, PausePanel panel) CreatePause()
        {
            // A minigame scene loaded by an earlier test may have left its host behind.
            if (MinigameGuideHost.Current != null)
                Object.DestroyImmediate(MinigameGuideHost.Current.gameObject);
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var root = new GameObject("PausePanel", typeof(RectTransform));
            root.transform.SetParent(canvasObject.transform, false);
            return (canvasObject, root.AddComponent<PausePanel>());
        }

        static RectTransform Card(GameObject canvas) =>
            (RectTransform)canvas.transform.Find("PauseMenu/PauseCard");

        static Button GuideButton(GameObject canvas) =>
            canvas.transform.Find("PauseMenu/PauseCard/GuideButton").GetComponent<Button>();

        [Test]
        public void PausePanel_HidesTheGuideButtonWithoutAGuide()
        {
            var (canvas, panel) = CreatePause();
            try
            {
                panel.Open();
                Assert.That(panel.GuideAvailable, Is.False);
                Assert.That(GuideButton(canvas).gameObject.activeSelf, Is.False);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(440f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void PausePanel_GuideReviewKeepsTheGamePausedUntilResume()
        {
            var (canvas, panel) = CreatePause();
            var hostObject = new GameObject("Host");
            try
            {
                var host = hostObject.AddComponent<MinigameGuideHost>();
                host.Configure(new FakeSource(), new MemoryTutorialSeenStore());
                Time.timeScale = .5f;
                panel.Open();

                Button guide = GuideButton(canvas);
                Assert.That(guide.gameObject.activeSelf, Is.True);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(544f), "one more row than the 440 card");

                guide.onClick.Invoke();
                Assert.That(host.Panel.IsOpen, Is.True);
                Assert.That(host.Panel.Mode, Is.EqualTo(GuideMode.Review));
                host.Panel.PressPrimary();
                Assert.That(host.Panel.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.Zero, "closing the review returns to the paused menu");
                Assert.That(panel.IsOpen, Is.True);

                panel.Resume();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(canvas);
            }
        }

        [Test]
        public void PausePanel_FrogJumpMenuKeepsResumeAndGuide()
        {
            var (canvas, panel) = CreatePause();
            var hostObject = new GameObject("Host");
            try
            {
                hostObject.AddComponent<MinigameGuideHost>().Configure(new FakeSource(), new MemoryTutorialSeenStore());
                panel.SetLeaveOptionsVisible(false);
                panel.Open();

                Assert.That(GuideButton(canvas).gameObject.activeSelf, Is.True);
                Assert.That(canvas.transform.Find("PauseMenu/PauseCard/RestartButton").gameObject.activeSelf, Is.False);
                Assert.That(Card(canvas).sizeDelta.y, Is.EqualTo(336f));
                var resume = (RectTransform)canvas.transform.Find("PauseMenu/PauseCard/ResumeButton");
                var guide = (RectTransform)GuideButton(canvas).transform;
                Assert.That(resume.anchoredPosition.y - guide.anchoredPosition.y, Is.EqualTo(104f).Within(.01f));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(hostObject);
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
