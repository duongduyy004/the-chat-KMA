using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

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
    }
}
