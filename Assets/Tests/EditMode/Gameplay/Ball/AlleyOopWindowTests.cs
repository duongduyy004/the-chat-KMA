using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class AlleyOopWindowTests
    {
        const float FixedStep = .02f;

        // The judge needs |velocityY| <= VelocityThreshold, so the window's real length is what a
        // player has to hit. Measuring it in physics steps is the only honest assertion: a
        // threshold that opens for less than one step cannot be hit at all.
        [Test]
        public void AuthoredApexWindow_StaysOpenForAtLeastTwelveFixedSteps()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault();
            var gravity = new Vector2(0f, -9.81f);

            var openSteps = 0;
            var velocity = new Vector2(0f, 6f);
            for (var step = 0; step < 2000; step++)
            {
                velocity = Ballistics.AdvanceVelocity(velocity, gravity, 0f, .02f, FixedStep);
                if (Mathf.Abs(velocity.y) <= pattern.VelocityThreshold)
                    openSteps++;
                else if (openSteps > 0)
                    break;
            }

            Assert.That(openSteps, Is.GreaterThanOrEqualTo(12),
                "The Perfect window must last long enough for a human tap, not a single physics step. " +
                "Measured steps: " + openSteps);
        }

        // Widening the timing window must not widen the aiming band, or the two difficulty axes
        // collapse into one.
        [Test]
        public void AuthoredApexWindow_KeepsTheAuthoredHeightBandAndLaunchContract()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault(new Vector2(1f, .75f));

            Assert.That(pattern.ApexMin, Is.EqualTo(2.8f));
            Assert.That(pattern.ApexMax, Is.EqualTo(3.2f));
            Assert.That(pattern.LaunchForce, Is.EqualTo(8f));
            Assert.That(pattern.Curvature, Is.EqualTo(0f));
            Assert.That(pattern.PassVector, Is.EqualTo(new Vector2(1f, .75f)));
            Assert.That(pattern.IsApexWindow(2.79f, 0f), Is.False, "The height band must stay exclusive.");
            Assert.That(pattern.IsApexWindow(3.21f, 0f), Is.False, "The height band must stay exclusive.");
        }

        // A ball whose apex lands outside the authored band must still be judged wrong even when
        // the tap is perfectly timed - that is the aiming axis.
        [Test]
        public void PerfectTiming_OnAMisaimedLob_IsNotPerfect()
        {
            AlleyOopPattern pattern = AlleyOopPattern.AuthoredDefault();

            Assert.That(pattern.IsApexWindow(2.4f, 0f), Is.False, "An under-charged lob apexes below the band.");
            Assert.That(pattern.IsApexWindow(3.6f, 0f), Is.False, "An over-charged lob apexes above the band.");
            Assert.That(pattern.IsApexWindow(3.0f, 0f), Is.True, "A correctly charged lob apexes inside the band.");
        }
    }
}
