using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    /// Router and result-panel construction shared by the journey routing tests.
    internal static class JourneyRoutingFixture
    {
        /// A router with a fresh in-memory session whose routes are accepted (and recorded) at once.
        /// No journey persistence is configured, so every save succeeds.
        public static SceneRouter CreateRouter(List<SceneRouteTransition> routes = null)
        {
            // These router unit tests own their result panel. Scene panels left
            // by preceding integration tests must not intercept its bindings.
            foreach (ResultPanel existingPanel in Object.FindObjectsByType<ResultPanel>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(existingPanel.gameObject);
            var existing = Object.FindFirstObjectByType<SceneRouter>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            SceneRouter router = SceneRouter.EnsurePersistentInstance();
            router.LoadSession(new GameSession());
            router.ConfigureRouteAcceptanceForTests((transition, complete) =>
            {
                routes?.Add(transition);
                complete();
                return true;
            });
            return router;
        }

        /// A code-built ResultPanel in the active scene; its buttons carry TMP labels.
        public static ResultPanel CreateResultPanel()
        {
            var go = new GameObject("JourneyTestResult", typeof(RectTransform));
            var panel = go.AddComponent<ResultPanel>();
            TMP_Text Text(string name, Transform parent)
            {
                var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                text.transform.SetParent(parent);
                return text;
            }
            Button Button(string name)
            {
                var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
                button.transform.SetParent(go.transform);
                Text("Label", button.transform);
                return button;
            }
            panel.Configure(go, Text("Status", go.transform), Text("Detail", go.transform),
                Text("Score", go.transform), Text("Rank", go.transform), Text("Lives", go.transform),
                Button("Continue"), Button("Retry"), Text("Error", go.transform));
            return panel;
        }

        public static void Destroy(SceneRouter router, ResultPanel panel)
        {
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
            if (router != null) Object.DestroyImmediate(router.gameObject);
        }

        public static void CompleteThrough(GameSession session, string id) =>
            JourneyGameplayDriver.CompleteThrough(session, id);

        /// Starts `id` through the router (unless a frog-jump retry already started it) and
        /// reports a failed result to the result panel.
        public static void FailActive(SceneRouter router, ResultPanel panel, string id)
        {
            ChallengeDefinition definition = router.Session.Journey.Catalog.Get(id);
            ChallengeAttemptContext active = router.Session.Journey.ActiveAttempt;
            if (active == null || active.ChallengeId != id)
                Assert.That(router.TryStartChallenge(id, ChallengeAttemptMode.Journey, definition.Difficulty),
                    Is.True, id);
            router.ReportChallengeResultForTests(new ChallengeAttemptResult(router.Session.Journey.ActiveAttempt,
                false, new ChallengeMetrics(), ChallengeDefinition.IsScored(definition.Kind)
                    ? new MinigameResult(false, 0f, Rank.F) : null));
            Assert.That(panel.CurrentResult, Is.Not.Null, "The fixture panel must show the failed result.");
        }

        public static void PassActive(SceneRouter router, ResultPanel panel, string id, ChallengeAttemptMode mode =
            ChallengeAttemptMode.Journey)
        {
            ChallengeDefinition definition = router.Session.Journey.Catalog.Get(id);
            Assert.That(router.TryStartChallenge(id, mode, definition.Difficulty), Is.True, id);
            router.ReportChallengeResultForTests(new ChallengeAttemptResult(router.Session.Journey.ActiveAttempt,
                true, new ChallengeMetrics(elapsed: 40f, completedTargets: definition.TargetCount),
                ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null));
            Assert.That(panel.CurrentResult, Is.Not.Null);
        }
    }
}
