using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FrogJumpRoutingTests
    {
        readonly List<SceneRouteTransition> routes = new List<SceneRouteTransition>();
        SceneRouter router;
        ResultPanel panel;

        [SetUp]
        public void SetUp()
        {
            // Same construction as JourneyRoutingTests: a router with an in-memory session,
            // route acceptance recorded into `routes`, persistence that always succeeds,
            // and a code-built ResultPanel in the active scene.
            routes.Clear();
            router = JourneyRoutingFixture.CreateRouter(routes);
            panel = JourneyRoutingFixture.CreateResultPanel();
        }

        [TearDown]
        public void TearDown() => JourneyRoutingFixture.Destroy(router, panel);

        [Test]
        public void FailedExamOffersOnlyTheFrogJumpAndRoutesToItsScene()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");

            Assert.That(panel.RetryAvailable, Is.False);
            Assert.That(panel.ContinueLabel, Is.EqualTo(VietText.Fix("BẬT CÓC")));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.FrogJump));
            Assert.That(routes[routes.Count - 1].SceneName, Is.EqualTo("MG_FrogJump"));
        }

        [Test]
        public void WinningTheFirstFrogJumpKeepsTheLifeAndRestartsTheChallenge()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);

            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(router.Session.PendingFrogJump, Is.Null);
            Assert.That(panel.ContinueLabel, Is.EqualTo(VietText.Fix("THI LẠI")));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(router.Session.Journey.ActiveAttempt.ChallengeId, Is.EqualTo("sprint_exam"));
        }

        [Test]
        public void DuplicateFrogCompletionAndDoubleContinueApplyOnce()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(false);
            router.CompleteFrogJumpForTests(false);
            Assert.That(router.Session.Lives, Is.EqualTo(4));
            int before = routes.Count;
            panel.Continue();
            panel.Continue();
            Assert.That(routes.Count, Is.EqualTo(before + 1));
        }

        [Test]
        public void LastLifeLostLandsOnTheMap()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);
            panel.Continue();
            router.Session.Journey.SetAttemptsRemaining(1);
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");
            panel.Continue();
            router.CompleteFrogJumpForTests(true);

            Assert.That(router.Session.Lives, Is.Zero);
            Assert.That(panel.ContinueLabel, Is.EqualTo(VietText.Fix("VỀ BẢN ĐỒ")));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(router.Session.ActiveSubject, Is.Null);
        }

        [Test]
        public void LearnFailureNeverOffersTheFrogJump()
        {
            JourneyRoutingFixture.FailActive(router, panel, "sprint_learn");

            Assert.That(router.Session.PendingFrogJump, Is.Null);
            Assert.That(panel.ContinueLabel, Is.EqualTo(VietText.Fix("TIẾP TỤC")));
            panel.Continue();
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.Map));
        }

        [Test]
        public void ResumeWithAPendingFrogJumpRoutesToItsScene()
        {
            JourneyRoutingFixture.CompleteThrough(router.Session, "sprint_practice");
            JourneyRoutingFixture.FailActive(router, panel, "sprint_exam");

            Assert.That(router.ResumeCampaign(), Is.True);
            Assert.That(routes[routes.Count - 1].Route, Is.EqualTo(SessionRoute.FrogJump));
            Assert.That(routes[routes.Count - 1].SceneName, Is.EqualTo("MG_FrogJump"));
        }
    }
}
