using System.Collections;
using KMA.Gameplay;
using UnityEngine.InputSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class SprintControllerTests
    {
        [UnityTest]
        public IEnumerator WindCue_UsesDistanceThresholdAndLeadsActivationByPointEightSeconds()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(29.9f);
            controller.Simulate(.1f);
            Assert.That(controller.WindCueVisible, Is.False);
            Assert.That(controller.WindWindowActive, Is.False);

            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            Assert.That(controller.WindCueVisible, Is.True);
            Assert.That(controller.WindWindowActive, Is.False);
            controller.Simulate(.79f);
            Assert.That(controller.WindWindowActive, Is.False);
            controller.Simulate(.01f);
            Assert.That(controller.WindWindowActive, Is.True);
            DestroyController(controller);
            yield return null;
        }
        [UnityTest]
        public IEnumerator InputSystemAsset_BindsSprintActionsAndControllerResolvesThem()
        {
            var controller = CreateSprintController(.8f);
            var asset = new InputActionAsset();
            var map = asset.AddActionMap("Sprint");
            var left = map.AddAction("SprintLeft", InputActionType.Button);
            var right = map.AddAction("SprintRight", InputActionType.Button);
            left.AddBinding("<Keyboard>/leftArrow");
            right.AddBinding("<Keyboard>/rightArrow");

            controller.ConfigureInputForTest(asset);

            Assert.That(controller.InputActionsReady, Is.True);
            Assert.That(controller.LeftInputAction, Is.EqualTo("SprintLeft"));
            Assert.That(controller.RightInputAction, Is.EqualTo("SprintRight"));
            Assert.That(left.bindings[0].path, Is.EqualTo("<Keyboard>/leftArrow"));
            Assert.That(right.bindings[0].path, Is.EqualTo("<Keyboard>/rightArrow"));
            DestroyController(controller);
            yield return null;
        }


        [UnityTest]
        public IEnumerator CorrectWindCounterplay_AllowsViableFinishAndEmitsOnePassResult()
        {
            var controller = CreateActiveWindController();
            MinigameResult result = null;
            int completions = 0;
            controller.Completed += value => { result = value; completions++; };
            controller.OnLeftTap();
            controller.AdvanceToDistance(100f);
            controller.Simulate(0f);
            Assert.That(controller.WindChallengeCountered, Is.True);
            Assert.That(controller.WindChallengeFailed, Is.False);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Pass, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Resolve));
            controller.OnLeftTap();
            controller.OnRightTap();
            controller.Simulate(1f);
            Assert.That(completions, Is.EqualTo(1));
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrongWindCounterplay_EmitsOneFailureResultEvenOnViableFinishPath()
        {
            var controller = CreateActiveWindController();
            MinigameResult result = null;
            int completions = 0;
            controller.Completed += value => { result = value; completions++; };
            controller.AdvanceToDistance(100f);
            controller.OnRightTap();
            Assert.That(controller.WindChallengeCountered, Is.False);
            Assert.That(controller.WindChallengeFailed, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Pass, Is.False);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(controller.Phase, Is.EqualTo(MinigamePhase.Resolve));
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpiredWindWindow_DoesNotAcceptLateCounterplay()
        {
            var controller = CreateActiveWindController();
            controller.Simulate(1.21f);
            Assert.That(controller.WindWindowActive, Is.False);
            Assert.That(controller.WindChallengeExpired, Is.True);
            controller.OnLeftTap();
            Assert.That(controller.WindChallengeCountered, Is.False);
            Assert.That(controller.WindChallengeFailed, Is.False);
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LargeSimulationStep_ExpiresWindWindowBeforeLateCounterplay()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            controller.Simulate(2.01f);

            Assert.That(controller.WindWindowActive, Is.False);
            Assert.That(controller.WindChallengeExpired, Is.True);
            controller.OnLeftTap();
            Assert.That(controller.WindChallengeCountered, Is.False);
            Assert.That(controller.WindChallengeFailed, Is.False);
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CueCrossingInsideLargeStep_StartsTimerAtThresholdAndExpiresAfterAuthoredDuration()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(29f);
            controller.OnLeftTap();
            controller.OnRightTap();

            controller.Simulate(1f);
            Assert.That(controller.WindCueVisible, Is.True);
            Assert.That(controller.WindWindowActive, Is.False);
            controller.Simulate(.39f);
            Assert.That(controller.WindWindowActive, Is.False);
            controller.Simulate(.02f);
            Assert.That(controller.WindWindowActive, Is.True);
            controller.Simulate(1.18f);
            Assert.That(controller.WindWindowActive, Is.True);
            controller.Simulate(.02f);
            Assert.That(controller.WindWindowActive, Is.False);
            Assert.That(controller.WindChallengeExpired, Is.True);
            controller.OnLeftTap();
            Assert.That(controller.WindChallengeCountered, Is.False);
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimeLimit_EmitsOneFailureResultWithoutStaminaDepletion()
        {
            var controller = CreateSprintController(.8f);
            MinigameResult result = null;
            int completions = 0;
            controller.Completed += value => { result = value; completions++; };
            controller.Simulate(14f);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Pass, Is.False);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(controller.Snapshot.Stamina, Is.GreaterThan(0f));
            DestroyController(controller);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SprintScene_AuthorsThreeCosmeticRivalsAndThreeLayerParallax()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint", LoadSceneMode.Single);
            yield return null;

            var scene = SceneManager.GetActiveScene();
            var rivals = SceneObjects<RivalRunnerAI>(scene);
            Assert.That(rivals.Length, Is.EqualTo(3));

            var lanes = new[] { rivals[0].Lane, rivals[1].Lane, rivals[2].Lane };
            System.Array.Sort(lanes);
            Assert.That(lanes, Is.EqualTo(new[] { 1, 3, 4 }));
            for (var i = 0; i < rivals.Length; i++)
            {
                Assert.That(rivals[i].Lane, Is.Not.EqualTo(2));
                Assert.That(rivals[i].ProfileAsset, Is.Not.Null);
                var profile = rivals[i].ProfileAsset.ToRuntime();
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.OpeningSpeed, Is.GreaterThan(0f));
                Assert.That(profile.SustainedSpeed, Is.GreaterThan(0f));
            }

            var parallax = SceneObjects<SprintParallax>(scene);
            Assert.That(parallax.Length, Is.EqualTo(1));
            Assert.That(parallax[0].LayerCount, Is.EqualTo(3));
            Assert.That(parallax[0].CoveragePixels, Is.EqualTo(new Vector2(2560f, 1080f)));
        }

        [Test]
        public void RivalVisualBurstAtSeventyPercent_IsCosmeticAndDoesNotChangeRulesDistances()
        {
            var profile = ScriptableObject.CreateInstance<RivalPaceProfileAsset>();
            profile.profileName = "Test";
            profile.openingSpeed = 8f;
            profile.sustainedSpeed = 7f;
            var runner = new GameObject("Rival").AddComponent<RivalRunnerAI>();
            runner.Configure(profile, 1, 0, null);

            var rules = new SprintRules(rivalProfiles: new[] { profile.ToRuntime() });
            rules.Tick(1f);
            var before = rules.RivalDistances;
            runner.RefreshForTest(before[0], 70f, MinigamePhase.Play, null);

            Assert.That(runner.State, Is.EqualTo(RivalRunnerState.Burst));
            Assert.That(rules.RivalDistances, Is.EqualTo(before));
            Object.DestroyImmediate(runner.gameObject);
            Object.DestroyImmediate(profile);
        }

        static SprintController CreateActiveWindController()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            controller.Simulate(.8f);
            Assert.That(controller.WindWindowActive, Is.True);
            return controller;
        }

        static SprintController CreateSprintController(float cueLeadSeconds)
        {
            var value = new GameObject("SprintController").AddComponent<SprintController>();
            value.ConfigureForTest(cueLeadSeconds);
            return value;
        }

        static void DestroyController(SprintController controller) => Object.Destroy(controller.gameObject);

        static T[] SceneObjects<T>(Scene scene) where T : Component
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matches = new System.Collections.Generic.List<T>();
            for (var i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == scene) matches.Add(all[i]);
            return matches.ToArray();
        }
    }
}
