using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class GameFreezeTests
    {
        GameObject probeObject;
        PauseProbe probe;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = .5f;
            probeObject = new GameObject("PauseProbe");
            probe = probeObject.AddComponent<PauseProbe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(probeObject);
            Time.timeScale = 1f;
        }

        [Test]
        public void TheLastReleaseRestoresTheSavedScale()
        {
            object pause = new object(), guide = new object();
            GameFreeze.Acquire(pause);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(probe.Paused, Is.True);

            GameFreeze.Acquire(guide);
            GameFreeze.Release(pause);
            Assert.That(Time.timeScale, Is.Zero, "the guide still holds the freeze");
            Assert.That(GameFreeze.IsFrozen, Is.True);

            GameFreeze.Release(guide);
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(probe.Paused, Is.False);
            Assert.That(GameFreeze.IsFrozen, Is.False);
        }

        [Test]
        public void RepeatedCallsByOneHolderCountOnce()
        {
            object holder = new object();
            GameFreeze.Acquire(holder);
            GameFreeze.Acquire(holder);
            Assert.That(probe.PauseCalls, Is.EqualTo(1));
            GameFreeze.Release(holder);
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            GameFreeze.Release(holder);
            Assert.That(Time.timeScale, Is.EqualTo(.5f), "an extra release must not touch the scale");
            Assert.That(probe.ResumeCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReleasingAnUnknownHolderDoesNothing()
        {
            GameFreeze.Release(new object());
            Assert.That(Time.timeScale, Is.EqualTo(.5f));
            Assert.That(probe.ResumeCalls, Is.Zero);
        }

        sealed class PauseProbe : MonoBehaviour, IPauseAware
        {
            public bool Paused;
            public int PauseCalls, ResumeCalls;

            public void SetPaused(bool paused)
            {
                Paused = paused;
                if (paused) PauseCalls++;
                else ResumeCalls++;
            }
        }
    }
}
