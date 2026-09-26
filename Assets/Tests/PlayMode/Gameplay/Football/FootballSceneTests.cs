using System.Collections;
using System.IO;
using UnityEngine.UI;
using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballSceneTests
    {
        const string SceneName = "MG_Football";

        [UnityTest]
        public IEnumerator SavedFieldCoversWideAndStandardCameraWithoutScalingActors()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            var camera = Camera.main;
            var field = GameObject.Find("FootballWorld/Field").GetComponent<SpriteRenderer>();
            var player = GameObject.Find("FootballWorld/Player").transform;
            var goal = GameObject.Find("FootballWorld/Goal").transform;
            var playerScale = player.localScale;
            var goalScale = goal.localScale;
            foreach (float aspect in new[] { 1230f / 570f, 16f / 9f, 4f / 3f })
            {
                camera.aspect = aspect;
                yield return null;
                yield return null;
                var bounds = field.bounds;
                var bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, 10f));
                var topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, 10f));
                Assert.That(bounds.min.x, Is.LessThanOrEqualTo(bottomLeft.x + .01f));
                Assert.That(bounds.max.x, Is.GreaterThanOrEqualTo(topRight.x - .01f));
                Assert.That(bounds.min.y, Is.LessThanOrEqualTo(bottomLeft.y + .01f));
                Assert.That(bounds.max.y, Is.GreaterThanOrEqualTo(topRight.y - .01f));
                Assert.That(player.localScale, Is.EqualTo(playerScale));
                Assert.That(goal.localScale, Is.EqualTo(goalScale));
            }
            camera.ResetAspect();
            var scene = SceneManager.GetSceneByName(SceneName);
            SceneManager.SetActiveScene(SceneManager.CreateScene("FootballCoverageCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator SavedSceneSliderHoldReleaseMatchesPreviewVisibility()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            var scene = SceneManager.GetSceneByName(SceneName);
            FootballController controller = null;
            FootballHud hud = null;
            FootballPresentation view = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                controller ??= root.GetComponentInChildren<FootballController>(true);
                hud ??= root.GetComponentInChildren<FootballHud>(true);
                view ??= root.GetComponentInChildren<FootballPresentation>(true);
            }
            Assert.That(controller.BeginMatch(FootballDifficulty.Normal), Is.True);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (controller.Rules.State != FootballState.Aiming && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.Rules.State, Is.EqualTo(FootballState.Aiming));
            var crosshair = view.transform.Find("AimCrosshair").GetComponent<SpriteRenderer>();
            var dot = view.transform.Find("TrajectoryDot0").GetComponent<SpriteRenderer>();
            hud.DirectionSlider.value = .55f;
            yield return null;
            Assert.That(controller.Rules.AimX, Is.EqualTo(.55f));
            Assert.That(crosshair.enabled, Is.False);
            Directory.CreateDirectory("Builds/Screenshots/football-flight");
            if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Builds/Screenshots/football-flight/idle.png");
            yield return null;
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 73, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(hud.ShootButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(1f);
            Assert.That(controller.Rules.State, Is.EqualTo(FootballState.Charging));
            Assert.That(crosshair.enabled, Is.True); Assert.That(dot.enabled, Is.True);
            Assert.That(hud.DirectionSlider.interactable, Is.False);
            if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Builds/Screenshots/football-flight/charging.png");
            yield return null;
            ExecuteEvents.Execute(hud.ShootButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(crosshair.enabled, Is.False, "Release must hide the preview before the next render frame");
            Assert.That(dot.enabled, Is.False);
            yield return new WaitForSeconds(.4f);
            Assert.That(crosshair.enabled, Is.False); Assert.That(dot.enabled, Is.False);
            Assert.That(controller.Rules.State, Is.EqualTo(FootballState.Flying));
            if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Builds/Screenshots/football-flight/flight.png");
            deadline = Time.realtimeSinceStartup + 15f;
            while (!controller.Rules.Flight.Outcome.HasValue && Time.realtimeSinceStartup < deadline) yield return null;
            if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("Builds/Screenshots/football-flight/outcome.png");
            deadline = Time.realtimeSinceStartup + 15f;
            while (controller.Rules.Kicks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(controller.Rules.Kicks, Is.EqualTo(1));
            SceneManager.SetActiveScene(SceneManager.CreateScene("FootballQaCleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator SavedSceneContainsPlayableSourcedFootballPresentation()
        {
            var operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
            var scene = SceneManager.GetSceneByName(SceneName);
            Assert.That(scene.IsValid(), Is.True);
            int controllers = 0, placeholders = 0, results = 0, owners = 0;
            FootballPresentation presentation = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                controllers += root.GetComponentsInChildren<FootballController>(true).Length;
                placeholders += root.GetComponentsInChildren<PlaceholderMinigameController>(true).Length;
                results += root.GetComponentsInChildren<MonoBehaviour>(true).OfTypeResultPanels();
                owners += root.GetComponentsInChildren<MinigamePresentationOwner>(true).Length;
                presentation ??= root.GetComponentInChildren<FootballPresentation>(true);
            }
            Assert.That(controllers, Is.EqualTo(1));
            Assert.That(placeholders, Is.Zero);
            Assert.That(results, Is.EqualTo(1));
            Assert.That(owners, Is.EqualTo(1));
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.ValidateReferences(), Is.True);
            Assert.That(presentation.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty);
            var eventSystem = System.Array.Find(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), item => item.gameObject.scene == scene);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            var text = System.Array.Find(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), item => item.gameObject.scene == scene);
            Assert.That(text, Is.Not.Null);
            Assert.That(text.font, Is.Not.Null);
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    static class FootballSceneTestExtensions
    {
        public static int OfTypeResultPanels(this MonoBehaviour[] components)
        {
            int count = 0;
            foreach (var component in components) if (component is IResultPreviewPanel) count++;
            return count;
        }
    }
}
