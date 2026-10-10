using System.Collections;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuideSceneTests
    {
        [UnityTest]
        public IEnumerator EveryMinigameSceneInstallsItsGuide(
            [Values("MG_Sprint:Sprint", "MG_Football:Football", "MG_Volleyball:Volleyball",
                "MG_FrogJump:FrogJump", "MG_ChessFinal:Chess")] string sceneAndKey)
        {
            string[] parts = sceneAndKey.Split(':');
            yield return SceneManager.LoadSceneAsync(parts[0], LoadSceneMode.Single);
            yield return null;
            yield return null;

            MinigameGuideHost host = MinigameGuideHost.Current;
            try
            {
                Assert.That(host, Is.Not.Null, parts[0] + " must install a guide host");
                Assert.That(host.Source.GuideKey, Is.EqualTo(parts[1]));
                Assert.That(host.Panel.IsOpen, Is.False, "auto-open is off in PlayMode tests");

                // A fresh store makes the test independent of any save left by earlier tests.
                host.Configure(host.Source, new MemoryTutorialSeenStore());
                Assert.That(host.TryOpenFirstRun(), Is.True);
                Assert.That(host.Panel.CurrentPage.Title, Is.EqualTo("MỤC TIÊU"));
                Assert.That(host.Panel.CurrentPage.Instruction, Is.Not.Empty);
                Assert.That(Time.timeScale, Is.Zero);

                host.Panel.PressSkip();
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Time.timeScale = 1f;
            }
        }
    }
}
