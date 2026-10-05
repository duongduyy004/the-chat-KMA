using System;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Common
{
    public sealed class CampusBackdropLayoutTests
    {
        static readonly Rect View16By9 = new Rect(-9.6f, -5.4f, 19.2f, 10.8f);
        static readonly Rect View20By9 = new Rect(-12f, -5.4f, 24f, 10.8f);

        [Test]
        public void Cover_KeepsAspectAndCoversTheWholeView()
        {
            Rect cover = CampusBackdropLayout.Cover(View16By9, 2f);
            Assert.That(cover.width / cover.height, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(cover.xMin, Is.LessThanOrEqualTo(View16By9.xMin + 1e-4f));
            Assert.That(cover.xMax, Is.GreaterThanOrEqualTo(View16By9.xMax - 1e-4f));
            Assert.That(cover.yMin, Is.LessThanOrEqualTo(View16By9.yMin + 1e-4f));
            Assert.That(cover.yMax, Is.GreaterThanOrEqualTo(View16By9.yMax - 1e-4f));
            Assert.That(cover.center.x, Is.EqualTo(View16By9.center.x).Within(1e-4f));
        }

        [Test]
        public void Cover_TallSpriteFillsTheWidth()
        {
            Rect cover = CampusBackdropLayout.Cover(View16By9, .5f);
            Assert.That(cover.width, Is.EqualTo(View16By9.width).Within(1e-4f));
            Assert.That(cover.height, Is.EqualTo(View16By9.width * 2f).Within(1e-4f));
        }

        [TestCase(1.6f)]
        [TestCase(6.4f)]
        [TestCase(30f)]
        public void Band_CoversA16By9View(float aspect) => AssertBandCovers(View16By9, aspect);

        [TestCase(1.6f)]
        [TestCase(6.4f)]
        public void Band_CoversA20By9View(float aspect) => AssertBandCovers(View20By9, aspect);

        [Test]
        public void Band_TilesAreOddCountCentredAndTouching()
        {
            Rect[] tiles = CampusBackdropLayout.Band(View16By9, 1f, 2f, 2f);
            Assert.That(tiles.Length % 2, Is.EqualTo(1));
            Assert.That(tiles[tiles.Length / 2].center.x, Is.EqualTo(View16By9.center.x).Within(1e-4f));
            for (int i = 1; i < tiles.Length; i++)
                Assert.That(tiles[i].xMin, Is.EqualTo(tiles[i - 1].xMax).Within(1e-4f));
            foreach (Rect tile in tiles)
            {
                Assert.That(tile.yMin, Is.EqualTo(1f).Within(1e-4f));
                Assert.That(tile.height, Is.EqualTo(2f).Within(1e-4f));
                Assert.That(tile.width, Is.EqualTo(4f).Within(1e-4f));
            }
        }

        [Test]
        public void Band_ZeroHeightDrawsNothing() =>
            Assert.That(CampusBackdropLayout.Band(View16By9, 0f, 0f, 2f), Is.Empty);

        [Test]
        public void NonPositiveAspectThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CampusBackdropLayout.Cover(View16By9, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => CampusBackdropLayout.Band(View16By9, 0f, 1f, -1f));
        }

        static void AssertBandCovers(Rect view, float aspect)
        {
            Rect[] tiles = CampusBackdropLayout.Band(view, 0f, 3f, aspect);
            Assert.That(tiles, Is.Not.Empty);
            Assert.That(tiles[0].xMin, Is.LessThanOrEqualTo(view.xMin + 1e-4f), "left edge uncovered");
            Assert.That(tiles[tiles.Length - 1].xMax, Is.GreaterThanOrEqualTo(view.xMax - 1e-4f), "right edge uncovered");
        }
    }
}
