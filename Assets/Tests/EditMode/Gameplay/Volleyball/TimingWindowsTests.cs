using KMA.Gameplay.Volleyball;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class TimingWindowsTests
    {
        [TestCase(0f, TimingGrade.Perfect)]
        [TestCase(.08f, TimingGrade.Perfect)]
        [TestCase(-.08f, TimingGrade.Perfect)]
        [TestCase(.081f, TimingGrade.Good)]
        [TestCase(-.18f, TimingGrade.Good)]
        [TestCase(.181f, TimingGrade.Late)]
        [TestCase(-.3f, TimingGrade.Late)]
        [TestCase(.301f, TimingGrade.Miss)]
        public void GradesByAbsoluteOffset(float offset, TimingGrade expected)
        {
            Assert.That(TimingWindows.Grade(offset), Is.EqualTo(expected));
        }

        [Test]
        public void ServeUsesTheWiderPerfectWindow()
        {
            Assert.That(TimingWindows.Grade(.11f, TimingWindows.ServePerfect), Is.EqualTo(TimingGrade.Perfect));
            Assert.That(TimingWindows.Grade(.11f), Is.EqualTo(TimingGrade.Good));
        }

        [Test]
        public void QualityMatchesTheSpec()
        {
            Assert.That(TimingWindows.Quality(TimingGrade.Perfect), Is.EqualTo(1f));
            Assert.That(TimingWindows.Quality(TimingGrade.Good), Is.EqualTo(.6f));
            Assert.That(TimingWindows.Quality(TimingGrade.Late), Is.EqualTo(.25f));
            Assert.That(TimingWindows.Quality(TimingGrade.Miss), Is.EqualTo(0f));
        }
    }
}
