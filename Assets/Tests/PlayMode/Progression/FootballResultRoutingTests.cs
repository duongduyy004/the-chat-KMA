using System;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FootballResultRoutingTests
    {
        readonly List<GameObject> roots = new List<GameObject>();
        SceneRouter router;
        FootballResultPanel panel;
        ControllerStub controller;
        readonly List<SceneRouteTransition> transitions = new List<SceneRouteTransition>();
        int lifeLost, saved;

        [SetUp]
        public void SetUp()
        {
            foreach (var existing in UnityEngine.Object.FindObjectsByType<SceneRouter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            router = SceneRouter.EnsurePersistentInstance();
            roots.Add(router.gameObject);
            var panelRoot = new GameObject("FootballResultPanel");
            roots.Add(panelRoot);
            panel = panelRoot.AddComponent<FootballResultPanel>();
            ButtonRefs buttons = BuildPanel(panelRoot, panel);
            panel.Configure(buttons.root, buttons.status, buttons.goals, buttons.score, buttons.rank,
                buttons.lives, buttons.continueButton, buttons.retryButton);
            router.ConfigureRouteAcceptanceForTests((transition, completed) =>
            {
                transitions.Add(transition);
                completed();
                return true;
            });
            LoadLives(5);
            Assert.That(router.StartSubject(SubjectId.Football), Is.True);
            var controllerObject = new GameObject("FootballControllerStub");
            roots.Add(controllerObject);
            controller = controllerObject.AddComponent<ControllerStub>();
            router.BindSubject(controller, SubjectId.Football);
            router.LifeLost += _ => lifeLost++;
            router.SessionChanged += () => saved++;
            transitions.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var root in roots)
                if (root) UnityEngine.Object.DestroyImmediate(root);
            roots.Clear();
        }

        [Test]
        public void RetryConsumesOneLifeAndRoutesDirectlyBackToFootballOnce()
        {
            LoadLives(2);
            router.StartSubject(SubjectId.Football);
            transitions.Clear(); saved = 0; lifeLost = 0;
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            Assert.That(panel.RetryAvailable, Is.True);
            panel.Retry();
            panel.Retry();

            Assert.That(router.Session.Lives, Is.EqualTo(1));
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Football));
            Assert.That(transitions.Count, Is.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Subject));
            Assert.That(transitions[0].SceneName, Is.EqualTo("MG_Football"));
            Assert.That(transitions[0].Subject, Is.EqualTo(SubjectId.Football));
            Assert.That(lifeLost, Is.EqualTo(1));
            Assert.That(saved, Is.EqualTo(1));
        }

        [Test]
        public void BackWhileFailureIsPendingCommitsFailureExactlyOnce()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            transitions.Clear(); saved = 0; lifeLost = 0;
            Assert.That(router.ExitActiveSubjectToMap(), Is.True);
            Assert.That(router.Session.Lives, Is.EqualTo(4));
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(transitions.Count, Is.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Map));
            Assert.That(lifeLost, Is.EqualTo(1));
            Assert.That(saved, Is.EqualTo(1));
        }

        [Test]
        public void LastLifeFailureRoutesGameOverAndDoesNotOfferRetry()
        {
            LoadLives(1);
            router.StartSubject(SubjectId.Football);
            transitions.Clear(); lifeLost = 0;
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            Assert.That(panel.RetryAvailable, Is.False);
            panel.Continue();

            Assert.That(router.Session.Lives, Is.Zero);
            Assert.That(router.Session.ActiveSubject, Is.Null);
            Assert.That(transitions.Count, Is.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.GameOver));
            Assert.That(lifeLost, Is.EqualTo(1));
        }

        [Test]
        public void RejectedSceneLoadRollsBackAndAllowsRetryAgain()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            var before = router.Session.ToSaveData();
            int failedVisitsBefore = router.Session.GetRecord(SubjectId.Football).FailedVisits;
            router.ConfigureSceneLoaderForTests(_ => null);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Could not load scene 'MG_Football'"));
            panel.Retry();
            Assert.That(router.Session.ToSaveData().lives, Is.EqualTo(before.lives));
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(before.activeSubject));
            Assert.That(router.Session.GetRecord(SubjectId.Football).FailedVisits, Is.EqualTo(failedVisitsBefore));
            Assert.That(panel.IsActionPending, Is.False);
            Assert.That(panel.ContinueInteractable, Is.True);

            transitions.Clear();
            router.ConfigureRouteAcceptanceForTests((transition, completed) =>
            {
                transitions.Add(transition);
                completed();
                return true;
            });
            panel.Retry();
            Assert.That(router.Session.Lives, Is.EqualTo(before.lives - 1));
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Football));
            Assert.That(router.Session.GetRecord(SubjectId.Football).FailedVisits, Is.EqualTo(failedVisitsBefore + 1));
            Assert.That(transitions.Count, Is.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.Subject));
        }

        [Test]
        public void ThrowingSceneLoaderRollsBackAndKeepsResultActionsAvailable()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            int livesBefore = router.Session.Lives;
            int failedVisitsBefore = router.Session.GetRecord(SubjectId.Football).FailedVisits;
            router.ConfigureSceneLoaderForTests(_ => throw new InvalidOperationException("synthetic load failure"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Could not load scene 'MG_Football': synthetic load failure"));

            panel.Retry();

            Assert.That(router.Session.Lives, Is.EqualTo(livesBefore));
            Assert.That(router.Session.ActiveSubject, Is.EqualTo(SubjectId.Football));
            Assert.That(router.Session.GetRecord(SubjectId.Football).FailedVisits, Is.EqualTo(failedVisitsBefore));
            Assert.That(panel.IsActionPending, Is.False);
            Assert.That(panel.ContinueInteractable, Is.True);
            Assert.That(panel.RetryAvailable, Is.True);
        }

        void LoadLives(int lives)
        {
            var data = SaveData.CreateDefault();
            data.lives = lives;
            var session = new GameSession();
            session.Restore(data);
            router.LoadSession(session);
            if (controller) router.BindSubject(controller, SubjectId.Football);
        }

        static ButtonRefs BuildPanel(GameObject root, FootballResultPanel panel)
        {
            var continueButton = Button("Continue");
            var retryButton = Button("Retry");
            var status = Text("Status"); var goals = Text("Goals"); var score = Text("Score");
            var rank = Text("Rank"); var lives = Text("Lives");
            continueButton.transform.SetParent(root.transform, false);
            retryButton.transform.SetParent(root.transform, false);
            status.transform.SetParent(root.transform, false); goals.transform.SetParent(root.transform, false);
            score.transform.SetParent(root.transform, false); rank.transform.SetParent(root.transform, false);
            lives.transform.SetParent(root.transform, false);
            return new ButtonRefs(root, status, goals, score, rank, lives, continueButton, retryButton);
        }

        static Button Button(string name) => new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>();
        static TMPro.TMP_Text Text(string name) => new GameObject(name).AddComponent<TMPro.TextMeshPro>();

        readonly struct ButtonRefs
        {
            public ButtonRefs(GameObject root, TMPro.TMP_Text status, TMPro.TMP_Text goals,
                TMPro.TMP_Text score, TMPro.TMP_Text rank, TMPro.TMP_Text lives,
                UnityEngine.UI.Button continueButton, UnityEngine.UI.Button retryButton)
            { this.root=root; this.status=status; this.goals=goals; this.score=score; this.rank=rank; this.lives=lives;
              this.continueButton=continueButton; this.retryButton=retryButton; }
            public readonly GameObject root;
            public readonly TMPro.TMP_Text status, goals, score, rank, lives;
            public readonly UnityEngine.UI.Button continueButton, retryButton;
        }

        public sealed class ControllerStub : MinigameBase
        {
            protected override void TickPlay(float dt) { }
            public void Complete(MinigameResult result)
            {
                SetTutorialGate(false);
                Lifecycle.Tick(5f);
                Finish(result);
            }
        }
    }
}
