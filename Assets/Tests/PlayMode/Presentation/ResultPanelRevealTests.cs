using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class ResultPanelRevealTests
    {
        GameObject root;
        GameObject content;
        ResultPanel panel;
        TMP_Text status, score, rank;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ResultPanel", typeof(RectTransform));
            content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            Button cont = UiKit.Button(content.transform, "ActionButton", "TIẾP TỤC", ButtonVariant.Primary).Button;
            Button retry = UiKit.Button(content.transform, "RetryButton", "CHƠI LẠI", ButtonVariant.Primary).Button;
            status = Text("StatusLabel"); score = Text("ScoreLabel"); rank = Text("RankLabel");
            TMP_Text detail = Text("DetailLabel"), lives = Text("LivesLabel"), error = Text("ErrorLabel");
            panel = root.AddComponent<ResultPanel>();
            panel.Configure(content, status, detail, score, rank, lives, cont, retry, error);
        }

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        [UnityTest]
        public IEnumerator RevealStartsFromZeroAndEndsInItsFinalState()
        {
            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            Assert.That(score.text, Is.EqualTo("0"), "the final score must not flash before the count-up");
            Assert.That(content.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));
            Assert.That(rank.rectTransform.localScale.x, Is.EqualTo(.6f).Within(.001f));

            yield return new WaitForSecondsRealtime(1.5f);

            Assert.That(score.text, Is.EqualTo("8"));
            Assert.That(content.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(content.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rank.rectTransform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(status.color.a, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ShowingAgainMidRevealEndsInTheSecondResultsState()
        {
            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            yield return new WaitForSecondsRealtime(.1f);
            panel.Show(new MinigameResult(false, 3f, Rank.F), "Map");

            yield return new WaitForSecondsRealtime(1.5f);

            Assert.That(score.text, Is.EqualTo("3"));
            Assert.That(status.text, Is.EqualTo("THẤT BẠI"));
            Assert.That(status.color.r, Is.EqualTo(MinigameUiTheme.Energy.r).Within(.001f));
            Assert.That(status.color.a, Is.EqualTo(1f));
            Assert.That(rank.text, Is.EqualTo("XẾP HẠNG F"));
            Assert.That(content.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(content.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rank.rectTransform.localScale, Is.EqualTo(Vector3.one));
        }

        TMP_Text Text(string name)
        {
            var text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(content.transform, false);
            return text;
        }
    }
}
