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
    }
}
