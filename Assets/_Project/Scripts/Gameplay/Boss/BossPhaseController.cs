using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    public sealed class BossPhaseController : MinigameBase
    {
        [SerializeField] BossSequenceAsset bossSequenceAsset;
        [SerializeField] BossTapMashDetectorAdapter tapMashDetector;
        [SerializeField] BossRhythmHoldDetectorAdapter rhythmHoldDetector;
        [SerializeField] BossAlternateTapDetectorAdapter alternateTapDetector;
        [SerializeField] float configuredDuration = 35f;

        ChallengeSequence sequence;
        float remainingSeconds;
        float remainingPhaseSeconds;
        float totalObjectiveTarget;
        float successfulInputs;
        int totalInputs;
        int completedPhases;
        BossTapSide expectedSide;
        bool running;
        bool terminalResolved;

        public BossSequenceAsset SequenceAsset => bossSequenceAsset;
        public GameSession Session { get; private set; }
        public BossTapMashDetectorAdapter TapMashDetector => tapMashDetector;
        public BossRhythmHoldDetectorAdapter RhythmHoldDetector => rhythmHoldDetector;
        public BossAlternateTapDetectorAdapter AlternateTapDetector => alternateTapDetector;
        public ChallengeStep Current => sequence == null ? default : sequence.Current;
        public ChallengeMechanic CurrentMechanic => Current.Mechanic;
        public float CurrentProgress => sequence == null ? 0f : sequence.CurrentProgress;
        public float RemainingSeconds => remainingSeconds;
        public float RemainingPhaseSeconds => remainingPhaseSeconds;
        public bool IsRunning => running;
        public bool IsComplete => Phase == MinigamePhase.Resolve;
        public bool ObjectiveComplete => sequence != null && sequence.IsComplete;
        public float Accuracy => totalInputs == 0 ? 0f : 2f * successfulInputs / totalInputs;
        public float Efficiency => totalObjectiveTarget <= 0f
            ? 0f : Mathf.Clamp01(successfulInputs / totalObjectiveTarget);
        public float Mastery => Mathf.Clamp01(completedPhases / (float)BossSequenceAsset.CanonicalStepCount);
        public MinigameResult LastResult { get; private set; }
        public int CompletionCount { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;

        protected override void Awake()
        {
            base.Awake();
            Session = new GameSession();
            try
            {
                InitializeSequence();
                InitializeDetectors();
            }
            catch (Exception exception)
            {
                enabled = false;
                Debug.LogException(exception, this);
            }
        }

        void OnDestroy() => UnsubscribeDetectors();

        public void SetSession(GameSession configuredSession)
        {
            if (configuredSession == null)
                throw new ArgumentNullException(nameof(configuredSession));
            if (running)
                throw new InvalidOperationException("The boss phase is already running.");

            Session = configuredSession;
        }

        public void Configure(GameSession configuredSession, BossSequenceAsset configuredAsset, float duration)
        {
            if (configuredAsset == null)
                throw new ArgumentNullException(nameof(configuredAsset));

            bossSequenceAsset = configuredAsset;
            Configure(configuredSession, configuredAsset.CreateRuntimeSequence(), duration);
        }

        public void Configure(GameSession configuredSession, ChallengeSequence configuredSequence, float duration)
        {
            if (configuredSession == null)
                throw new ArgumentNullException(nameof(configuredSession));
            if (configuredSequence == null)
                throw new ArgumentNullException(nameof(configuredSequence));
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));

            BossSequenceAsset.ValidateCanonical(configuredSequence);
            EnsureMatchesSerializedAsset(configuredSequence);
            Session = configuredSession;
            sequence = configuredSequence;
            configuredDuration = duration;
            ResetRuntimeState();
        }

        public void Begin()
        {
            if (Session == null || sequence == null)
                throw new InvalidOperationException("Boss phase is not initialized.");
            if (Phase != MinigamePhase.Play)
                throw new InvalidOperationException("Boss phase can begin only during the foundation Play phase.");
            if (!Session.BossUnlocked)
                throw new InvalidOperationException("Pass all seven subjects before starting the boss.");
            if (running)
                throw new InvalidOperationException("The boss phase is already running.");

            sequence.Reset();
            ResetRuntimeState();
            running = true;
            remainingSeconds = configuredDuration;
            remainingPhaseSeconds = sequence.Current.Duration;
        }

        protected override void TickPlay(float deltaTime)
        {
            if (!running)
                return;

            remainingSeconds -= deltaTime;
            remainingPhaseSeconds -= deltaTime;
            if (remainingSeconds <= 0f || remainingPhaseSeconds <= 0f)
            {
                Resolve(false);
                return;
            }

            if (sequence.IsComplete)
                Resolve(true);
        }

        void InitializeSequence()
        {
            if (bossSequenceAsset == null)
                throw new InvalidOperationException("MG_Boss requires a serialized BossSequenceAsset.");
            if (configuredDuration < 30f || configuredDuration > 40f)
                throw new InvalidOperationException("Boss duration must be between 30 and 40 seconds.");

            sequence = bossSequenceAsset.CreateRuntimeSequence();
            totalObjectiveTarget = 0f;
            for (var index = 0; index < sequence.Count; index++)
                totalObjectiveTarget += sequence.GetStep(index).Target;
            ResetRuntimeState();
        }

        void InitializeDetectors()
        {
            if (tapMashDetector == null || rhythmHoldDetector == null || alternateTapDetector == null)
                throw new InvalidOperationException("MG_Boss requires all three detector adapter components.");

            tapMashDetector.Tap += OnTapMash;
            rhythmHoldDetector.HoldCompleted += OnRhythmHold;
            alternateTapDetector.Tap += OnAlternateTap;
        }

        void EnsureMatchesSerializedAsset(ChallengeSequence configuredSequence)
        {
            if (bossSequenceAsset == null)
                throw new InvalidOperationException("Boss Configure requires the serialized BossSequenceAsset.");

            var authoredSequence = bossSequenceAsset.CreateRuntimeSequence();
            if (configuredSequence.Count != authoredSequence.Count)
                throw new InvalidOperationException("Boss Configure received a foreign challenge sequence.");

            for (var index = 0; index < authoredSequence.Count; index++)
            {
                var authored = authoredSequence.GetStep(index);
                var configured = configuredSequence.GetStep(index);
                if (authored.Mechanic != configured.Mechanic ||
                    !Mathf.Approximately(authored.Duration, configured.Duration) ||
                    !Mathf.Approximately(authored.Target, configured.Target))
                {
                    throw new InvalidOperationException("Boss Configure received a foreign challenge sequence.");
                }
            }
        }

        void UnsubscribeDetectors()
        {
            if (tapMashDetector != null) tapMashDetector.Tap -= OnTapMash;
            if (rhythmHoldDetector != null) rhythmHoldDetector.HoldCompleted -= OnRhythmHold;
            if (alternateTapDetector != null) alternateTapDetector.Tap -= OnAlternateTap;
        }

        void OnTapMash()
        {
            if (!CanAcceptInput()) return;
            if (CurrentMechanic != ChallengeMechanic.TapMash) { RecordInput(false); return; }
            RecordInput(true);
            AdvanceCurrentPhase();
        }

        void OnRhythmHold(float secondsHeld)
        {
            if (!CanAcceptInput()) return;
            if (CurrentMechanic != ChallengeMechanic.RhythmHold || secondsHeld < .5f)
            { RecordInput(false); return; }
            RecordInput(true);
            AdvanceCurrentPhase();
        }

        void OnAlternateTap(BossTapSide side)
        {
            if (!CanAcceptInput()) return;
            if (CurrentMechanic != ChallengeMechanic.AlternateTap || side != expectedSide)
            { RecordInput(false); return; }
            RecordInput(true);
            expectedSide = expectedSide == BossTapSide.Left ? BossTapSide.Right : BossTapSide.Left;
            AdvanceCurrentPhase();
        }

        bool CanAcceptInput() => running && Phase == MinigamePhase.Play && !terminalResolved;

        void RecordInput(bool successful)
        {
            totalInputs++;
            if (successful) successfulInputs++;
        }

        void AdvanceCurrentPhase()
        {
            var progress = sequence.CurrentProgress + 1f;
            var previousMechanic = sequence.Current.Mechanic;
            sequence.ReportProgress(progress);
            if (sequence.IsComplete || sequence.Current.Mechanic != previousMechanic)
            {
                completedPhases++;
                if (!sequence.IsComplete)
                    remainingPhaseSeconds = sequence.Current.Duration;
            }
        }

        void Resolve(bool pass)
        {
            if (!running || terminalResolved)
                return;

            terminalResolved = true;
            running = false;
            LastResult = ScoreUtil.Build(pass && ObjectiveComplete, Accuracy, Efficiency, Mastery);
            CompletionCount++;
            Finish(LastResult);
        }

        void ResetRuntimeState()
        {
            remainingSeconds = configuredDuration;
            remainingPhaseSeconds = sequence == null ? 0f : sequence.Current.Duration;
            successfulInputs = 0f;
            totalInputs = 0;
            completedPhases = 0;
            expectedSide = BossTapSide.Left;
            running = false;
            terminalResolved = false;
            LastResult = null;
            CompletionCount = 0;
        }
    }
}
