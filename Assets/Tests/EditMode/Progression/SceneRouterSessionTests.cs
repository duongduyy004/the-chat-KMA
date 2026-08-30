using System;
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
            Assert.That(router.TryGetSceneName(SessionRoute.Map, null, out string mapBefore), Is.True);
            Assert.That(router.TryGetSceneName(SessionRoute.Subject, SubjectId.Sprint, out string sprintBefore),
                Is.True);
            var restored = new GameSession();
            SaveData data = SaveData.CreateDefault();
            data.lives = 2;
            restored.Restore(data);

            router.LoadSession(restored);
            SceneRouteTransition transition = default;
            int transitionCount = 0;
            router.TransitionStarted += startedTransition =>
            {
                transition = startedTransition;
                transitionCount++;
            };

            Assert.That(router.Session, Is.SameAs(restored));
            Assert.That(router.TryGetSceneName(SessionRoute.Map, null, out string mapAfter), Is.True);
            Assert.That(router.TryGetSceneName(SessionRoute.Subject, SubjectId.Sprint, out string sprintAfter),
                Is.True);
            Assert.That(mapAfter, Is.EqualTo(mapBefore));
            Assert.That(sprintAfter, Is.EqualTo(sprintBefore));
            Assert.That(router.Route(SessionRoute.Map), Is.True);
            Assert.That(transitionCount, Is.EqualTo(1));
            Assert.That(transition.Session, Is.SameAs(restored));
        }

        [Test]
        public void LoadSession_RejectsNullWithoutReplacingCurrentSession()
        {
            GameSession original = router.Session;

            Assert.That(() => router.LoadSession(null), Throws.TypeOf<ArgumentNullException>());
            Assert.That(router.Session, Is.SameAs(original));
        }
    }
}
