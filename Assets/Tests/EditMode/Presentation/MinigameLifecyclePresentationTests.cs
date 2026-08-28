using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameLifecyclePresentationTests
    {
        [Test]
        public void PhaseChangedFiresOncePerTransition()
        {
            var lifecycle = new MinigameLifecycle(1f, 2f);
            var phases = new List<MinigamePhase>();
            lifecycle.PhaseChanged += phases.Add;

            lifecycle.Tick(1f);
            lifecycle.Tick(2f);
            lifecycle.Tick(5f);

            Assert.That(phases, Is.EqualTo(new[] { MinigamePhase.Countdown, MinigamePhase.Play }));
        }

        [Test]
        public void DefaultHudStateIsEmptyAndSafe()
        {
            var gameObject = new GameObject("test-minigame");
            try
            {
                var controller = gameObject.AddComponent<TestMinigameBase>();
                Assert.That(controller.ReadHudState().statusText, Is.EqualTo(string.Empty));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        sealed class TestMinigameBase : MinigameBase
        {
            public new KMA.Gameplay.UI.MinigameHudState ReadHudState() => BuildHudState();

            protected override void TickPlay(float dt)
            {
            }
        }
    }
}
