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

        [Test]
        public void BackgroundAddsIllustrationAndWashAfterTheOpaqueVeil()
        {
            var tex = new Texture2D(40, 20);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 40, 20), new Vector2(.5f, .5f));
            screen.SetBackground(sprite);
            GameOverPresentationBuilder.Build(screen, new GameSession());

            var veil = screen.transform.Find("GameOverVeil");
            Assert.That(veil.GetComponent<Image>().color, Is.EqualTo(HomeMenuStyle.Navy));
            var art = veil.Find("Illustration").GetComponent<Image>();
            Assert.That(art.sprite, Is.SameAs(sprite));
            Assert.That(art.rectTransform.sizeDelta.x / art.rectTransform.sizeDelta.y, Is.EqualTo(2f).Within(.01f));
            var wash = veil.Find("Wash").GetComponent<Image>();
            Assert.That(wash.color.a, Is.EqualTo(.78f).Within(.001f));
            Assert.That(art.transform.GetSiblingIndex(), Is.LessThan(wash.transform.GetSiblingIndex()));
            Object.DestroyImmediate(sprite); Object.DestroyImmediate(tex);
        }

        [Test]
        public void BackgroundIsAddedOnceToAnAlreadyBuiltLayout()
        {
            GameOverPresentationBuilder.Build(screen, new GameSession());
            var tex = new Texture2D(40, 20);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 40, 20), new Vector2(.5f, .5f));
            screen.SetBackground(sprite);
            GameOverPresentationBuilder.Build(screen, new GameSession());
            GameOverPresentationBuilder.Build(screen, new GameSession());

            var veil = screen.transform.Find("GameOverVeil");
            Assert.That(veil.Cast<Transform>().Count(t => t.name == "Illustration"), Is.EqualTo(1));
            Object.DestroyImmediate(sprite); Object.DestroyImmediate(tex);
        }
    }
}
