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
            // The one-pixel coverage ramp follows the corner arc. Scan the whole corner
            // block rather than the 45-degree diagonal, where the ramp is sqrt(2)x steeper
            // and falls entirely between integer samples.
            int partial = 0;
            for (int y = 0; y <= 24; y++)
            {
                for (int x = 0; x <= 24; x++)
                {
                    float alpha = texture.GetPixel(x, y).a;
                    if (alpha > .05f && alpha < .95f)
                        partial++;
                }
            }

            Assert.That(partial, Is.GreaterThanOrEqualTo(4),
                "corner should be antialiased along the arc, not a hard step");
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
