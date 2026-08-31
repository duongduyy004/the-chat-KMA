using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class CoreLoopTests
    {
        [Test]
        public void PreviewRoute_FirstFailureReturnsPunishmentWithoutMutation()
        {
            var session = new GameSession();
            session.StartSubject(SubjectId.Sprint);
            var result = new MinigameResult(false, 0f, Rank.F);

            Assert.That(session.PreviewRoute(SubjectId.Sprint, result), Is.EqualTo(SessionRoute.Punishment));
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
