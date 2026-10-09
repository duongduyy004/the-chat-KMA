using System.Collections;
using System.Text.RegularExpressions;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class VolleyballControllerTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root)
                Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator MissingReferencesLogOnceAndDisable()
        {
            root = new GameObject("Controller");
            var controller = root.AddComponent<VolleyballController>();
            LogAssert.Expect(LogType.Error, new Regex("VolleyballController is missing"));
            yield return null;
            yield return null;
            Assert.That(controller.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator PressesDuringPlayReachTheMatchAndCompletionFiresOnce()
        {
            root = new GameObject("Controller");
            root.SetActive(false);
            var input = root.AddComponent<VolleyballInputBridge>();
            var controller = root.AddComponent<VolleyballController>();
            VolleyAthleteView player = View("Player"), opponent = View("Opponent");
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.transform.SetParent(root.transform);
            var hud = root.AddComponent<VolleyballHud>();
            controller.Configure(player, opponent, ballView, input, hud);
            root.SetActive(true);
            int completions = 0;
            controller.Completed += _ => completions++;
            yield return null;

            controller.SkipToPlayForTest();
            input.FeedActionForTest();
            yield return null;
            Assert.That(controller.Match.BallState, Is.EqualTo(BallState.Toss));

            // The toss above is still in the air, so this one long tick drops it: the opponent's
            // fifth point ends the match (there is no clock).
            controller.Match.SetScoreForTest(1, 4);
            controller.Match.Tick(10f);
            controller.Match.Tick(10f);
            yield return null;
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(controller.LastResult.Pass, Is.False);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
        }

        [UnityTest]
        public IEnumerator JumpPressesReachTheMatch()
        {
            root = new GameObject("Controller");
            root.SetActive(false);
            var input = root.AddComponent<VolleyballInputBridge>();
            var controller = root.AddComponent<VolleyballController>();
            VolleyAthleteView player = View("Player"), opponent = View("Opponent");
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.transform.SetParent(root.transform);
            var hud = root.AddComponent<VolleyballHud>();
            controller.Configure(player, opponent, ballView, input, hud);
            root.SetActive(true);
            yield return null;

            controller.SkipToPlayForTest();
            controller.Match.ForceServerForTest(CourtSide.Opponent);
            while (controller.Match.BallState != BallState.InPlay)
                controller.Match.Tick(1f / 60f);

            input.FeedJumpForTest();
            yield return null;
            Assert.That(controller.Match.Player.IsAirborne, Is.True);
        }

        VolleyAthleteView View(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            var renderer = go.AddComponent<SpriteRenderer>();
            var book = go.AddComponent<SpriteFlipbook>();
            var view = go.AddComponent<VolleyAthleteView>();
            var frames = new[] { Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.zero) };
            view.Configure(renderer, book, false, frames, frames, frames, frames, frames, frames);
            return view;
        }
    }
}
