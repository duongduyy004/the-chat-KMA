using System;

namespace KMA.Gameplay
{
    public sealed class MinigameLifecycle
    {
        readonly float tutorialSeconds;
        readonly float countdownSeconds;
        float elapsed;
        bool tutorialGateClosed;

        public MinigamePhase Phase { get; private set; } = MinigamePhase.Tutorial;

        public event Action<MinigamePhase> PhaseChanged;

        public MinigameLifecycle(float tutorialSeconds, float countdownSeconds)
        {
            this.tutorialSeconds = tutorialSeconds;
            this.countdownSeconds = countdownSeconds;
        }

        public void Tick(float dt)
        {
            if (Phase == MinigamePhase.Tutorial)
            {
                if (tutorialGateClosed)
                    return;

                elapsed += dt;
                if (elapsed >= tutorialSeconds)
                    BeginCountdown();
            }
            else if (Phase == MinigamePhase.Countdown)
            {
                elapsed += dt;
                if (elapsed >= countdownSeconds)
                {
                    var previousPhase = Phase;
                    Phase = MinigamePhase.Play;
                    elapsed = 0;
                    if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
                }
            }
        }

        public void SetTutorialGate(bool closed)
        {
            tutorialGateClosed = closed;
            if (!closed && Phase == MinigamePhase.Tutorial)
                BeginCountdown();
        }

        public bool BeginResolve()
        {
            if (Phase != MinigamePhase.Play)
                return false;

            var previousPhase = Phase;
            Phase = MinigamePhase.Resolve;
            if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
            return true;
        }

        void BeginCountdown()
        {
            if (Phase != MinigamePhase.Tutorial)
                return;

            var previousPhase = Phase;
            Phase = MinigamePhase.Countdown;
            elapsed = 0;
            if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
        }
    }
}
