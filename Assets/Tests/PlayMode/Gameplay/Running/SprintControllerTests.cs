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
        public IEnumerator WindCue_PrecedesNarrowWindowByPointEightSeconds()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(29.9f);

            controller.Simulate(.1f);

            Assert.That(controller.WindCueVisible, Is.True);
            Assert.That(controller.WindWindowActive, Is.False);

            controller.Simulate(.8f);

            Assert.That(controller.WindWindowActive, Is.True);
            Object.Destroy(controller.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CorrectWindCounterplay_ClearsChallengeWithoutRandomOutcome()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(29.9f);
            controller.Simulate(.1f);
            controller.Simulate(.8f);

            controller.OnLeftTap();

            Assert.That(controller.WindChallengeCountered, Is.True);
            Assert.That(controller.WindChallengeFailed, Is.False);
            Assert.That(controller.ExpectedSide, Is.EqualTo(Side.Right));
            Object.Destroy(controller.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrongWindCounterplay_ProducesDeterministicFailureOutcome()
        {
            var controller = CreateSprintController(.8f);
            controller.AdvanceToDistance(29.9f);
            controller.Simulate(.1f);
            controller.Simulate(.8f);

            controller.OnRightTap();

            Assert.That(controller.WindChallengeCountered, Is.False);
            Assert.That(controller.WindChallengeFailed, Is.True);
            Assert.That(controller.BuildResult().Pass, Is.False);
            Object.Destroy(controller.gameObject);
            yield return null;
        }

        static SprintController CreateSprintController(float cueLeadSeconds)
        {
            var value = new GameObject("SprintController").AddComponent<SprintController>();
            value.ConfigureForTest(cueLeadSeconds);
            return value;
        }
    }
}
