using System.Collections;
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class RunnerVisualTests
    {
        [UnityTest]
        public IEnumerator WindExpiryShowsBriefHitThenResumesPlayerAndRivalRunning()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var controller = Object.FindFirstObjectByType<SprintController>();
            controller.ConfigureForTest(.8f);
            controller.enabled = false;
            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            controller.Simulate(2.01f);
            Assert.That(controller.WindChallengeExpired, Is.True);
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Play));
            yield return null;
            yield return null;
            var player = GameObject.Find("Player").GetComponentInChildren<Animator>();
            Assert.That(player.GetCurrentAnimatorStateInfo(0).IsName("Stumble"), Is.True);
            yield return new WaitForSeconds(.6f);
            Assert.That(player.GetCurrentAnimatorStateInfo(0).IsName("Run"), Is.True,
                "A missed wind window must not leave a running player frozen in the hit pose.");
            foreach (var rival in Object.FindObjectsByType<RivalRunnerAI>(FindObjectsSortMode.None)
                .Where(r => r.RivalIndex < controller.RivalCount))
                Assert.That(rival.Animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerFramesAnimateWithoutChangingRulesAndFreezeWhenPaused()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var controller = Object.FindFirstObjectByType<SprintController>();
            controller.ConfigureForTest(0.8f);
            controller.enabled = false;
            var player = GameObject.Find("Player");
            var animator = player.GetComponentInChildren<Animator>();
            Assert.That(animator, Is.Not.Null, "The player requires a real sprite animator.");
            var renderer = animator.GetComponentInChildren<SpriteRenderer>();
            var snapshot = controller.Snapshot;
            yield return null;
            animator.Play("Run", 0, 0f);
            animator.Update(0f);
            var firstFrame = renderer.sprite;
            animator.Update(0.13f);
            Assert.That(renderer.sprite, Is.Not.EqualTo(firstFrame), "Run must change poses, not just bounce one image.");
            Assert.That(controller.Snapshot, Is.EqualTo(snapshot));
            float originalTime = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                yield return null;
                var frozen = renderer.sprite;
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(renderer.sprite, Is.EqualTo(frozen));
            }
            finally { Time.timeScale = originalTime; }
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var resetAnimator = GameObject.Find("Player").GetComponentInChildren<Animator>();
            Assert.That(resetAnimator, Is.Not.Null);
            yield return null;
            Assert.That(resetAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
        }

        [UnityTest]
        public IEnumerator SceneUsesArtworkOnEveryParallaxTileAndRunner()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var parallax = Object.FindFirstObjectByType<SprintParallax>();
            var backgrounds = parallax.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(backgrounds, Has.Length.EqualTo(6));
            foreach (var background in backgrounds)
            {
                Assert.That(background.sprite, Is.Not.Null);
                Assert.That(background.sharedMaterial, Is.Not.Null,
                    "Authored artwork must have a renderable material in the Android build.");
                Assert.That(background.sprite.texture.width, Is.GreaterThan(1000));
                Assert.That(background.bounds.size.x, Is.EqualTo(25.6f).Within(0.01f));
            }
            var rivals = Object.FindObjectsByType<RivalRunnerAI>(FindObjectsSortMode.None);
            Assert.That(rivals, Has.Length.EqualTo(3));
            foreach (var rival in rivals)
            {
                Assert.That(rival.Sprite.sprite.texture.name, Does.StartWith("Runner_"));
                Assert.That(rival.Animator.runtimeAnimatorController.animationClips
                    .Where(c => c.name.Contains("Run")), Is.Not.Empty);
            }
        }

        [UnityTest]
        public IEnumerator PlayerMapsItsOwnRaceDistanceAcrossTheTrack()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var controller = Object.FindFirstObjectByType<SprintController>();
            controller.ConfigureForTest(.8f);
            controller.enabled = false;
            var player = GameObject.Find("Player");

            controller.AdvanceToDistance(50f);
            yield return null;

            Assert.That(player.transform.position.x, Is.EqualTo(0f).Within(.001f),
                "At half race distance the player must be halfway across the authored track.");
            Assert.That(player.transform.position.y, Is.EqualTo(.7f).Within(.001f),
                "Race progress must not move the player out of lane 2.");
        }

        [UnityTest]
        public IEnumerator SprintDisablesGenericHudContentButKeepsCustomMetrics()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");

            Assert.That(GameObject.Find("SafeAreaRoot"), Is.Null,
                "The generic HUD content must not cover the Sprint presentation.");
            Assert.That(GameObject.Find("SprintMetrics"), Is.Not.Null,
                "Sprint-specific race metrics must remain visible.");
        }

        [UnityTest]
        public IEnumerator TrackLayerStaysFixedAsRunnersAdvance()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint");
            var parallax = Object.FindFirstObjectByType<SprintParallax>();
            Assert.That(parallax.TryGetLayerTilePositions(2, out var firstBefore, out var secondBefore), Is.True);

            parallax.RefreshForTest(25f);

            Assert.That(parallax.TryGetLayerTilePositions(2, out var firstAfter, out var secondAfter), Is.True);
            Assert.That(firstAfter, Is.EqualTo(firstBefore).Within(.001f));
            Assert.That(secondAfter, Is.EqualTo(secondBefore).Within(.001f));
        }
    }
}
