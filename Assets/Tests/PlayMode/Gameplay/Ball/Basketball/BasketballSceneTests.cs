using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BasketballSceneTests
    {
        const string SceneName = "MG_Basketball";

        [UnityTest]
        public IEnumerator BasketballScene_HasPlayableControllerAndSinglePhysicsBall()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<BasketballController>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallRig>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BasketballHud>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<PlaceholderMinigameController>(scene), Is.Empty);
            Assert.That(GameObject.Find("BasketballCourt"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballHoop"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballPlayer"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballFinisher"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballDefender"), Is.Not.Null);

            var controller = SceneObjects<BasketballController>(scene)[0];
            Assert.That(controller.Ball, Is.SameAs(SceneObjects<BallRig>(scene)[0]));
            Assert.That(controller.Ball.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(controller.PlayerHand, Is.Not.Null);
            Assert.That(controller.Finisher, Is.Not.Null);
            Assert.That(controller.Ball.Profile, Is.Not.Null);
            Assert.That(controller.Ball.Profile.name, Is.EqualTo("FlightProfile_Basketball"));
        }

        // The authored rim height and the authored apex band must agree, or the visible hoop is
        // not where the scoring window is.
        [UnityTest]
        public IEnumerator BasketballScene_PutsTheRimInsideTheAuthoredApexBand()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var controller = SceneObjects<BasketballController>(SceneManager.GetActiveScene())[0];
            var hoop = GameObject.Find("BasketballHoop");

            Assert.That(hoop.transform.position.y,
                Is.InRange(controller.AuthoredBand.ApexMin, controller.AuthoredBand.ApexMax),
                "The drawn rim must sit inside the apex band the judge scores in.");
            Assert.That(controller.PlayerHand.position.y, Is.EqualTo(1.2f).Within(.01f));
            Assert.That(controller.TargetChargeMin, Is.GreaterThan(0f));
            Assert.That(controller.TargetChargeMax, Is.GreaterThan(controller.TargetChargeMin),
                "The authored launch height must leave a reachable charge band.");
        }

        [UnityTest]
        public IEnumerator BasketballScene_RoutesGameplayThroughOneSharedInputRouterAndSurface()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<GameplayInputRouter>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<ScreenTapArea>(scene), Has.Length.EqualTo(1));

            var controller = SceneObjects<BasketballController>(scene)[0];
            var router = SceneObjects<GameplayInputRouter>(scene)[0];
            var surface = SceneObjects<ScreenTapArea>(scene)[0];
            Assert.That(controller.InputRouter, Is.SameAs(router));
            Assert.That(surface.Router, Is.SameAs(router));
            Assert.That(controller.HasProductionDetectors, Is.True);
            Assert.That(router.InputActions, Is.Not.Null,
                "Basketball must have a keyboard fallback, unlike the S9 Volleyball scene.");

            Assert.That(EventSystem.current, Is.Not.Null);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * .5f, Screen.height * .5f)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<ScreenTapArea>(), Is.SameAs(surface),
                "The top raycast hit at screen centre must belong to the gameplay surface. Blocker: " + hits[0].gameObject.name);
        }

        [UnityTest]
        public IEnumerator BasketballScene_ReferencesTheS8PresentationKitWithoutDuplicatingIt()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<TrajectoryPreview>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<BallShadow>(scene), Has.Length.EqualTo(1));

            var controller = SceneObjects<BasketballController>(scene)[0];
            Assert.That(controller.Preview, Is.SameAs(SceneObjects<TrajectoryPreview>(scene)[0]));
            Assert.That(controller.Shadow, Is.SameAs(SceneObjects<BallShadow>(scene)[0]));
            Assert.That(controller.Preview.Source, Is.SameAs(controller.Ball));
            Assert.That(controller.Preview.Line, Is.Not.Null);
            Assert.That(controller.Shadow.Target, Is.SameAs(controller.Ball.transform));
        }

        [UnityTest]
        public IEnumerator BasketballScene_ShowsGenericAndBasketballHudLabels()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<MinigameHUD>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<PausePanel>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<ResultPanel>(scene), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("GameCamera"), Is.Not.Null);

            var controller = SceneObjects<BasketballController>(scene)[0];
            var hud = SceneObjects<BasketballHud>(scene)[0];
            var sharedHud = SceneObjects<MinigameHUD>(scene)[0];
            Assert.That(hud.Controller, Is.SameAs(controller));

            yield return null;

            Assert.That(sharedHud.LastState.phase, Is.EqualTo(controller.PresentationPhase.ToString()));
            Assert.That(sharedHud.LastState.timeRemaining, Is.GreaterThan(0f));

            hud.Refresh();

            Assert.That(hud.ScoreText, Is.EqualTo("BASKETS 0/5"));
            Assert.That(hud.AttemptsText, Is.EqualTo("ATTEMPTS 0"));
            Assert.That(hud.ComboText, Is.EqualTo("COMBO 0"));
            Assert.That(hud.JudgeText, Is.Empty);
            Assert.That(hud.ChargeText, Is.EqualTo("AIM"));
            Assert.That(LabelText("BasketballScoreLabel"), Is.EqualTo(hud.ScoreText));
            Assert.That(LabelText("BasketballAttemptsLabel"), Is.EqualTo(hud.AttemptsText));
            Assert.That(LabelText("BasketballComboLabel"), Is.EqualTo(hud.ComboText));
            Assert.That(LabelText("BasketballJudgeLabel"), Is.EqualTo(hud.JudgeText));
            Assert.That(LabelText("BasketballChargeLabel"), Is.EqualTo(hud.ChargeText));
            Assert.That(GameObject.Find("BasketballApexRing"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballApexZone"), Is.Not.Null);
            Assert.That(GameObject.Find("BasketballChargeTargetBand"), Is.Not.Null);
        }

        // S9 shipped a scene whose colliders were invisible; do not repeat it.
        [UnityTest]
        public IEnumerator BasketballScene_RendersTheCourtHoopActorsAndBall()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            var controller = SceneObjects<BasketballController>(SceneManager.GetActiveScene())[0];
            AssertVisible("BasketballCourt");
            AssertVisible("BasketballHoop");
            AssertVisible("BasketballBackboard");
            AssertVisible("BasketballPlayer");
            AssertVisible("BasketballFinisher");
            AssertVisible("BasketballDefender");

            var ballIsDrawn = false;
            foreach (SpriteRenderer renderer in controller.Ball.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.enabled && renderer.sprite != null &&
                    renderer.gameObject != controller.Shadow.Shadow.gameObject)
                    ballIsDrawn = true;
            }

            Assert.That(ballIsDrawn, Is.True, "The ball itself must be drawn, not only its shadow.");

            var court = GameObject.Find("BasketballCourt").GetComponent<SpriteRenderer>();
            Assert.That(court.bounds.size.x, Is.EqualTo(18f).Within(.05f),
                "Authored sizes are world units; the built-in sprite is 0.16 units, so localScale = size / 0.16.");
        }

        [UnityTest]
        public IEnumerator BasketballScene_TeachesHoldAimFinishThroughTheSharedTutorialOverlay()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            Assert.That(SceneObjects<PhaseOverlay>(scene), Has.Length.EqualTo(1));
            Assert.That(SceneObjects<TutorialOverlay>(scene), Has.Length.EqualTo(1));

            var overlay = SceneObjects<TutorialOverlay>(scene)[0];
            Assert.That(overlay.CurrentStep, Is.Not.Null);
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("HOLD"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Hold to charge the lob."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("AIM"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Release inside the glowing charge band."));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Title, Is.EqualTo("FINISH"));
            Assert.That(overlay.CurrentStep.Instruction, Is.EqualTo("Tap when the ball reaches the apex ring."));
            Assert.That(overlay.CanGoNext, Is.False, "Three steps, as the spec requires for a complex mechanic.");
        }

        static void AssertVisible(string objectName)
        {
            var root = GameObject.Find(objectName);
            Assert.That(root, Is.Not.Null, objectName + " must exist in the Basketball scene.");
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            Assert.That(renderers, Is.Not.Empty, objectName + " must be visible on screen.");
            foreach (SpriteRenderer renderer in renderers)
            {
                Assert.That(renderer.sprite, Is.Not.Null, objectName + " has a renderer with no sprite.");
                Assert.That(renderer.enabled, Is.True, objectName + " has a disabled renderer.");
            }
        }

        static string LabelText(string objectName)
        {
            var labelObject = GameObject.Find(objectName);
            Assert.That(labelObject, Is.Not.Null, objectName + " must exist in the Basketball scene.");
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
