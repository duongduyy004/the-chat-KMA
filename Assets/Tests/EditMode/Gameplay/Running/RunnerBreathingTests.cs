using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class RunnerBreathingTests
    {
        [Test]
        public void RestingRunnerHeavesTheChestAndReturnsToItsSizeWhenRunningAgain()
        {
            var go = new GameObject("Runner");
            try
            {
                var breathing = go.AddComponent<RunnerBreathing>();
                breathing.Step(0f);
                float restSize = go.transform.localScale.y;

                breathing.SetResting(true);
                float tallest = restSize;
                for (int i = 0; i < 240; i++)
                {
                    breathing.Step(1f / 60f);
                    tallest = Mathf.Max(tallest, go.transform.localScale.y);
                }
                Assert.That(tallest, Is.GreaterThan(restSize + .01f));

                breathing.SetResting(false);
                for (int i = 0; i < 120; i++) breathing.Step(1f / 60f);
                Assert.That(go.transform.localScale.y, Is.EqualTo(restSize).Within(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RivalIsSlowingOnlyInTheLowPartOfItsSurgeAfterTheOpening()
        {
            // Phase -pi/2 starts the surge at its trough; +pi/2 starts it at its peak.
            var low = new RivalPaceProfile("Low", 6f, 6f, .2f, 4f, -Mathf.PI / 2f);
            var high = new RivalPaceProfile("High", 6f, 6f, .2f, 4f, Mathf.PI / 2f);
            float afterOpening = RivalPaceProfile.OpeningSeconds + .01f;

            Assert.That(low.IsSlowingAt(afterOpening), Is.True);
            Assert.That(high.IsSlowingAt(afterOpening), Is.False);
            Assert.That(low.IsSlowingAt(1f), Is.False, "The opening burst is never a slow patch.");
        }
    }
}
