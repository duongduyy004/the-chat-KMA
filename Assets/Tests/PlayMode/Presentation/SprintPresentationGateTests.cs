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
        public IEnumerator SprintSceneHasCompletePresentationContractAndKeepsTutorialStateOutOfPlayerPrefs()
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
            Assert.That(PlayerPrefs.HasKey(TutorialKey), Is.False);
            Assert.That(overlay.ShouldShow, Is.False);

            var pause = pauses[0];
            var player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null);
            var playerVisual = player.GetComponentInChildren<SpriteRenderer>();
            Assert.That(playerVisual, Is.Not.Null, "Player needs a visible child SpriteRenderer.");
            Assert.That(playerVisual.sprite, Is.Not.Null);
            var safeArea = pause.GetComponentInParent<SafeAreaFitter>();
            Assert.That(safeArea, Is.Not.Null, "Pause must be inside the safe-area hierarchy.");
            var pauseRect = pause.GetComponent<RectTransform>();
            Assert.That(pauseRect.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.pivot, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(pauseRect.anchoredPosition.y, Is.LessThan(0f));
            pause.Open();
            Assert.That(FindNamed<Button>(scene, "ResumeButton"), Is.Not.Null);
            Assert.That(FindNamed<Button>(scene, "RestartButton"), Is.Not.Null);
            Assert.That(FindNamed<Button>(scene, "ExitButton"), Is.Not.Null);
            pause.Resume();

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

            PlayerPrefs.DeleteKey(TutorialKey);
        }

        [UnityTest]
        public IEnumerator SprintPresentationRendersDedicatedHudMetricsAndWindStateTransitions()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var sprintHud = SceneObjects<SprintHud>(scene)[0];
            var windCue = SceneObjects<SprintWindCue>(scene)[0];
            var minigameHud = SceneObjects<MinigameHUD>(scene)[0];
            var distance = FindNamed<TMP_Text>(scene, "SprintDistance");
            var rank = FindNamed<TMP_Text>(scene, "SprintRank");
            var cadence = FindNamed<TMP_Text>(scene, "SprintCadence");
            var distanceFill = FindNamed<Image>(scene, "SprintDistanceFill");
            var sharedScore = FindNamed<TMP_Text>(scene, "Score");
            var sharedPhase = FindNamed<TMP_Text>(scene, "Phase");
            var sharedStatus = FindNamed<TMP_Text>(scene, "Status");

            Assert.That(sprintHud.HasBoundVisuals, Is.True);
            Assert.That(distance, Is.Not.Null);
            Assert.That(rank, Is.Not.Null);
            Assert.That(cadence, Is.Not.Null);
            Assert.That(distanceFill, Is.Not.Null);
            Assert.That(sharedScore, Is.Not.Null);
            Assert.That(distance, Is.Not.SameAs(sharedScore));
            Assert.That(rank, Is.Not.SameAs(sharedPhase));
            Assert.That(cadence, Is.Not.SameAs(sharedStatus));

            controller.ConfigureForTest(.8f);
            controller.AdvanceToDistance(42f);
            sprintHud.Refresh();
            minigameHud.RefreshFrom(controller.ReadHudState());

            Assert.That(distance.text, Is.EqualTo("42 m"));
            Assert.That(rank.text, Is.EqualTo("1st"));
            Assert.That(cadence.text, Is.EqualTo("COMBO x0"));
            Assert.That(distanceFill.fillAmount, Is.EqualTo(.42f).Within(.001f));
            Assert.That(sharedScore.text, Is.EqualTo("0"));

            var cueImage = FindNamed<Image>(scene, "WindCueHost");
            Assert.That(cueImage, Is.Not.Null);
            var cueHost = cueImage.gameObject;
            var cueState = cueHost.GetComponentInChildren<TMP_Text>(true);
            Assert.That(cueState, Is.Not.Null);
            Assert.That(cueHost.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(cueHost.GetComponentInParent<SafeAreaFitter>(), Is.Not.Null);
            Assert.That(windCue.gameObject.activeSelf, Is.True);

            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            windCue.Refresh();
            Assert.That(cueHost.activeSelf, Is.True);
            Assert.That(cueState.text, Is.EqualTo("WIND INCOMING"));
            Assert.That(cueImage.color, Is.EqualTo(Color.white));

            controller.Simulate(.8f);
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("COUNTER THE WIND NOW"));
            Assert.That(cueImage.color, Is.EqualTo(new Color(1f, .8f, 0f, 1f)));

            controller.OnLeftTap();
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("WIND COUNTERED"));
            Assert.That(cueImage.color, Is.EqualTo(Color.green));

            controller.ConfigureForTest(.8f);
            controller.AdvanceToDistance(30f);
            controller.Simulate(2.1f);
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("WIND MISSED"));
            Assert.That(cueImage.color, Is.EqualTo(Color.red));

            yield return null;
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
