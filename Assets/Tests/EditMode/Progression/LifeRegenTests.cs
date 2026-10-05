using System;
using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class LifeRegenTests
    {
        static readonly DateTime T0 = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        static readonly TimeSpan Five = TimeSpan.FromMinutes(5);

        [Test]
        public void FullLivesClearTheMark()
        {
            int lives = 5; long next = T0.Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0, 5), Is.Zero);
            Assert.That(next, Is.Zero);
            Assert.That(LifeRegen.Remaining(lives, next, T0, 5), Is.Null);
        }

        [Test]
        public void DroppingBelowMaxStartsAFiveMinuteMark()
        {
            int lives = 4; long next = 0;
            LifeRegen.Advance(ref lives, ref next, T0, 5);
            Assert.That(lives, Is.EqualTo(4));
            Assert.That(next, Is.EqualTo((T0 + Five).Ticks));
            Assert.That(LifeRegen.Remaining(lives, next, T0 + TimeSpan.FromSeconds(28), 5),
                Is.EqualTo(TimeSpan.FromSeconds(272)));
        }

        [Test]
        public void SpendingAgainWhileRunningKeepsTheMark()
        {
            int lives = 4; long next = 0;
            LifeRegen.Advance(ref lives, ref next, T0, 5);
            long mark = next;
            lives = 3;
            LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromMinutes(2), 5);
            Assert.That(next, Is.EqualTo(mark));
        }

        [Test]
        public void ReachingTheMarkGrantsOneAndChainsTheNext()
        {
            int lives = 3; long next = (T0 + Five).Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + Five, 5), Is.EqualTo(1));
            Assert.That(lives, Is.EqualTo(4));
            Assert.That(next, Is.EqualTo((T0 + Five + Five).Ticks));
        }

        [Test]
        public void LongAbsenceGrantsSeveralAndStopsAtMax()
        {
            int lives = 0; long next = (T0 + Five).Ticks;
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromMinutes(12), 5), Is.EqualTo(2));
            Assert.That(lives, Is.EqualTo(2));
            Assert.That(next, Is.EqualTo((T0 + TimeSpan.FromMinutes(15)).Ticks));
            Assert.That(LifeRegen.Advance(ref lives, ref next, T0 + TimeSpan.FromDays(3), 5), Is.EqualTo(3));
            Assert.That(lives, Is.EqualTo(5));
            Assert.That(next, Is.Zero);
        }

        [Test]
        public void ClockMovedBackwardsResetsTheMarkWithoutGrantingLives()
        {
            int lives = 2; long next = (T0 + Five).Ticks;
            DateTime rewound = T0 - TimeSpan.FromHours(6);
            Assert.That(LifeRegen.Advance(ref lives, ref next, rewound, 5), Is.Zero);
            Assert.That(lives, Is.EqualTo(2));
            Assert.That(next, Is.EqualTo((rewound + Five).Ticks));
        }
    }
}
