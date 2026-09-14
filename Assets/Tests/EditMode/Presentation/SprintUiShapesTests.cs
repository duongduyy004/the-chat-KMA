using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiShapesTests
    {
        [SetUp]
        public void ResetCache() => SprintUiShapes.ClearCacheForTest();

        [Test]
        public void RoundedRect_CarriesA9SliceBorderEqualToTheRadius()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Assert.That(sprite.border, Is.EqualTo(new Vector4(24f, 24f, 24f, 24f)));
        }

        [Test]
        public void RoundedRect_IsTransparentAtTheCornerAndOpaqueAtTheCentre()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Texture2D texture = sprite.texture;
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(.01f), "corner must be cut away");
            Assert.That(texture.GetPixel(texture.width / 2, texture.height / 2).a,
                Is.EqualTo(1f).Within(.01f), "centre must be solid");
        }

        [Test]
        public void RoundedRect_IsOpaqueAtEachEdgeMidpoint()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(24);
            Texture2D texture = sprite.texture;
            int mid = texture.width / 2;
            Assert.That(texture.GetPixel(mid, 0).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(mid, texture.height - 1).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(0, mid).a, Is.EqualTo(1f).Within(.01f));
            Assert.That(texture.GetPixel(texture.width - 1, mid).a, Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_HasASoftenedCornerRatherThanAHardStep()
        {
            Texture2D texture = SprintUiShapes.RoundedRect(24).texture;
            // Walking the diagonal out of the corner must cross at least one partial pixel.
            bool sawPartial = false;
            for (int i = 0; i < 24; i++)
            {
                float alpha = texture.GetPixel(i, i).a;
                if (alpha > .05f && alpha < .95f)
                    sawPartial = true;
            }
            Assert.That(sawPartial, Is.True, "corner should be antialiased, not a hard step");
        }

        [Test]
        public void RoundedRect_ReturnsTheCachedInstanceForTheSameRadius()
        {
            Sprite first = SprintUiShapes.RoundedRect(36);
            Sprite second = SprintUiShapes.RoundedRect(36);
            Assert.That(second, Is.SameAs(first));
            Assert.That(SprintUiShapes.RoundedRect(24), Is.Not.SameAs(first));
        }

        [Test]
        public void RoundedRect_AcceptsZeroRadiusAsAPlainSquare()
        {
            Sprite sprite = SprintUiShapes.RoundedRect(0);
            Assert.That(sprite.border, Is.EqualTo(Vector4.zero));
            Assert.That(sprite.texture.GetPixel(0, 0).a, Is.EqualTo(1f).Within(.01f));
        }

        [Test]
        public void RoundedRect_RejectsNegativeRadius() =>
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SprintUiShapes.RoundedRect(-1));
    }
}
