using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class MinigameLifecycleContractTests
    {
        [Test]
        public void BeginResolve_RejectsTutorialAndCountdown()
        {
            var flow = new MinigameLifecycle(2f, 3f);

            Assert.That(flow.BeginResolve(), Is.False);
            Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Tutorial));

            flow.Tick(2f);
            Assert.That(flow.BeginResolve(), Is.False);
            Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Countdown));
        }

        [Test]
        public void Finish_EmitsOnlyOnceForSuccessfulPlayToResolve()
        {
            var gameObject = new GameObject("minigame-test");
            var minigame = gameObject.AddComponent<TestMinigame>();
            minigame.Initialize();
            var first = new MinigameResult(true, 8.5f, Rank.A);
            var second = new MinigameResult(false, 0f, Rank.F);
            var emitted = 0;
            MinigameResult completed = null;
            minigame.Completed += result =>
            {
                emitted++;
                completed = result;
            };

            minigame.Complete(first);
            minigame.AdvanceToPlay();
            minigame.Complete(first);
            minigame.Complete(second);

            Assert.That(emitted, Is.EqualTo(1));
            Assert.That(completed, Is.SameAs(first));
            Assert.That(minigame.CurrentPhase, Is.EqualTo(MinigamePhase.Resolve));
            Object.DestroyImmediate(gameObject);
        }

        sealed class TestMinigame : MinigameBase
        {
            public MinigamePhase CurrentPhase => Lifecycle.Phase;

            public void Initialize() => Awake();

            public void AdvanceToPlay()
            {
                Lifecycle.Tick(2f);
                Lifecycle.Tick(3f);
            }

            public void Complete(MinigameResult result) => Finish(result);

            protected override void TickPlay(float dt) { }
        }
    }
}
