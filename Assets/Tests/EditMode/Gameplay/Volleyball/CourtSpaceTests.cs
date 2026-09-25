using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class CourtSpaceTests
    {
        [Test]
        public void CourtCornersAndNetMapOntoTheBackgroundLines()
        {
            Assert.That(CourtSpace.ToBackgroundPixel(new Vector2(-8f, 4f)), Is.EqualTo(new Vector2(17f, 145f)));
            Assert.That(CourtSpace.ToBackgroundPixel(new Vector2(8f, -4f)), Is.EqualTo(new Vector2(383f, 413f)));
            Assert.That(CourtSpace.ToBackgroundPixel(Vector2.zero), Is.EqualTo(new Vector2(200f, 279f)));
        }

        [Test]
        public void WorldPositionAgreesWithTheBackgroundSpritePlacement()
        {
            var ground = new Vector2(-5.5f, 2.25f);
            Vector2 pixel = CourtSpace.ToBackgroundPixel(ground);
            Vector3 expected = CourtSpace.BackgroundWorldPosition + new Vector3(
                (pixel.x - 200f) / CourtSpace.BackgroundPixelsPerUnit,
                (215f - pixel.y) / CourtSpace.BackgroundPixelsPerUnit, 0f);

            Vector3 actual = CourtSpace.ToWorld(ground, 0f);

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-4f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-4f));
        }

        [Test]
        public void HeightLiftsTheWorldPositionStraightUp()
        {
            Vector3 ground = CourtSpace.ToWorld(new Vector2(3f, 1f), 0f);
            Vector3 raised = CourtSpace.ToWorld(new Vector2(3f, 1f), 2f);

            Assert.That(raised.x, Is.EqualTo(ground.x));
            Assert.That(raised.y - ground.y,
                Is.EqualTo(2f * CourtSpace.HeightLift * CourtSpace.PixelsPerMetreY / CourtSpace.BackgroundPixelsPerUnit).Within(1e-4f));
        }

        [Test]
        public void LinesCountAsIn()
        {
            Assert.That(CourtSpace.IsIn(new Vector2(8f, 4f)), Is.True);
            Assert.That(CourtSpace.IsIn(new Vector2(-8f, -4f)), Is.True);
            Assert.That(CourtSpace.IsIn(new Vector2(8.01f, 0f)), Is.False);
            Assert.That(CourtSpace.IsIn(new Vector2(0f, -4.01f)), Is.False);
        }

        [Test]
        public void SidesSplitAtTheNet()
        {
            Assert.That(CourtSpace.SideOf(new Vector2(-.1f, 0f)), Is.EqualTo(CourtSide.Player));
            Assert.That(CourtSpace.SideOf(new Vector2(.1f, 0f)), Is.EqualTo(CourtSide.Opponent));
            Assert.That(CourtSide.Player.Other(), Is.EqualTo(CourtSide.Opponent));
            Assert.That(CourtSide.Opponent.Other(), Is.EqualTo(CourtSide.Player));
            Assert.That(CourtSpace.SideSign(CourtSide.Player), Is.EqualTo(-1f));
        }
    }
}
