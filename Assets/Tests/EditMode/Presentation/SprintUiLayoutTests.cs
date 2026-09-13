using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiLayoutTests
    {
        [TestCase(0, 0.125f)]
        [TestCase(1, 0.375f)]
        [TestCase(2, 0.625f)]
        [TestCase(3, 0.875f)]
        public void LaneCenter01_CentersFourEqualLanes(int lane, float expected) =>
            Assert.That(KMA.Gameplay.SprintUiLayout.LaneCenter01(lane, 4), Is.EqualTo(expected).Within(.0001f));

        [Test]
        public void Controls_AreSymmetricSmallerThanHitAreasAndDisjoint()
        {
            var safe = new Rect(0, 0, 1920, 1080);
            Rect leftVisual = KMA.Gameplay.SprintUiLayout.VisibleControlRect(safe, true);
            Rect rightVisual = KMA.Gameplay.SprintUiLayout.VisibleControlRect(safe, false);
            Rect leftHit = KMA.Gameplay.SprintUiLayout.HitAreaRect(safe, true);
            Rect rightHit = KMA.Gameplay.SprintUiLayout.HitAreaRect(safe, false);
            Assert.That(leftVisual.width, Is.EqualTo(rightVisual.width));
            Assert.That(leftVisual.height, Is.EqualTo(rightVisual.height));
            Assert.That(leftVisual.width, Is.LessThan(leftHit.width));
            Assert.That(leftVisual.height, Is.LessThan(leftHit.height));
            Assert.That(leftHit.xMax, Is.LessThanOrEqualTo(safe.center.x));
            Assert.That(rightHit.xMin, Is.GreaterThanOrEqualTo(safe.center.x));
        }

        [TestCase(69.9f, false)]
        [TestCase(70f, true)]
        [TestCase(100f, true)]
        public void FinishVisible_UsesApprovedThreshold(float distance, bool expected) =>
            Assert.That(KMA.Gameplay.SprintUiLayout.FinishVisible(distance), Is.EqualTo(expected));
    }
}
