using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    public sealed class BossPhaseController : MonoBehaviour
    {
        [SerializeField] BossSequenceAsset bossSequenceAsset;
        [SerializeField] float configuredDuration = 35f;

        GameSession session;
        ChallengeSequence sequence;
        float remainingSeconds;
        bool running;

        public event Action<MinigameResult> Resolved;

        public ChallengeStep Current => sequence == null ? default : sequence.Current;
        public ChallengeMechanic CurrentMechanic => Current.Mechanic;
        public float RemainingSeconds => remainingSeconds;
        public bool IsRunning => running;
        public bool IsComplete => !running && Result != null;
        public MinigameResult Result { get; private set; }

        public void Configure(GameSession configuredSession, ChallengeSequence configuredSequence, float duration)
        {
            session = configuredSession ?? throw new ArgumentNullException(nameof(configuredSession));
            sequence = configuredSequence ?? throw new ArgumentNullException(nameof(configuredSequence));
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));

            configuredDuration = duration;
            remainingSeconds = duration;
            running = false;
            Result = null;
        }

        public void Configure(GameSession configuredSession, BossSequenceAsset configuredAsset, float duration)
        {
            if (configuredAsset == null)
                throw new ArgumentNullException(nameof(configuredAsset));

            Configure(configuredSession, configuredAsset.CreateRuntimeSequence(), duration);
        }

        public void Begin()
        {
            if (session == null || sequence == null)
                throw new InvalidOperationException("Boss phase is not configured.");
            if (!session.BossUnlocked)
                throw new InvalidOperationException("Pass all seven subjects before starting the boss.");
            if (running)
                throw new InvalidOperationException("The boss phase is already running.");

            remainingSeconds = configuredDuration;
            sequence.Reset();
            Result = null;
            running = true;
        }

        public void CompleteCurrent()
        {
            CompleteCurrent(CurrentMechanic);
        }

        public void CompleteCurrent(ChallengeMechanic mechanic)
        {
            if (!running)
                throw new InvalidOperationException("The boss phase is not running.");
            if (mechanic != CurrentMechanic)
                throw new InvalidOperationException($"Expected {CurrentMechanic}, received {mechanic}.");

            sequence.ReportProgress(sequence.Current.Target);
        }

        void Update()
        {
            if (!running)
                return;

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
                Resolve(false);
            else if (sequence.IsComplete)
                Resolve(true);
        }

        void Resolve(bool pass)
        {
            if (!running)
                return;

            running = false;
            Result = new MinigameResult(pass, pass ? configuredDuration - remainingSeconds : 0f,
                pass ? Rank.S : Rank.F);
            Resolved?.Invoke(Result);
        }
    }
}
