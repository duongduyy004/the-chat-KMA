using System;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class HeartBarCountdownTests
    {
        [TestCase(272, "4:32")]
        [TestCase(300, "5:00")]
        [TestCase(59.2, "1:00")]
        [TestCase(0, "0:00")]
        public void FormatsMinutesAndSecondsRoundingUp(double seconds, string expected)
        {
            Assert.That(HeartBar.FormatCountdown(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expected));
        }

        [Test]
        public void ShowsAboveTheHeartsOnlyWhileRegenerating()
        {
            var go = new GameObject("HeartBar", typeof(RectTransform));
            try
            {
                var bar = go.AddComponent<HeartBar>();
                bar.SetCountdown(TimeSpan.FromSeconds(272), false);
                Assert.That(bar.CountdownVisible, Is.True);
                Assert.That(bar.CountdownText, Is.EqualTo("4:32"));
                bar.SetCountdown(null, false);
                Assert.That(bar.CountdownVisible, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void RewritesTheLabelOnlyWhenTheShownSecondChanges()
        {
            var go = new GameObject("HeartBar", typeof(RectTransform));
            try
            {
                var bar = go.AddComponent<HeartBar>();
                bar.SetCountdown(TimeSpan.FromSeconds(271.9), false);
                bar.SetCountdown(TimeSpan.FromSeconds(271.5), false);
                bar.SetCountdown(TimeSpan.FromSeconds(271.1), false);
                Assert.That(bar.CountdownWrites, Is.EqualTo(1));
                Assert.That(bar.CountdownText, Is.EqualTo("4:32"));
                bar.SetCountdown(TimeSpan.FromSeconds(270.9), false);
                Assert.That(bar.CountdownWrites, Is.EqualTo(2));
                Assert.That(bar.CountdownText, Is.EqualTo("4:31"));
                bar.SetCountdown(TimeSpan.FromSeconds(270.5), true);
                Assert.That(bar.CountdownWrites, Is.EqualTo(3), "A warning colour change rewrites.");
                bar.SetCountdown(null, true);
                bar.SetCountdown(TimeSpan.FromSeconds(270.4), true);
                Assert.That(bar.CountdownVisible, Is.True);
                Assert.That(bar.CountdownWrites, Is.EqualTo(4), "Reappearing rewrites the label.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
