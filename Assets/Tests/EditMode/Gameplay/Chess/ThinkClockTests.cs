using KMA.Gameplay.Chess;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Chess
{
    public sealed class ThinkClockTests
    {
        [Test]
        public void OnlyRunningTimeCountsAndExpiryFiresOnce()
        {
            var clock = new ThinkClock(90f);
            Assert.That(clock.Tick(10f), Is.False);
            Assert.That(clock.Elapsed, Is.EqualTo(0f));
            clock.Start();
            Assert.That(clock.Tick(30f), Is.False);
            clock.Stop();
            clock.Tick(100f);
            Assert.That(clock.Remaining, Is.EqualTo(60f));
            clock.Start();
            Assert.That(clock.Tick(60f), Is.True);
            Assert.That(clock.Running, Is.False);
            Assert.That(clock.Tick(1f), Is.False);
            Assert.That(clock.Remaining, Is.EqualTo(0f));
        }

        [Test]
        public void ResetRestoresTheFullLimit()
        {
            var clock = new ThinkClock(90f);
            clock.Start();
            clock.Tick(45f);
            clock.Reset();
            Assert.That(clock.Remaining, Is.EqualTo(90f));
            Assert.That(clock.Running, Is.False);
        }
    }
}
