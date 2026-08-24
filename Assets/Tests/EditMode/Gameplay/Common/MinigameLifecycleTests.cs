using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class MinigameLifecycleTests
    {
        [Test]
        public void Tick_AdvancesTutorialCountdownAndPlay()
        {
            var flow = new MinigameLifecycle(2f, 3f);

            flow.Tick(2f);
            Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Countdown));

            flow.Tick(3f);
            Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Play));

            flow.BeginResolve();
            Assert.That(flow.Phase, Is.EqualTo(MinigamePhase.Resolve));
        }
    }
}
