using System;
using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay.Boss;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Gameplay.Core
{
    public interface ISceneRouteTransitionSink
    {
        void Begin(SceneRouteTransition transition, Action onCompleted);
    }

    public readonly struct SceneRouteTransition
    {
        public SceneRouteTransition(SessionRoute route, SubjectId? subject, GameSession session,
            string sceneName)
        {
            Route = route;
            Subject = subject;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            SceneName = sceneName ?? string.Empty;
        }

        public SessionRoute Route { get; }
        public SubjectId? Subject { get; }
        public GameSession Session { get; }
        public string SceneName { get; }
    }

    public sealed class SessionRouteTransitioner
    {
        readonly GameSession session;
        readonly ISceneRouteTransitionSink sink;
        bool transitioning;

        public SessionRouteTransitioner(GameSession session, ISceneRouteTransitionSink sink)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool IsTransitioning => transitioning;

        public bool TryRoute(SessionRoute route, SubjectId? subject, string sceneName = null)
        {
            if (transitioning)
                return false;

            if (RequiresSubject(route) && !subject.HasValue)
                throw new ArgumentNullException(nameof(subject));
            if (route == SessionRoute.Boss && !session.BossUnlocked)
                throw new InvalidOperationException("Pass all seven subjects before starting the boss.");

            transitioning = true;
            try
            {
                if (route == SessionRoute.Boss)
                    BossSceneSessionHandoff.SetPendingSession(session);

                sink.Begin(new SceneRouteTransition(route, subject, session, sceneName ?? route.ToString()),
                    CompleteTransition);
                return true;
            }
            catch
            {
                transitioning = false;
                throw;
            }
        }

        void CompleteTransition() => transitioning = false;

        static bool RequiresSubject(SessionRoute route) => route == SessionRoute.Subject ||
            route == SessionRoute.Punishment || route == SessionRoute.RetrySubject;
    }

    public sealed class SceneRouter : MonoBehaviour, ISceneRouteTransitionSink
    {
        [Serializable]
        public struct SubjectScene
        {
            public SubjectId Subject;
            public string SceneName;
        }

        static SceneRouter instance;

        [SerializeField] string punishmentScene = "Punishment";
        [SerializeField] string mapScene = "Map";
        [SerializeField] string gameOverScene = "GameOver";
        [SerializeField] string bossScene = "MG_Boss";
        [SerializeField] SubjectScene[] subjectScenes = DefaultSubjectScenes();

        readonly Dictionary<MinigameBase, Action<MinigameResult>> subjectCompletionHandlers =
            new Dictionary<MinigameBase, Action<MinigameResult>>();
        readonly Dictionary<BossPhaseController, Action<MinigameResult>> bossCompletionHandlers =
            new Dictionary<BossPhaseController, Action<MinigameResult>>();
        IResultPreviewPanel pendingResultPanel;
        Action<string> pendingResultPanelHandler;
        SubjectId? activeSubject;
        bool awaitingSubjectScene;
        bool awaitingBossScene;
        GameSession session;
        SessionRouteTransitioner transitioner;

        public event Action<SceneRouteTransition> TransitionStarted;
        public event Action<SubjectId, MinigameResult> SubjectCompleted;
        public event Action<int> LifeLost;

        public static SceneRouter Instance => instance;
        public GameSession Session => session;
        public bool IsTransitioning => transitioner != null && transitioner.IsTransitioning;

        public static SceneRouter EnsurePersistentInstance()
        {
            if (instance != null)
                return instance;

            var existing = FindFirstObjectByType<SceneRouter>();
            if (existing != null)
                return existing;

            return new GameObject(nameof(SceneRouter)).AddComponent<SceneRouter>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            session = new GameSession();
            transitioner = new SessionRouteTransitioner(session, this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindSubjects();
            UnbindBosses();
            UnbindResultPanel();
        }

        public bool StartSubject(SubjectId subject)
        {
            EnsureRouteIsConfigured(SessionRoute.Subject, subject);
            return Route(session.StartSubject(subject), subject);
        }

        public void LoadSession(GameSession restoredSession)
        {
            if (restoredSession == null)
                throw new ArgumentNullException(nameof(restoredSession));

            UnbindSubjects();
            UnbindBosses();
            UnbindResultPanel();
            activeSubject = null;
            awaitingSubjectScene = false;
            awaitingBossScene = false;
            session = restoredSession;
            transitioner = new SessionRouteTransitioner(session, this);
        }

        public bool SubmitSubjectResult(SubjectId subject, MinigameResult result)
        {
            int livesBefore = session.Lives;
            SessionRoute route = session.SubmitResult(subject, result);
            bool routed = Route(route, subject);

            if (result.Pass)
                SubjectCompleted?.Invoke(subject, result);
            else if (session.Lives < livesBefore)
                LifeLost?.Invoke(session.Lives);

            return routed;
        }

        public bool CompletePunishment(SubjectId subject) => Route(session.CompletePunishment(), subject);

        public bool StartBoss() => Route(SessionRoute.Boss, null);

        public bool RouteToMenu()
        {
            if (IsTransitioning)
                return false;
            UnbindSubjects();
            UnbindBosses();
            UnbindResultPanel();
            activeSubject = null;
            awaitingSubjectScene = false;
            awaitingBossScene = false;
            SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            return true;
        }

        public bool RestartActiveSubject()
        {
            if (!activeSubject.HasValue || !session.ActiveSubject.HasValue)
                return false;
            var subject = activeSubject.Value;
            session.AbandonActiveSubject();
            return StartSubject(subject);
        }

        public bool ExitActiveSubjectToMap()
        {
            if (session.ActiveSubject.HasValue)
                session.AbandonActiveSubject();
            activeSubject = null;
            return Route(SessionRoute.Map);
        }

        public bool CompleteBoss() => Route(SessionRoute.Map, null);

        public void BindSubject(MinigameBase controller, SubjectId subject)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (subjectCompletionHandlers.ContainsKey(controller))
                return;

            Action<MinigameResult> handler = result => PreviewSubjectResult(subject, result);
            subjectCompletionHandlers.Add(controller, handler);
            controller.Completed += handler;
        }

        void PreviewSubjectResult(SubjectId subject, MinigameResult result)
        {
            SessionRoute previewRoute = session.PreviewRoute(subject, result);
            var panel = FindResultPanel();
            if (panel == null)
                throw new InvalidOperationException("A ResultPanel is required to continue from a subject result.");

            UnbindResultPanel();
            Action<string> handler = _ =>
            {
                UnbindResultPanel();
                SubmitSubjectResult(subject, result);
            };

            pendingResultPanel = panel;
            pendingResultPanelHandler = handler;
            panel.ActionRequested += handler;
            panel.Show(result, previewRoute.ToString());
        }

        public void BindBoss(BossPhaseController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (bossCompletionHandlers.ContainsKey(controller))
                return;

            Action<MinigameResult> handler = _ => CompleteBoss();
            bossCompletionHandlers.Add(controller, handler);
            controller.Completed += handler;
        }

        public bool Route(SessionRoute route, SubjectId? subject = null)
        {
            if (!TryGetSceneName(route, subject, out var sceneName))
                throw new InvalidOperationException($"No loadable scene is configured for {route}" +
                    (subject.HasValue ? $" ({subject.Value})." : "."));

            PrepareSceneBinding(route, subject);
            return transitioner.TryRoute(route, subject, sceneName);
        }

        public bool TryGetSceneName(SessionRoute route, SubjectId? subject, out string sceneName)
        {
            sceneName = route switch
            {
                SessionRoute.Punishment => punishmentScene,
                SessionRoute.Map => mapScene,
                SessionRoute.GameOver => gameOverScene,
                SessionRoute.Boss => bossScene,
                SessionRoute.Subject or SessionRoute.RetrySubject => SceneFor(subject),
                _ => null
            };

            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return true;
#if UNITY_EDITOR
            foreach (var buildScene in UnityEditor.EditorBuildSettings.scenes)
                if (buildScene.enabled && string.Equals(
                    System.IO.Path.GetFileNameWithoutExtension(buildScene.path), sceneName,
                    StringComparison.Ordinal))
                    return true;
#endif
            return false;
        }

        public void Begin(SceneRouteTransition transition, Action onCompleted)
        {
            TransitionStarted?.Invoke(transition);
            StartCoroutine(LoadGameplayScene(transition.SceneName, onCompleted));
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UnbindSubjects();
            UnbindBosses();

            if (awaitingSubjectScene && activeSubject.HasValue &&
                string.Equals(scene.name, SceneFor(activeSubject), StringComparison.Ordinal))
            {
                var boundController = false;
                foreach (var controller in FindObjectsByType<MinigameBase>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!(controller is BossPhaseController))
                    {
                        BindSubject(controller, activeSubject.Value);
                        boundController = true;
                    }
                }
                awaitingSubjectScene = !boundController;
            }

            if (awaitingBossScene && string.Equals(scene.name, bossScene, StringComparison.Ordinal))
            {
                var boundBoss = false;
                foreach (var boss in FindObjectsByType<BossPhaseController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!ReferenceEquals(boss.Session, session))
                        boss.SetSession(session);
                    BindBoss(boss);
                    boundBoss = true;
                }
                awaitingBossScene = !boundBoss;
            }
        }

        void EnsureRouteIsConfigured(SessionRoute route, SubjectId? subject)
        {
            if (!TryGetSceneName(route, subject, out _))
            {
                throw new InvalidOperationException($"No loadable scene is configured for {route}" +
                    (subject.HasValue ? $" ({subject.Value})." : "."));
            }
        }

        void PrepareSceneBinding(SessionRoute route, SubjectId? subject)
        {
            switch (route)
            {
                case SessionRoute.Subject:
                case SessionRoute.RetrySubject:
                    activeSubject = subject;
                    awaitingSubjectScene = true;
                    awaitingBossScene = false;
                    break;
                case SessionRoute.Boss:
                    awaitingSubjectScene = false;
                    awaitingBossScene = true;
                    break;
                case SessionRoute.Map:
                case SessionRoute.GameOver:
                    activeSubject = null;
                    awaitingSubjectScene = false;
                    awaitingBossScene = false;
                    break;
            }
        }

        void UnbindSubjects()
        {
            foreach (var binding in subjectCompletionHandlers)
                binding.Key.Completed -= binding.Value;
            subjectCompletionHandlers.Clear();
        }

        void UnbindBosses()
        {
            foreach (var binding in bossCompletionHandlers)
                binding.Key.Completed -= binding.Value;
            bossCompletionHandlers.Clear();
        }

        void UnbindResultPanel()
        {
            if (pendingResultPanel != null && pendingResultPanelHandler != null)
                pendingResultPanel.ActionRequested -= pendingResultPanelHandler;
            pendingResultPanel = null;
            pendingResultPanelHandler = null;
        }

        static IResultPreviewPanel FindResultPanel()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IResultPreviewPanel panel)
                    return panel;
            }

            return null;
        }

        string SceneFor(SubjectId? subject)
        {
            if (!subject.HasValue)
                return null;

            foreach (var entry in subjectScenes)
            {
                if (entry.Subject == subject.Value)
                    return entry.SceneName;
            }

            return null;
        }

        IEnumerator LoadGameplayScene(string sceneName, Action onCompleted)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;
            }

            onCompleted?.Invoke();
        }

        static SubjectScene[] DefaultSubjectScenes() => new[]
        {
            new SubjectScene { Subject = SubjectId.Sprint, SceneName = "MG_Sprint" },
            new SubjectScene { Subject = SubjectId.Endurance, SceneName = "MG_Endurance" },
            new SubjectScene { Subject = SubjectId.Volleyball, SceneName = "MG_Volleyball" },
            new SubjectScene { Subject = SubjectId.Basketball, SceneName = "MG_Basketball" },
            new SubjectScene { Subject = SubjectId.PingPong, SceneName = "MG_PingPong" },
            new SubjectScene { Subject = SubjectId.Badminton, SceneName = "MG_Badminton" },
            new SubjectScene { Subject = SubjectId.Football, SceneName = "MG_Football" }
        };
    }
}
