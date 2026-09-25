using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballSceneTests
    {
        [UnityTest]
        public IEnumerator SceneIsFullyWired()
        {
            yield return SceneManager.LoadSceneAsync("MG_Volleyball", LoadSceneMode.Single);
            yield return null;

            var controllers = Object.FindObjectsByType<VolleyballController>(FindObjectsSortMode.None);
            Assert.That(controllers, Has.Length.EqualTo(1));
            VolleyballController controller = controllers[0];
            Assert.That(controller.HasAllReferences, Is.True);
            Assert.That(controller.enabled, Is.True);

            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<VolleyAthleteView>(FindObjectsSortMode.None), Has.Length.EqualTo(2));
            Assert.That(Object.FindFirstObjectByType<EventSystem>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<InputSystemUIInputModule>(), Is.Not.Null);

            var joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            var button = Object.FindFirstObjectByType<ActionButton>();
            Assert.That(joystick, Is.Not.Null);
            Assert.That(button, Is.Not.Null);
            Assert.That(joystick.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(controller.PlayerView.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ActionPressInTheSceneTossesTheServe()
        {
            yield return SceneManager.LoadSceneAsync("MG_Volleyball", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<VolleyballController>();

            controller.SkipToPlayForTest();
            controller.Input.FeedActionForTest();
            yield return null;

            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(controller.Match.BallState, Is.EqualTo(BallState.Toss));
        }
    }
}
