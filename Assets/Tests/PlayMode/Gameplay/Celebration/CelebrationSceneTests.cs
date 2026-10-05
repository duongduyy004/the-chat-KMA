using System.Collections;
using KMA.Gameplay.Celebration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Celebration
{
    public sealed class CelebrationSceneTests
    {
        [UnityTest]
        public IEnumerator OpenedWithoutASessionShowsSampleDataAndSkipReachesTheSummary()
        {
            yield return SceneManager.LoadSceneAsync("Celebration", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<CelebrationSceneController>();
            Assert.That(controller.Summary.IsSample, Is.True);
            Assert.That(controller.SummaryVisible, Is.False);
            controller.Skip();
            yield return new WaitForSeconds(.6f);
            Assert.That(controller.SummaryVisible, Is.True);
            Assert.That(controller.RowTexts.Length, Is.EqualTo(4));
            Assert.That(controller.RowTexts[3], Does.Contain("Bài kiểm tra cuối"));
        }
    }
}
