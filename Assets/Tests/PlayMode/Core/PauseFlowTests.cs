using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class PauseFlowTests
    {
        [Test]
        public void PausePanel_RestoresPreviousTimeScaleAndRaisesActionsOnce()
        {
            var root = new GameObject("PausePanel");
            var panel = root.AddComponent<PausePanel>();
            try
            {
                Time.timeScale = .5f;
                var restarts = 0;
                var exits = 0;
                panel.RestartRequested += () => restarts++;
                panel.ExitToMapRequested += () => exits++;
                panel.Open();
                Assert.That(Time.timeScale, Is.Zero);
                panel.Restart();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
                Assert.That(restarts, Is.EqualTo(1));
                panel.Open();
                panel.ExitToMap();
                Assert.That(Time.timeScale, Is.EqualTo(.5f));
                Assert.That(exits, Is.EqualTo(1));
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(root);
            }
        }
    }
}
