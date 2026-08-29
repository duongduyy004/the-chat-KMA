using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class SprintPresentationGateTests
    {
        [UnityTest]
        public IEnumerator SprintSceneShowsTutorialCountdownHudAndInputResponse()
        {
            PlayerPrefs.DeleteKey("KMA.tutorialSeen.Sprint");
            yield return SceneManager.LoadSceneAsync("MG_Sprint", LoadSceneMode.Single);

            var controller = Object.FindFirstObjectByType<SprintController>();
            var hud = Object.FindFirstObjectByType<MinigameHUD>();
            var overlay = Object.FindFirstObjectByType<TutorialOverlay>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.ShouldShow, Is.True);

            yield return new WaitForSeconds(2.1f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));

            yield return new WaitForSeconds(3.1f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));

            var before = controller.Snapshot.Distance;
            if (controller.ExpectedSide == Side.Left)
                controller.OnLeftTap();
            else
                controller.OnRightTap();
            yield return null;
            Assert.That(controller.Snapshot.Distance, Is.GreaterThan(before));
            Assert.That(hud.LastState.statusText, Does.Contain("TAP").Or.Contain("WIND"));
        }
    }
}
