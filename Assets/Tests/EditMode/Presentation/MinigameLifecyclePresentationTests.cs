using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameLifecyclePresentationTests
    {
        [Test]
        public void PhaseChangedFiresOnceForEachLifecycleTransition()
        {
            var lifecycle = new MinigameLifecycle(1f, 2f);
            var phases = new List<MinigamePhase>();
            lifecycle.PhaseChanged += phases.Add;

            lifecycle.Tick(1f);
            Assert.That(lifecycle.Phase, Is.EqualTo(MinigamePhase.Countdown));
            lifecycle.Tick(2f);
            Assert.That(lifecycle.Phase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(lifecycle.BeginResolve(), Is.True);
            Assert.That(lifecycle.Phase, Is.EqualTo(MinigamePhase.Resolve));
            Assert.That(lifecycle.BeginResolve(), Is.False);

            Assert.That(phases, Is.EqualTo(new[]
            {
                MinigamePhase.Countdown,
                MinigamePhase.Play,
                MinigamePhase.Resolve
            }));
        }

        [Test]
        public void MinigameHudStateIsImmutableAndDeclaresExactlySixDataFields()
        {
            var fields = typeof(MinigameHudState)
                .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OrderBy(field => field.MetadataToken)
                .ToArray();

            Assert.That(fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "phase",
                "timeRemaining",
                "progress01",
                "stamina01",
                "score",
                "statusText"
            }));
            Assert.That(fields.All(field => field.IsInitOnly), Is.True);
            Assert.That(typeof(MinigameHudState).GetField("Empty", BindingFlags.Public | BindingFlags.Static), Is.Null);
            Assert.That(typeof(MinigameHudState).GetProperty("Empty", BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
            Assert.That(MinigameHudState.Empty.statusText, Is.EqualTo(string.Empty));
        }

        [Test]
        public void MinigameBaseUsesSerializedTwoAndThreeSecondDefaults()
        {
            var gameObject = new GameObject("test-minigame");
            try
            {
                var controller = gameObject.AddComponent<TestMinigameBase>();
                var tutorialField = typeof(MinigameBase).GetField("tutorialSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
                var countdownField = typeof(MinigameBase).GetField("countdownSeconds", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(tutorialField, Is.Not.Null);
                Assert.That(countdownField, Is.Not.Null);
                Assert.That(tutorialField.IsDefined(typeof(SerializeField), false), Is.True);
                Assert.That(countdownField.IsDefined(typeof(SerializeField), false), Is.True);
                Assert.That(tutorialField.GetValue(controller), Is.EqualTo(2f));
                Assert.That(countdownField.GetValue(controller), Is.EqualTo(3f));

                Assert.That(controller.CurrentPhase, Is.EqualTo(MinigamePhase.Tutorial));
                controller.AdvanceLifecycle(2f);
                Assert.That(controller.CurrentPhase, Is.EqualTo(MinigamePhase.Countdown));
                controller.AdvanceLifecycle(3f);
                Assert.That(controller.CurrentPhase, Is.EqualTo(MinigamePhase.Play));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        sealed class TestMinigameBase : MinigameBase
        {
            public MinigamePhase CurrentPhase => Lifecycle.Phase;

            public void AdvanceLifecycle(float deltaSeconds) => Lifecycle.Tick(deltaSeconds);

            protected override void TickPlay(float dt)
            {
            }
        }
    }
}
