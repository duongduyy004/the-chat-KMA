using KMA.Gameplay;
using NUnit.Framework;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class TimingEvaluatorTests
    {
        [TestCase(0.07999999999999999d, TimingJudge.Perfect)]
        [TestCase(-0.07999999999999999d, TimingJudge.Perfect)]
        [TestCase(0.08d, TimingJudge.Perfect)]
        [TestCase(-0.08d, TimingJudge.Perfect)]
        [TestCase(0.08000000000000002d, TimingJudge.Good)]
        [TestCase(-0.08000000000000002d, TimingJudge.Good)]
        [TestCase(0.15999999999999998d, TimingJudge.Good)]
        [TestCase(-0.15999999999999998d, TimingJudge.Good)]
        [TestCase(0.16d, TimingJudge.Good)]
        [TestCase(-0.16d, TimingJudge.Good)]
        [TestCase(0.16000000000000003d, TimingJudge.Miss)]
        [TestCase(-0.16000000000000003d, TimingJudge.Miss)]
        public void RhythmJudge_UsesExactInclusiveBoundaries(double inputDspTime, TimingJudge expected)
        {
            var judge = new RhythmBeatEvaluator(80, 160).Judge(inputDspTime, 0d);
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
