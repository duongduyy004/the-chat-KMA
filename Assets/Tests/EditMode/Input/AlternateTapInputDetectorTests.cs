using System.Collections.Generic;
using NUnit.Framework;

namespace KMA.Tests.Input
{
    public sealed class AlternateTapInputDetectorTests
    {
        [Test]
        public void FirstExpectedSide_IsValidAndAdvancesToOppositeSide()
        {
            var detector = new KMA.Input.AlternateTapInputDetector();
            var valid = new List<KMA.Input.Side>();
            var wrong = 0;
            detector.OnValidTap += side => valid.Add(side);
            detector.OnWrongSide += () => wrong++;

            detector.FeedTap(KMA.Input.Side.Left, 12345.678d);

            Assert.That(valid, Is.EqualTo(new[] { KMA.Input.Side.Left }));
            Assert.That(wrong, Is.EqualTo(0));
            Assert.That(detector.ExpectedSide, Is.EqualTo(KMA.Input.Side.Right));
        }

        [Test]
        public void RepeatedSide_EmitsWrongSideWithoutAdvancing()
        {
            var detector = new KMA.Input.AlternateTapInputDetector();
            var valid = 0;
            var wrong = 0;
            detector.OnValidTap += _ => valid++;
            detector.OnWrongSide += () => wrong++;

            detector.FeedTap(KMA.Input.Side.Left, 1d);
            detector.FeedTap(KMA.Input.Side.Left, 2d);

            Assert.That(valid, Is.EqualTo(1));
            Assert.That(wrong, Is.EqualTo(1));
            Assert.That(detector.ExpectedSide, Is.EqualTo(KMA.Input.Side.Right));
        }

        [Test]
        public void ValidAlternatingTaps_EmitExactlyOncePerTapAndPreserveSuppliedTimestamps()
        {
            var detector = new KMA.Input.AlternateTapInputDetector();
            var valid = new List<KMA.Input.Side>();
            detector.OnValidTap += side => valid.Add(side);

            detector.FeedTap(KMA.Input.Side.Left, 9001d);
            detector.FeedTap(KMA.Input.Side.Right, -9001d);

            Assert.That(valid, Is.EqualTo(new[] { KMA.Input.Side.Left, KMA.Input.Side.Right }));
            Assert.That(detector.ExpectedSide, Is.EqualTo(KMA.Input.Side.Left));
        }
    }
}
