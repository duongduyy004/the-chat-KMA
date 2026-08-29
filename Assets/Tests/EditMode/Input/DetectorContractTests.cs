using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Input
{
    public sealed class DetectorContractTests
    {
        [Test]
        public void TapMash_EmitsEachTapAndCountsOnlyTheTrailingSecond()
        {
            var detector = new KMA.Input.TapMashInputDetector();
            var emitted = 0;
            detector.OnTap += () => emitted++;

            detector.FeedTap(0d);
            detector.FeedTap(.25d);
            detector.FeedTap(.75d);
            detector.FeedTap(1.26d);

            Assert.That(emitted, Is.EqualTo(4));
            Assert.That(detector.TapsPerSecond, Is.EqualTo(2));
        }

        [TestCase(.08d, KMA.Input.TimingJudge.Perfect, 80d)]
        [TestCase(-.08d, KMA.Input.TimingJudge.Perfect, -80d)]
        [TestCase(.16d, KMA.Input.TimingJudge.Good, 160d)]
        [TestCase(-.16d, KMA.Input.TimingJudge.Good, -160d)]
        [TestCase(.16000000000000003d, KMA.Input.TimingJudge.Miss, 160.00000000000003d)]
        public void RhythmBeat_UsesInclusivePerfectAndGoodBoundaries(
            double inputDsp,
            KMA.Input.TimingJudge expectedJudge,
            double expectedDeltaMs)
        {
            var detector = new KMA.Input.RhythmBeatInputDetector();
            KMA.Input.TimingJudge actualJudge = KMA.Input.TimingJudge.Miss;
            double actualDeltaMs = 0d;
            detector.OnJudge += (judge, deltaMs) =>
            {
                actualJudge = judge;
                actualDeltaMs = deltaMs;
            };

            detector.FeedTap(inputDsp, 0d);

            Assert.That(actualJudge, Is.EqualTo(expectedJudge));
            Assert.That(actualDeltaMs, Is.EqualTo(expectedDeltaMs).Within(.0000001d));
        }

        [Test]
        public void Hold_ClampsOverchargeAndNegativeDurations()
        {
            var detector = new KMA.Input.HoldInputDetector();
            var starts = 0;
            double completedDuration = -1d;
            detector.OnHoldStart += () => starts++;
            detector.OnHoldEnd += duration => completedDuration = duration;

            detector.FeedDown(5d);
            detector.FeedUp(7d);

            Assert.That(starts, Is.EqualTo(1));
            Assert.That(completedDuration, Is.EqualTo(2d));
            Assert.That(detector.ChargeRatio, Is.EqualTo(1d));

            detector.FeedDown(10d);
            detector.FeedUp(9d);

            Assert.That(completedDuration, Is.EqualTo(0d));
            Assert.That(detector.ChargeRatio, Is.EqualTo(0d));
        }

        [Test]
        public void AlternateTap_OnlyAdvancesExpectedSideAfterAValidTap()
        {
            var detector = new KMA.Input.AlternateTapInputDetector();
            var valid = new System.Collections.Generic.List<KMA.Input.Side>();
            var wrong = 0;
            detector.OnValidTap += side => valid.Add(side);
            detector.OnWrongSide += () => wrong++;

            detector.FeedTap(KMA.Input.Side.Left, 1d);
            detector.FeedTap(KMA.Input.Side.Left, 2d);
            detector.FeedTap(KMA.Input.Side.Right, 3d);

            Assert.That(valid, Is.EqualTo(new[] { KMA.Input.Side.Left, KMA.Input.Side.Right }));
            Assert.That(wrong, Is.EqualTo(1));
        }

        [Test]
        public void Swipe_EmitsCurvedPathMetricsAndResetsAfterEnd()
        {
            var detector = new KMA.Input.SwipeInputDetector();
            KMA.Input.SwipeResult result = default;
            var emitted = 0;
            detector.OnSwipe += swipe =>
            {
                result = swipe;
                emitted++;
            };

            detector.FeedSample(Vector2.zero, 1d);
            detector.FeedSample(new Vector2(5f, 5f), 1.5d);
            detector.FeedSample(new Vector2(10f, 0f), 2d);
            detector.FeedEnd();
            detector.FeedEnd();

            Assert.That(emitted, Is.EqualTo(1));
            Assert.That(result.Direction, Is.EqualTo(KMA.Input.SwipeDirection.Right));
            Assert.That(result.Length, Is.EqualTo(10d).Within(.0000001d));
            Assert.That(result.Duration, Is.EqualTo(1d));
            Assert.That(result.Curvature, Is.EqualTo(.5d).Within(.0000001d));
        }

        [Test]
        public void Detectors_RejectNonFiniteTimestampsWithoutEmittingEvents()
        {
            var taps = new KMA.Input.TapMashInputDetector();
            var rhythm = new KMA.Input.RhythmBeatInputDetector();
            var hold = new KMA.Input.HoldInputDetector();
            var alternate = new KMA.Input.AlternateTapInputDetector();
            var swipe = new KMA.Input.SwipeInputDetector();
            var emitted = 0;
            taps.OnTap += () => emitted++;
            rhythm.OnJudge += (_, _) => emitted++;
            hold.OnHoldStart += () => emitted++;
            alternate.OnValidTap += _ => emitted++;
            alternate.OnWrongSide += () => emitted++;
            swipe.OnSwipe += _ => emitted++;

            taps.FeedTap(double.NaN);
            rhythm.FeedTap(double.PositiveInfinity, 0d);
            hold.FeedDown(double.NegativeInfinity);
            alternate.FeedTap(KMA.Input.Side.Left, double.NaN);
            swipe.FeedSample(Vector2.zero, double.PositiveInfinity);
            swipe.FeedEnd();

            Assert.That(emitted, Is.EqualTo(0));
            Assert.That(taps.TapsPerSecond, Is.EqualTo(0));
            Assert.That(hold.ChargeRatio, Is.EqualTo(0d));
            Assert.That(alternate.ExpectedSide, Is.EqualTo(KMA.Input.Side.Left));
        }
    }
}
