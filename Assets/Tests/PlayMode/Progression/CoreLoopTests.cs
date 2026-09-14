using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class CoreLoopTests
    {
        [Test]
        public void PreviewRoute_FailureReturnsMapWithoutMutation()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            var result = new MinigameResult(false, 0f, Rank.F);

            Assert.That(session.PreviewRoute(SubjectId.Sprint, result), Is.EqualTo(SessionRoute.Map));
            Assert.That(session.Lives, Is.EqualTo(5));
            Assert.That(session.PendingPunishmentSubject, Is.Null);
        }

        [Test]
        public void ResultPanel_ContinueEmitsActionOnlyOnce()
        {
            var panel = new GameObject().AddComponent<ResultPanel>();
            try
            {
                var calls = 0;
                panel.ActionRequested += _ => calls++;
                panel.Show(new MinigameResult(true, 8f, Rank.A), "Map");
                panel.Continue();
                panel.Continue();

                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(panel.gameObject);
            }
        }

        [Test]
        public void ResultPanel_ContinueEmitsActionOnlyOnceWithSprintResultPresentationAttached()
        {
            var root = new GameObject("result-panel");
            var panel = root.AddComponent<ResultPanel>();
            var presentation = root.AddComponent<SprintResultPresentation>();
            try
            {
                var calls = 0;
                string route = null;
                panel.ActionRequested += r => { calls++; route = r; };

                panel.Show(new MinigameResult(false, 0f, Rank.F), "Map");
                presentation.Bind(panel, null);
                presentation.ShowForTest(panel.CurrentResult);

                panel.Continue();
                panel.Continue();

                Assert.That(calls, Is.EqualTo(1));
                Assert.That(route, Is.EqualTo("Map"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResultPanel_ShowActivatesAnInactivePanelRoot()
        {
            var root = new GameObject("result-panel");
            var panel = root.AddComponent<ResultPanel>();
            try
            {
                root.SetActive(false);

                panel.Show(new MinigameResult(true, 8f, Rank.A), "Map");

                Assert.That(root.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
