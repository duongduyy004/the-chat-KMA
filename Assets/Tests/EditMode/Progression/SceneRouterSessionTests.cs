using System;
using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class SceneRouterSessionTests
    {
        SceneRouter router;

        [SetUp]
        public void SetUp()
        {
            router = new GameObject("SceneRouterSessionTests.Router").AddComponent<SceneRouter>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (SceneRouter existing in UnityEngine.Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        [Test]
        public void LoadSession_ReplacesSessionWithoutChangingRouteConfiguration()
        {
            string routeConfigurationBefore = JsonUtility.ToJson(router);
            Assert.That(routeConfigurationBefore, Does.Contain("\"mapScene\":\"Map\""));
            Assert.That(routeConfigurationBefore, Does.Contain("MG_Sprint"));
            var restored = new GameSession();
            SaveData data = SaveData.CreateDefault();
            data.lives = 2;
            restored.Restore(data);

            var originalTransitioner = GetTransitioner(router);
            router.LoadSession(restored);

            Assert.That(router.Session, Is.SameAs(restored));
            Assert.That(JsonUtility.ToJson(router), Is.EqualTo(routeConfigurationBefore));

            var rebuiltTransitioner = GetTransitioner(router);
            Assert.That(rebuiltTransitioner, Is.Not.SameAs(originalTransitioner));
            Assert.That(GetTransitionerSession(rebuiltTransitioner), Is.SameAs(restored));
        }

        [Test]
        public void LoadSession_RejectsNullWithoutReplacingCurrentSession()
        {
            GameSession original = router.Session;

            Assert.That(() => router.LoadSession(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(router.Session, Is.SameAs(original));
        }

        static SessionRouteTransitioner GetTransitioner(SceneRouter sceneRouter) =>
            (SessionRouteTransitioner)typeof(SceneRouter)
                .GetField("transitioner", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sceneRouter);

        static GameSession GetTransitionerSession(SessionRouteTransitioner sceneTransitioner) =>
            (GameSession)typeof(SessionRouteTransitioner)
                .GetField("session", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sceneTransitioner);
    }
}
