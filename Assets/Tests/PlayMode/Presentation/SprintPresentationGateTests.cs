using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class SprintPresentationGateTests
    {
        const string SceneName = "MG_Sprint";
        const string TutorialKey = "KMA.tutorialSeen.Sprint";

        [UnityTest]
        public IEnumerator SprintSceneHasCompletePresentationContractAndPersistsTutorialSkip()
        {
            PlayerPrefs.DeleteKey(TutorialKey);
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controllers = SceneObjects<SprintController>(scene);
            var sprintHuds = SceneObjects<SprintHud>(scene);
            var windCues = SceneObjects<SprintWindCue>(scene);
            var overlays = SceneObjects<TutorialOverlay>(scene);
            var pauses = SceneObjects<PausePanel>(scene);
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(sprintHuds.Length, Is.EqualTo(1));
            Assert.That(windCues.Length, Is.EqualTo(1));
            Assert.That(overlays.Length, Is.EqualTo(1));
            Assert.That(pauses.Length, Is.EqualTo(1));

            var sprintHud = sprintHuds[0];
            var windCue = windCues[0];
            Assert.That(sprintHud.HasBoundVisuals, Is.True, "SprintHud must bind all authored HUD labels/fills.");
            Assert.That(windCue.HasBoundVisuals, Is.True, "SprintWindCue must bind a separate host, Image, and TMP state label.");

            var overlay = overlays[0];
            Assert.That(overlay.ShouldShow, Is.True);
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Tap the shown side"));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Counter the wind before the window closes"));
            overlay.Skip();
            Assert.That(PlayerPrefs.GetInt(TutorialKey, 0), Is.EqualTo(1));
            Assert.That(overlay.ShouldShow, Is.False);

            var pause = pauses[0];
            var safeArea = pause.GetComponentInParent<SafeAreaFitter>();
            Assert.That(safeArea, Is.Not.Null, "Pause must be inside the safe-area hierarchy.");
            var pauseRect = pause.GetComponent<RectTransform>();
            Assert.That(pauseRect.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.pivot, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(pauseRect.anchoredPosition.y, Is.LessThan(0f));

            var canvas = pause.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            var left = FindNamed<ScreenTapArea>(scene, "LeftTap");
            var right = FindNamed<ScreenTapArea>(scene, "RightTap");
            Assert.That(left, Is.Not.Null);
            Assert.That(right, Is.Not.Null);
            var leftRect = left.GetComponent<RectTransform>();
            var rightRect = right.GetComponent<RectTransform>();
            Assert.That(leftRect.anchorMin.x, Is.EqualTo(0f));
            Assert.That(leftRect.anchorMax.x, Is.LessThan(rightRect.anchorMin.x));
            Assert.That(rightRect.anchorMax.x, Is.EqualTo(1f));
            Assert.That(1920f * (leftRect.anchorMax.x - leftRect.anchorMin.x) + leftRect.sizeDelta.x, Is.GreaterThanOrEqualTo(140f));
            Assert.That(1920f * (rightRect.anchorMax.x - rightRect.anchorMin.x) + rightRect.sizeDelta.x, Is.GreaterThanOrEqualTo(140f));

            yield return LoadSprint();
            var reloadedOverlay = SceneObjects<TutorialOverlay>(SceneManager.GetActiveScene());
            Assert.That(reloadedOverlay.Length, Is.EqualTo(1));
            Assert.That(reloadedOverlay[0].ShouldShow, Is.False, "Sprint tutorial skip must survive scene reload.");
            PlayerPrefs.DeleteKey(TutorialKey);
        }

        static IEnumerator LoadSprint()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        static T[] SceneObjects<T>(Scene scene) where T : Component
        {
            var all = Object.FindObjectsOfType<T>(true);
            var count = 0;
            for (var i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == scene) count++;
            var result = new T[count];
            var index = 0;
            for (var i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == scene) result[index++] = all[i];
            return result;
        }

        static T FindNamed<T>(Scene scene, string name) where T : Component
        {
            var objects = SceneObjects<T>(scene);
            for (var i = 0; i < objects.Length; i++)
                if (objects[i].name == name) return objects[i];
            return null;
        }
    }
}
