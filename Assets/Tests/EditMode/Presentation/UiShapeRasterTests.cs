using System;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class UiShapeRasterTests
    {
        static float Alpha(Color32[] pixels, int size, int x, int y) => pixels[y * size + x].a / 255f;

        [Test]
        public void RoundedRect_IsTransparentAtTheCornerAndOpaqueAtTheCentre()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            Assert.That(size, Is.EqualTo(50));
            Assert.That(pixels, Has.Length.EqualTo(size * size));
            Assert.That(Alpha(pixels, size, 0, 0), Is.EqualTo(0f).Within(.01f), "corner must be cut away");
            Assert.That(Alpha(pixels, size, size / 2, size / 2), Is.EqualTo(1f).Within(.01f), "centre must be solid");
        }

        [Test]
        public void RoundedRect_IsOpaqueAtEachEdgeMidpoint()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            int mid = size / 2;
            Assert.That(Alpha(pixels, size, mid, 0), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, mid, size - 1), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, 0, mid), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, size, size - 1, mid), Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_HasASoftenedCornerRatherThanAHardStep()
        {
            int size = UiShapeRaster.RoundedRectSize(24);
            Color32[] pixels = UiShapeRaster.RoundedRect(24);
            int partial = 0;
            for (int y = 0; y <= 24; y++)
                for (int x = 0; x <= 24; x++)
                {
                    float alpha = Alpha(pixels, size, x, y);
                    if (alpha > .05f && alpha < .95f)
                        partial++;
                }
            Assert.That(partial, Is.GreaterThanOrEqualTo(4), "corner should be antialiased along the arc");
        }

        [Test]
        public void RoundedRect_AcceptsZeroRadiusAsAPlainSquare()
        {
            Assert.That(UiShapeRaster.RoundedRectSize(0), Is.EqualTo(4));
            Assert.That(Alpha(UiShapeRaster.RoundedRect(0), 4, 0, 0), Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_RejectsNegativeRadius() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => UiShapeRaster.RoundedRect(-1));

        [Test]
        public void Circle_IsSolidInsideAndClearAtTheCorners()
        {
            Color32[] pixels = UiShapeRaster.Circle(128);
            Assert.That(Alpha(pixels, 128, 64, 64), Is.EqualTo(1f).Within(.01f));
            Assert.That(Alpha(pixels, 128, 0, 0), Is.EqualTo(0f).Within(.01f));
            Assert.That(Alpha(pixels, 128, 64, 0), Is.GreaterThan(.5f), "the circle touches its edge midpoints");
        }

        [Test]
        public void Ring_IsHollowInTheMiddleAndSolidOnItsBand()
        {
            Color32[] pixels = UiShapeRaster.Ring(128, 6f);
            Assert.That(Alpha(pixels, 128, 64, 64), Is.EqualTo(0f).Within(.01f), "hollow centre");
            Assert.That(Alpha(pixels, 128, 64, 2), Is.EqualTo(1f).Within(.01f), "solid band");
            Assert.That(Alpha(pixels, 128, 64, 10), Is.EqualTo(0f).Within(.01f), "inside the band");
            Assert.That(Alpha(pixels, 128, 0, 0), Is.EqualTo(0f).Within(.01f), "outside the ring");
        }

        [Test]
        public void Ring_RejectsABandWiderThanItsRadius() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => UiShapeRaster.Ring(128, 64f));
    }
}
