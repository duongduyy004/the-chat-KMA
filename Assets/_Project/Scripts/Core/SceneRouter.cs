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
        SubjectId? activeSubject;
        bool awaitingSubjectScene;
        bool awaitingBossScene;
        GameSession session;
        SessionRouteTransitioner transitioner;

        public event Action<SceneRouteTransition> TransitionStarted;

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
        }

        public bool StartSubject(SubjectId subject)
        {
            EnsureRouteIsConfigured(SessionRoute.Subject, subject);
            return Route(session.StartSubject(subject), subject);
        }

        public bool SubmitSubjectResult(SubjectId subject, MinigameResult result) =>
            Route(session.SubmitResult(subject, result), subject);

        public bool CompletePunishment(SubjectId subject) => Route(session.CompletePunishment(), subject);

        public bool StartBoss() => Route(SessionRoute.Boss, null);

        public bool CompleteBoss() => Route(SessionRoute.Map, null);

        public void BindSubject(MinigameBase controller, SubjectId subject)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (subjectCompletionHandlers.ContainsKey(controller))
                return;

            Action<MinigameResult> handler = result => SubmitSubjectResult(subject, result);
            subjectCompletionHandlers.Add(controller, handler);
            controller.Completed += handler;
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

            return Application.CanStreamedLevelBeLoaded(sceneName);
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
                foreach (var controller in FindObjectsByType<MinigameBase>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!(controller is BossPhaseController))
                        BindSubject(controller, activeSubject.Value);
                }
                awaitingSubjectScene = false;
            }

            if (awaitingBossScene && string.Equals(scene.name, bossScene, StringComparison.Ordinal))
            {
                foreach (var boss in FindObjectsByType<BossPhaseController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!ReferenceEquals(boss.Session, session))
                        boss.SetSession(session);
                    BindBoss(boss);
                }
                awaitingBossScene = false;
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
            new SubjectScene { Subject = SubjectId.Endurance, SceneName = "MG_Endurance" }
        };
    }
}
