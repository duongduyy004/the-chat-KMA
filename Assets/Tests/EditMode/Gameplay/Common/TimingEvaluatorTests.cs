using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class TimingEvaluatorTests
    {
        [TestCase(80, TimingJudge.Perfect)]
        [TestCase(-80, TimingJudge.Perfect)]
        [TestCase(160, TimingJudge.Good)]
        [TestCase(-160, TimingJudge.Good)]
        [TestCase(160.1, TimingJudge.Miss)]
        public void RhythmJudge_UsesInclusiveWindows(double deltaMs, TimingJudge expected)
        {
            var judge = new RhythmBeatEvaluator(80, 160).Judge(10 + deltaMs / 1000d, 10);
            Assert.That(judge, Is.EqualTo(expected));
        }

        [TestCase(0, 1)]
        [TestCase(50, .5f)]
        [TestCase(100, 0)]
        [TestCase(150, 0)]
        public void TimingWindow_ClampsAccuracy(float error, float expected) =>
            Assert.That(new TimingWindow(100).Evaluate(error), Is.EqualTo(expected).Within(.001));
    }
}
