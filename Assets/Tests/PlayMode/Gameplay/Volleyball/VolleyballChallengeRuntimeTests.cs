using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballChallengeRuntimeTests
    {
        GameObject root;
        GameObject viewRoot;

        [UnityTest]
        public IEnumerator ExamDeadlineProducesOneTypedScoredResult()
        {
            root = new GameObject("JourneyVolleyballController");
            root.SetActive(false);
            var input = root.AddComponent<VolleyballInputBridge>();
            var controller = root.AddComponent<VolleyballController>();
            viewRoot = new GameObject("VolleyballViews");
            VolleyAthleteView player = View("Player"), opponent = View("Opponent");
            var ball = new GameObject("Ball").AddComponent<VolleyBallView>();
            ball.transform.SetParent(viewRoot.transform);
            var hud = root.AddComponent<VolleyballHud>();
            controller.Configure(player, opponent, ball, input, hud);
            root.SetActive(true);

            ChallengeDefinition definition = ChallengeCatalog.LoadDefault().Get("volleyball_exam");
            var session = new GameSession();
            Complete(session, "sprint_learn");
            Complete(session, "sprint_practice");
            Complete(session, "sprint_exam");
            Complete(session, "volleyball_learn");
            Complete(session, "volleyball_practice");
            Assert.That(session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey,
                definition.Difficulty, out ChallengeAttemptContext context), Is.True);
            controller.ConfigureChallenge(definition, context);
            int typed = 0;
            ChallengeAttemptResult result = null;
            controller.ChallengeCompleted += value => { typed++; result = value; };
            yield return null;
            controller.SkipToPlayForTest();

            // An idle player loses every rally; the exam ends on the opponent's tenth point.
            for (int i = 0; i < 600 * 60 && !controller.Match.IsOver; i++) controller.Match.Tick(1f / 60f);
            yield return null;
            Assert.That(controller.Match.OpponentPoints, Is.EqualTo(10));
            Assert.That(typed, Is.EqualTo(1));
            Assert.That(result.Pass, Is.False);
            Assert.That(result.ExamResult, Is.Not.Null);
            Assert.That(session.SubmitChallengeResult(result).Accepted, Is.True);
            Assert.That(session.Journey.CheckpointChallengeId, Is.EqualTo("volleyball_exam"));
        }

        // On device the PhaseOverlay can open the start gate before the router binds the scene;
        // a throw there aborted every later sceneLoaded handler, pause exit included.
        [UnityTest]
        public IEnumerator ChallengeCanBeConfiguredOnceTheCountdownHasStarted()
        {
            root = new GameObject("JourneyVolleyballController");
            root.SetActive(false);
            var input = root.AddComponent<VolleyballInputBridge>();
            var controller = root.AddComponent<VolleyballController>();
            viewRoot = new GameObject("VolleyballViews");
            VolleyAthleteView player = View("Player"), opponent = View("Opponent");
            var ball = new GameObject("Ball").AddComponent<VolleyBallView>();
            ball.transform.SetParent(viewRoot.transform);
            var hud = root.AddComponent<VolleyballHud>();
            controller.Configure(player, opponent, ball, input, hud);
            root.SetActive(true);
            controller.SetTutorialGate(false);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));

            var session = new GameSession();
            Complete(session, "sprint_learn");
            Complete(session, "sprint_practice");
            Complete(session, "sprint_exam");
            ChallengeDefinition definition = ChallengeCatalog.LoadDefault().Get("volleyball_learn");
            Assert.That(session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey,
                definition.Difficulty, out ChallengeAttemptContext context), Is.True);
            Assert.DoesNotThrow(() => controller.ConfigureChallenge(definition, context));
            yield return null;
        }

        VolleyAthleteView View(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(viewRoot.transform);
            var renderer = go.AddComponent<SpriteRenderer>();
            var book = go.AddComponent<SpriteFlipbook>();
            var view = go.AddComponent<VolleyAthleteView>();
            var texture = new Texture2D(4, 4);
            var frames = new[] { Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero) };
            view.Configure(renderer, book, false, frames, frames, frames, frames, frames, frames);
            return view;
        }

        static void Complete(GameSession session, string id)
        {
            ChallengeDefinition item = session.Journey.Catalog.Get(id);
            Assert.That(session.TryStartChallenge(id, ChallengeAttemptMode.Journey, item.Difficulty,
                out ChallengeAttemptContext context), Is.True);
            Assert.That(session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                new ChallengeMetrics(completedTargets: item.TargetCount))).Accepted, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root) Object.Destroy(root);
            if (viewRoot) Object.Destroy(viewRoot);
            yield return null;
        }
    }
}
