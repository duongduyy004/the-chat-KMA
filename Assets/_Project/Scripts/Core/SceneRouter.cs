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

        [SerializeField] string punishmentScene;
        [SerializeField] string mapScene;
        [SerializeField] string gameOverScene;
        [SerializeField] string bossScene = "MG_Boss";
        [SerializeField] SubjectScene[] subjectScenes = Array.Empty<SubjectScene>();

        readonly Dictionary<MinigameBase, Action<MinigameResult>> subjectCompletionHandlers =
            new Dictionary<MinigameBase, Action<MinigameResult>>();
        BossPhaseController boundBoss;
        Action<MinigameResult> bossCompletionHandler;
        GameSession session;
        SessionRouteTransitioner transitioner;

        public GameSession Session => session;
        public bool IsTransitioning => transitioner != null && transitioner.IsTransitioning;

        void Awake()
        {
            session = new GameSession();
            transitioner = new SessionRouteTransitioner(session, this);
        }

        void OnDestroy()
        {
            foreach (var binding in subjectCompletionHandlers)
                binding.Key.Completed -= binding.Value;
            subjectCompletionHandlers.Clear();

            if (boundBoss != null)
                boundBoss.Completed -= bossCompletionHandler;
        }

        public bool StartSubject(SubjectId subject) => Route(session.StartSubject(subject), subject);

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
                throw new InvalidOperationException("The minigame controller is already bound.");

            Action<MinigameResult> handler = result => SubmitSubjectResult(subject, result);
            subjectCompletionHandlers.Add(controller, handler);
            controller.Completed += handler;
        }

        public void BindBoss(BossPhaseController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));
            if (boundBoss != null)
                throw new InvalidOperationException("A boss controller is already bound.");

            boundBoss = controller;
            bossCompletionHandler = _ => CompleteBoss();
            boundBoss.Completed += bossCompletionHandler;
        }

        public bool Route(SessionRoute route, SubjectId? subject = null)
        {
            if (!TryResolveScene(route, subject, out var sceneName))
                return false;

            return transitioner.TryRoute(route, subject, sceneName);
        }

        public void Begin(SceneRouteTransition transition, Action onCompleted)
        {
            StartCoroutine(LoadGameplayScene(transition.SceneName, onCompleted));
        }

        bool TryResolveScene(SessionRoute route, SubjectId? subject, out string sceneName)
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
            {
                Debug.LogError($"No scene is configured for {route}.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' for {route} is not enabled in Build Settings.", this);
                return false;
            }

            return true;
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
    }
}
