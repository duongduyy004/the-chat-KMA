using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    [DefaultExecutionOrder(-100)]
    public sealed class BossSceneSessionHandoff : MonoBehaviour
    {
        static GameSession pendingSession;
        static event Action<GameSession> PendingSessionChanged;

        public GameSession Session { get; private set; }
        public event Action<GameSession> SessionChanged;

        public static void SetPendingSession(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            pendingSession = session;
            PendingSessionChanged?.Invoke(session);
        }

        public static void ClearPendingSession() => pendingSession = null;

        void Awake()
        {
            Session = pendingSession ?? new GameSession();
            PendingSessionChanged += OnPendingSessionChanged;
        }

        void OnDestroy() => PendingSessionChanged -= OnPendingSessionChanged;

        public void SetSession(GameSession configuredSession)
        {
            if (configuredSession == null)
                throw new ArgumentNullException(nameof(configuredSession));

            SetPendingSession(configuredSession);
        }

        void OnPendingSessionChanged(GameSession configuredSession)
        {
            Session = configuredSession;
            SessionChanged?.Invoke(Session);
        }
    }
}
