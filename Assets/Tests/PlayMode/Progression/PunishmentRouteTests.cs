using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class PunishmentRouteTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var router in Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(router.gameObject);
            }

        }

        [UnityTest]
        public IEnumerator SprintLoss_ReturnsToSubjectSelect_WithoutEnteringPunishment()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            int livesBefore = router.Session.Lives;
            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForScene("MG_Sprint");

            Assert.That(router.SubmitSubjectResult(SubjectId.Sprint,
                new MinigameResult(false, 0f, Rank.F)), Is.True);
            yield return WaitForScene("Map");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map"));
            Assert.That(router.Session.PendingPunishmentSubject, Is.Null);
            Assert.That(router.Session.AwaitingPunishment, Is.False);
            Assert.That(router.Session.Lives, Is.EqualTo(livesBefore - 1));
        }

        static IEnumerator WaitForScene(string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName)
                yield return null;
            yield return null;
        }
    }
}
