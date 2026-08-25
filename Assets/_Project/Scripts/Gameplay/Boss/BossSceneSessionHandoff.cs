using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    [DefaultExecutionOrder(-100)]
    public sealed class BossSceneSessionHandoff : MonoBehaviour
    {
        [SerializeField] bool seedCanonicalUnlockForStandaloneScene;

        public GameSession Session { get; private set; }

        void Awake()
        {
            Session = new GameSession();
            if (seedCanonicalUnlockForStandaloneScene)
                SeedCanonicalPrerequisites(Session);
        }

        public void SetSession(GameSession configuredSession)
        {
            if (configuredSession == null)
                throw new ArgumentNullException(nameof(configuredSession));

            Session = configuredSession;
        }

        static void SeedCanonicalPrerequisites(GameSession session)
        {
            foreach (SubjectId id in Enum.GetValues(typeof(SubjectId)))
            {
                session.StartSubject(id);
                session.SubmitResult(id, new MinigameResult(true, 6, Rank.C));
            }
        }
    }
}
