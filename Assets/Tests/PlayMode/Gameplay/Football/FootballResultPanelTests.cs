using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballResultPanelTests
    {
        GameObject root;
        FootballResultPanel panel;
        Button continueButton, retryButton;
        TMP_Text status, goals, score, rank, lives;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FootballResultPanel");
            panel = root.AddComponent<FootballResultPanel>();
            continueButton = NewButton("Continue");
            retryButton = NewButton("Retry");
            status = NewText("Status"); goals = NewText("Goals"); score = NewText("Score");
            rank = NewText("Rank"); lives = NewText("Lives");
            continueButton.transform.SetParent(root.transform, false);
            retryButton.transform.SetParent(root.transform, false);
            status.transform.SetParent(root.transform, false); goals.transform.SetParent(root.transform, false);
            score.transform.SetParent(root.transform, false); rank.transform.SetParent(root.transform, false);
            lives.transform.SetParent(root.transform, false);
            panel.Configure(root, status, goals, score, rank, lives, continueButton, retryButton);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void FailureShowsRetryOnlyWhenLivesRemainAndReportsScoreAndGoals()
        {
            panel.SetGoals(2);
            panel.ConfigureRetry(2);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(goals.text, Is.EqualTo("2/5"));
            Assert.That(lives.text, Is.EqualTo("2"));
            Assert.That(retryButton.gameObject.activeSelf, Is.True);
            Assert.That(continueButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ExhaustedFailureHidesRetryAndPendingActionCanRecoverAfterError()
        {
            panel.ConfigureRetry(0);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "GameOver");
            Assert.That(retryButton.gameObject.activeSelf, Is.False);

            int actions = 0;
            panel.ActionRequested += _ => actions++;
            panel.SetActionPending(true, null);
            panel.Retry();
            Assert.That(actions, Is.Zero);
            Assert.That(continueButton.interactable, Is.False);
            panel.SetActionPending(false, "Could not load scene");
            Assert.That(continueButton.interactable, Is.True);
            panel.Continue();
            Assert.That(actions, Is.EqualTo(1));
        }

        [Test]
        public void ContinueAndRetryEmitNamedActionsOnlyOnce()
        {
            panel.ConfigureRetry(3);
            panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
            string action = null;
            panel.ActionRequested += requested => action = requested;
            panel.Retry();
            panel.Retry();
            Assert.That(action, Is.EqualTo(ResultPanelActions.Retry));

            panel.Show(new MinigameResult(true, 8f, Rank.A), "Map");
            action = null;
            panel.Continue();
            panel.Continue();
            Assert.That(action, Is.EqualTo(ResultPanelActions.Continue));
            Assert.That(retryButton.gameObject.activeSelf, Is.False);
        }

        static Button NewButton(string name) => new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
        TMP_Text NewText(string name) => new GameObject(name).AddComponent<TextMeshPro>();
    }
}
