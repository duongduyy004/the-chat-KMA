using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyRoutingTests
    {
        GameObject root;
        SceneRouter router;

        [SetUp]
        public void SetUp()
        {
            var existing = Object.FindFirstObjectByType<SceneRouter>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            router = SceneRouter.EnsurePersistentInstance();
            root = router.gameObject;
            router.LoadSession(new GameSession());
            router.ConfigureRouteAcceptanceForTests((transition, complete) =>
            {
                complete();
                return true;
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void FutureChallengeIsRejectedWithoutChangingSessionOrLoadingScene()
        {
            int routes = 0;
            router.TransitionStarted += _ => routes++;

            Assert.That(router.TryStartChallenge("volleyball_learn"), Is.False);

            Assert.That(router.Session.Journey.ActiveAttempt, Is.Null);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(routes, Is.Zero);
        }

        [Test]
        public void ChallengeStartPersistsActiveAttemptBeforeRouting()
        {
            bool persisted = false;
            router.ConfigureJourneyPersistence((out string error) =>
            {
                error = null;
                persisted = router.Session.Journey.ActiveAttempt != null;
                return persisted;
            });
            bool persistedAtRoute = false;
            router.TransitionStarted += _ => persistedAtRoute = persisted;

            Assert.That(router.TryStartChallenge("sprint_learn"), Is.True);

            Assert.That(persisted, Is.True);
            Assert.That(persistedAtRoute, Is.True);
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("sprint_learn"));
        }
    }
}
