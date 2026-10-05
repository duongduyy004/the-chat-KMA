using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class FootballResultRoutingTests
    {
        readonly List<GameObject> roots = new List<GameObject>();
        SceneRouter router;
        ResultPanel panel;
        ControllerStub controller;
        readonly List<SceneRouteTransition> transitions = new List<SceneRouteTransition>();
        SaveData lastSaved;

        [SetUp]
        public void SetUp()
        {
            var existing = Object.FindFirstObjectByType<SceneRouter>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            router = SceneRouter.EnsurePersistentInstance();
            roots.Add(router.gameObject);
            var panelRoot = new GameObject("ResultPanel");
            roots.Add(panelRoot);
            panel = panelRoot.AddComponent<ResultPanel>();
            ButtonRefs buttons = BuildPanel(panelRoot);
            panel.Configure(buttons.root, buttons.status, buttons.goals, buttons.score, buttons.rank,
                buttons.lives, buttons.continueButton, buttons.retryButton);
            router.ConfigureRouteAcceptanceForTests((transition, complete) =>
            {
                transitions.Add(transition);
                complete();
                return true;
            });

            var session = new GameSession();
            CompleteThrough(session, "soccer_practice");
            router.LoadSession(session);
            router.ConfigureJourneyPersistence((out string error) =>
            {
                error = null;
                lastSaved = router.Session.ToSaveData();
                return true;
            });
            Assert.That(router.StartSubject(SubjectId.Football), Is.True);
            var controllerObject = new GameObject("FootballControllerStub");
            roots.Add(controllerObject);
            controller = controllerObject.AddComponent<ControllerStub>();
            router.BindSubject(controller, SubjectId.Football);
            Assert.That(controller.GetComponent<JourneyControllerAdapter>(), Is.Not.Null);
            Assert.That(router.Session.Journey.ActiveAttempt, Is.Not.Null);
            transitions.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var root in roots)
                if (root) Object.DestroyImmediate(root);
            roots.Clear();
        }

        [Test]
        public void ExamFailureIsPersistedWhenControllerCompletes()
        {
            int notifications = 0;
            router.Session.JourneyChanged += () => notifications++;

            controller.Complete(new MinigameResult(false, 0f, Rank.F));

            // A first exam failure costs nothing at commit: it owes a frog jump instead.
            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(router.Session.PendingFrogJump, Is.Not.Null);
            Assert.That(router.Session.Journey.ActiveAttempt, Is.Null);
            Assert.That(lastSaved.lives, Is.EqualTo(5));
            Assert.That(lastSaved.journey.pendingFrogJump, Is.Not.Null);
            Assert.That(lastSaved.journey.lastCommittedAttemptId, Is.Not.Null.And.Not.Empty);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(transitions, Is.Empty);
        }

        [Test]
        public void ContinueRoutesAfterCommitWithoutChargingAgain()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            transitions.Clear();

            panel.Continue();

            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(transitions.Count, Is.EqualTo(1));
            Assert.That(transitions[0].Route, Is.EqualTo(SessionRoute.FrogJump));
        }

        [Test]
        public void ReloadFromResultCheckpointDoesNotChargeAgain()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            var reloaded = new GameSession();
            reloaded.Restore(lastSaved);

            // The commit is not charged again; only the unfinished frog jump is forfeited (-1).
            Assert.That(reloaded.ForfeitedFrogJumpOnRestore, Is.True);
            Assert.That(reloaded.Lives, Is.EqualTo(4));
            Assert.That(reloaded.Journey.CheckpointChallengeId, Is.EqualTo("soccer_exam"));
            Assert.That(reloaded.Journey.ActiveAttempt, Is.Null);
        }

        [Test]
        public void RouteFailureKeepsCommittedResultForRetry()
        {
            controller.Complete(new MinigameResult(false, 0f, Rank.F));
            router.ConfigureRouteAcceptanceForTests((_, _) => false);

            panel.Continue();
            Assert.That(router.Session.Lives, Is.EqualTo(5));
            Assert.That(router.Session.PendingFrogJump, Is.Not.Null);
            Assert.That(router.Session.Journey.CheckpointChallengeId, Is.EqualTo("soccer_exam"));
        }

        static ButtonRefs BuildPanel(GameObject root)
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
            typeof(Image), typeof(Button)).GetComponent<Button>();
        static TMPro.TMP_Text Text(string name) => new GameObject(name).AddComponent<TMPro.TextMeshPro>();

        static void CompleteThrough(GameSession session, string target)
        {
            foreach (ChallengeDefinition definition in ChallengeCatalog.LoadDefault().Ordered)
            {
                if (session.Journey.IsChallengeComplete(definition.Id)) continue;
                session.TryStartChallenge(definition.Id, ChallengeAttemptMode.Journey,
                    definition.Difficulty, out ChallengeAttemptContext context);
                var result = new ChallengeAttemptResult(context, true,
                    new ChallengeMetrics(completedTargets: definition.TargetCount),
                    ChallengeDefinition.IsScored(definition.Kind) ? new MinigameResult(true, 8f, Rank.A) : null);
                session.SubmitChallengeResult(result);
                if (definition.Id == target) return;
            }
        }

        readonly struct ButtonRefs
        {
            public ButtonRefs(GameObject root, TMPro.TMP_Text status, TMPro.TMP_Text goals,
                TMPro.TMP_Text score, TMPro.TMP_Text rank, TMPro.TMP_Text lives,
                Button continueButton, Button retryButton)
            { this.root=root; this.status=status; this.goals=goals; this.score=score; this.rank=rank; this.lives=lives;
              this.continueButton=continueButton; this.retryButton=retryButton; }
            public readonly GameObject root;
            public readonly TMPro.TMP_Text status, goals, score, rank, lives;
            public readonly Button continueButton, retryButton;
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
