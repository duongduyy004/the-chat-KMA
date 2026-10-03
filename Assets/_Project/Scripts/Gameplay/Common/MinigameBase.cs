using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay
{
    public abstract class MinigameBase : MonoBehaviour, IMinigameHudStateSource
    {
        public event System.Action<MinigameResult> Completed;
        public event System.Action<MinigamePhase> PhaseChanged;
        [SerializeField] float tutorialSeconds = 2f;
        [SerializeField] float countdownSeconds = 3f;
        MinigameLifecycle lifecycle;
        float nextCountdownSound;

        protected MinigameLifecycle Lifecycle
        {
            get => lifecycle;
            set
            {
                if (lifecycle != null)
                    lifecycle.PhaseChanged -= RelayPhaseChanged;
                lifecycle = value;
                if (lifecycle != null)
                    lifecycle.PhaseChanged += RelayPhaseChanged;
            }
        }

        public MinigamePhase PresentationPhase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;

        /// PhaseOverlay shows the shared tutorial card and holds the start gate while it is open.
        public virtual bool UsesSharedTutorial => true;

        /// PhaseOverlay shows the shared 3-2-1 countdown.
        public virtual bool UsesSharedCountdown => true;

        /// The minigame opens its own start gate, so PhaseOverlay must leave the gate alone.
        public virtual bool OwnsStartGate => false;

        /// The scene configurator paints its own camera background, so the shared assembler keeps it.
        public virtual bool OwnsCameraBackground => false;

        public void SetTutorialGate(bool closed) => Lifecycle?.SetTutorialGate(closed);

        protected virtual void Awake() => Lifecycle = new MinigameLifecycle(tutorialSeconds, countdownSeconds);

        protected virtual void Update()
        {
            Lifecycle.Tick(Time.deltaTime);
            if (Lifecycle.Phase == MinigamePhase.Countdown && Time.deltaTime > 0f)
            {
                nextCountdownSound -= Time.deltaTime;
                if (nextCountdownSound <= 0f)
                {
                    GameAudio.Play(GameSound.Countdown);
                    nextCountdownSound = 1f;
                }
            }
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(Time.deltaTime);
        }

        public MinigameHudState ReadHudState() => BuildHudState();
        protected virtual MinigameHudState BuildHudState() => MinigameHudState.Empty;
        protected abstract void TickPlay(float dt);

        void RelayPhaseChanged(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Countdown) nextCountdownSound = 0f;
            if (phase == MinigamePhase.Play) GameAudio.Play(GameSound.Whistle);
            PhaseChanged?.Invoke(phase);
        }

        protected void Finish(MinigameResult result)
        {
            if (TryBeginResolve(result.Pass))
            {
                GameAudio.Play(result.Pass ? GameSound.Win : GameSound.Lose);
                Completed?.Invoke(result);
            }
        }

        protected virtual bool TryBeginResolve(bool passed) => Lifecycle.BeginResolve();
    }
}
