using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
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
    }
}
