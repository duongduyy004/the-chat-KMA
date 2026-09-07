using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class VolleyballSceneTests
    {
        const string SceneName = "MG_Volleyball";

        [UnityTest]
        public IEnumerator VolleyballScene_HasPlayableControllerAndSinglePhysicsBall()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<VolleyballController>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallRig>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<VolleyballHud>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<PlaceholderMinigameController>(scene), Is.Empty);
            Assert.That(GameObject.Find("VolleyballCourt"), Is.Not.Null);
            Assert.That(GameObject.Find("VolleyballNet"), Is.Not.Null);
            Assert.That(GameObject.Find("VolleyballPlayer"), Is.Not.Null);
            Assert.That(GameObject.Find("VolleyballTeammate"), Is.Not.Null);
            Assert.That(GameObject.Find("VolleyballOpponent"), Is.Not.Null);

            var controller = SceneObjects<VolleyballController>(scene)[0];
            Assert.That(controller.Ball, Is.SameAs(SceneObjects<BallRig>(scene)[0]));
            Assert.That(controller.Ball.GetComponent<Rigidbody2D>(), Is.Not.Null,
                "The single authored ball must be the physics body BallRig/Ballistics own.");
            Assert.That(controller.ReachZone, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator VolleyballScene_RoutesGameplayThroughOneSharedInputRouterAndSurface()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<GameplayInputRouter>(scene), Has.Length.EqualTo(1),
                "Exactly one GameplayInputRouter may feed the Volleyball detectors.");
            Assert.That(SceneObjects<ScreenTapArea>(scene), Has.Length.EqualTo(1),
                "Only the shared router-owned gameplay surface may read Volleyball gestures.");

            var controller = SceneObjects<VolleyballController>(scene)[0];
            var router = SceneObjects<GameplayInputRouter>(scene)[0];
            var surface = SceneObjects<ScreenTapArea>(scene)[0];
            Assert.That(controller.InputRouter, Is.SameAs(router));
            Assert.That(surface.Router, Is.SameAs(router));
            Assert.That(controller.InstalledSwipeDetector, Is.Not.Null,
                "The controller owns the router's swipe detector because the scene has no bridge.");
        }

        [UnityTest]
        public IEnumerator VolleyballScene_ReferencesTheS8PresentationKitWithoutDuplicatingIt()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<TrajectoryPreview>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallShadow>(scene), Has.Length.EqualTo(1));

            var controller = SceneObjects<VolleyballController>(scene)[0];
            var preview = SceneObjects<TrajectoryPreview>(scene)[0];
            var shadow = SceneObjects<BallShadow>(scene)[0];
            Assert.That(controller.Preview, Is.SameAs(preview));
            Assert.That(controller.Shadow, Is.SameAs(shadow));
            Assert.That(preview.Source, Is.SameAs(controller.Ball));
            Assert.That(preview.Line, Is.Not.Null);
            Assert.That(shadow.Target, Is.SameAs(controller.Ball.transform));
            Assert.That(shadow.Shadow, Is.Not.Null);
            Assert.That(shadow.Renderer, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator VolleyballScene_ShowsGenericAndVolleyballHudLabels()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<MinigameHUD>(scene), Has.Length.EqualTo(1),
                "The generic lifecycle/timer HUD stays shared.");
            Assert.That(SceneObjects<PausePanel>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<ResultPanel>(scene), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("GameCamera"), Is.Not.Null);

            var hud = SceneObjects<VolleyballHud>(scene)[0];
            var controller = SceneObjects<VolleyballController>(scene)[0];
            Assert.That(hud.Controller, Is.SameAs(controller));
            Assert.That(hud.LabelsBound, Is.True, "Every Volleyball HUD label must be authored in the scene.");

            hud.Refresh();

            Assert.That(hud.TouchText, Is.EqualTo("TOUCH 1/3"));
            Assert.That(hud.ScoreText, Is.EqualTo("0 - 0"));
            Assert.That(hud.ComboText, Is.EqualTo("COMBO 0"));
            Assert.That(hud.ContextText, Is.Not.Empty);
            Assert.That(hud.TimingText, Is.Not.Empty);
            Assert.That(LabelText("VolleyballTouchLabel"), Is.EqualTo(hud.TouchText));
            Assert.That(LabelText("VolleyballScoreLabel"), Is.EqualTo(hud.ScoreText));
            Assert.That(LabelText("VolleyballComboLabel"), Is.EqualTo(hud.ComboText));
            Assert.That(LabelText("VolleyballContextLabel"), Is.EqualTo(hud.ContextText));
            Assert.That(LabelText("VolleyballTimingLabel"), Is.EqualTo(hud.TimingText));
            Assert.That(LabelText("VolleyballCounterCueLabel"), Is.EqualTo(hud.CounterCueText));
        }

        [UnityTest]
        public IEnumerator VolleyballScene_TeachesDigSetSpikeThroughTheSharedTutorialOverlay()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<PhaseOverlay>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<TutorialOverlay>(scene), Has.Length.EqualTo(1));

            var overlay = SceneObjects<TutorialOverlay>(scene)[0];
            Assert.That(overlay.CurrentStep, Is.Not.Null,
                "The Volleyball tutorial steps must be authored through the shared overlay.");
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("DIG"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Swipe down when the ball is low."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("SET"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Swipe up while the ball is rising."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("SPIKE"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Swipe toward the net near the apex."));
        }

        static string LabelText(string objectName)
        {
            var labelObject = GameObject.Find(objectName);
            Assert.That(labelObject, Is.Not.Null, objectName + " must exist in the Volleyball scene.");
            var label = labelObject.GetComponent<TMP_Text>();
            Assert.That(label, Is.Not.Null, objectName + " must carry a TMP_Text component.");
            return label.text;
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
    }
}
