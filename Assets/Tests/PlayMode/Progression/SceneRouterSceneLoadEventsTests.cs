using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class SceneRouterSceneLoadEventsTests
    {
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
        public IEnumerator SceneLoadEventsFireAroundEverySceneSwapSceneRouterTriggers()
        {
            var router = SceneRouter.EnsurePersistentInstance();
            var events = new List<string>();
            router.SceneLoadStarted += () => events.Add("started");
            router.SceneLoadCompleted += () => events.Add("completed");

            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            Assert.That(events, Is.EqualTo(new[] { "started" }));

            yield return WaitForScene("MG_Sprint");
            Assert.That(events, Is.EqualTo(new[] { "started", "completed" }));

            Assert.That(router.RouteToMenu(), Is.True);
            Assert.That(events, Is.EqualTo(new[] { "started", "completed", "started" }));

            yield return WaitForScene("Menu");
            Assert.That(events, Is.EqualTo(new[] { "started", "completed", "started", "completed" }));
        }

        static IEnumerator WaitForScene(string sceneName)
        {
            while (SceneManager.GetActiveScene().name != sceneName)
                yield return null;
            yield return null;
        }
    }
}
