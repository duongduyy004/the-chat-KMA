using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class GameOverPresentationTests
    {
        GameObject root;
        GameOverScreen screen;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("game-over", typeof(RectTransform));
            screen = root.AddComponent<GameOverScreen>();
            foreach (var name in new[] { "RETRYButton", "NEW GAMEButton", "MAIN MENUButton" })
            {
                var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(root.transform, false);
            }
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void BuildIsIdempotent()
        {
            GameOverPresentationBuilder.Build(screen, new GameSession());
            GameOverPresentationBuilder.Build(screen, new GameSession());

            Assert.That(root.transform.Cast<Transform>().Count(t => t.name == "GameOverLayout"), Is.EqualTo(1));
        }

        [Test]
        public void ButtonsKeepTheirListenersAndAreCentered()
        {
            int retries = 0;
            screen.RetryRequested += () => retries++;
            screen.RetryRequested += () => retries++;
            var retry = root.GetComponentsInChildren<Button>(true).First(b => b.name == "RETRYButton");
            retry.onClick.AddListener(screen.Retry);

            GameOverPresentationBuilder.Build(screen, new GameSession());
            retry.onClick.Invoke();

            Assert.That(retries, Is.EqualTo(2));
            var rect = (RectTransform)retry.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
            Assert.That(rect.anchoredPosition.x, Is.EqualTo(0f));
            Assert.That(retry.GetComponent<HomeMenuButton>(), Is.Not.Null);
        }

        [Test]
        public void NoTextFallsBelowTheFontFloor()
        {
            GameOverPresentationBuilder.Build(screen, new GameSession());

            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                float smallest = label.enableAutoSizing ? label.fontSizeMin : label.fontSize;
                Assert.That(smallest, Is.GreaterThanOrEqualTo(MinigameUiTheme.MinimumFontSize), label.name);
            }
        }

        [Test]
        public void StatsReflectTheEndedSession()
        {
            var session = new GameSession();
            var empty = GameOverStats.From(session);
            Assert.That(empty.SubjectsPassed, Is.EqualTo(0));
            Assert.That(empty.FailedVisits, Is.EqualTo(0));
            Assert.That(empty.TotalScore, Is.EqualTo(0));
            Assert.That(empty.SubjectsTotal, Is.GreaterThan(0));

            var missing = GameOverStats.From(null);
            Assert.That(missing.SubjectsPassed, Is.EqualTo(0));
        }
    }
}
