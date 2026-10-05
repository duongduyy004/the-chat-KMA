using KMA.Gameplay.Volleyball;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballHudTimerTests
    {
        [TestCase(120f, "2:00")]
        [TestCase(59.2f, "1:00")]
        [TestCase(5f, "0:05")]
        [TestCase(0f, "0:00")]
        [TestCase(-3f, "0:00")]
        public void TimerShowsMinutesAndSeconds(float seconds, string expected) =>
            Assert.That(VolleyballHud.TimerText(seconds), Is.EqualTo(expected));
    }
}
