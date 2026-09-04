using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class EndurancePresentationGateTests
    {
        const string SceneName = "MG_Endurance";
        const string TutorialKey = "KMA.tutorialSeen.Endurance";

        [UnityTest]
        public IEnumerator EnduranceSceneHasOneAuthoritativeInputPathAndPresentationContract()
        {
            PlayerPrefs.DeleteKey(TutorialKey);
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<EnduranceController>(scene).Length, Is.EqualTo(1));
            Assert.That(SceneObjects<GameplayInputRouter>(scene).Length, Is.EqualTo(1));
            Assert.That(SceneObjects<EnduranceInputBridge>(scene).Length, Is.EqualTo(1));
            Assert.That(SceneObjects<MinigameHUD>(scene).Length, Is.EqualTo(1));
            Assert.That(SceneComponentsNamed(scene, "EnduranceHud"), Has.Length.EqualTo(1));
            Assert.That(SceneComponentsNamed(scene, "EnduranceBeatRing"), Has.Length.EqualTo(1));
            Assert.That(SceneComponentsNamed(scene, "EnduranceObstacleCue"), Has.Length.EqualTo(1));
            Assert.That(SceneComponentsNamed(scene, "EnduranceParallax"), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<TutorialOverlay>(scene).Length, Is.EqualTo(1));
            Assert.That(SceneObjects<PausePanel>(scene).Length, Is.EqualTo(1));
            Assert.That(GameObject.Find("GameCamera"), Is.Not.Null);
            Assert.That(SceneObjects<ScreenTapArea>(scene).Length, Is.EqualTo(1),
                "Only the shared router-owned gameplay surface may read Endurance taps.");

            var overlay = SceneObjects<TutorialOverlay>(scene)[0];
            Assert.That(overlay.ShouldShow, Is.True);
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Tap on the beat"));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Hold to recover stamina"));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Swipe up/down to clear obstacles"));
            overlay.Skip();
            Assert.That(PlayerPrefs.HasKey(TutorialKey), Is.False);
            PlayerPrefs.DeleteKey(TutorialKey);
        }

        static T[] SceneObjects<T>(Scene scene) where T : Component
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var count = 0;
            for (var index = 0; index < all.Length; index++)
                if (all[index].gameObject.scene == scene) count++;

            var result = new T[count];
            var resultIndex = 0;
            for (var index = 0; index < all.Length; index++)
                if (all[index].gameObject.scene == scene) result[resultIndex++] = all[index];
            return result;
        }

        static Component[] SceneComponentsNamed(Scene scene, string componentName)
        {
            var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var count = 0;
            for (var index = 0; index < all.Length; index++)
                if (all[index].gameObject.scene == scene && all[index].GetType().Name == componentName) count++;

            var result = new Component[count];
            var resultIndex = 0;
            for (var index = 0; index < all.Length; index++)
                if (all[index].gameObject.scene == scene && all[index].GetType().Name == componentName) result[resultIndex++] = all[index];
            return result;
        }
    }
}
