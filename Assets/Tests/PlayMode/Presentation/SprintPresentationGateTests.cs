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
            var sprintHud = Object.FindFirstObjectByType<SprintHud>();
            var windCue = Object.FindFirstObjectByType<SprintWindCue>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(sprintHud, Is.Not.Null);
            Assert.That(windCue, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.ShouldShow, Is.True);
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Tap the shown side"));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Counter the wind before the window closes"));
            overlay.Skip();
            Assert.That(overlay.ShouldShow, Is.False);
            Assert.That(PlayerPrefs.GetInt("KMA.tutorialSeen.Sprint", 0), Is.EqualTo(1));

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
