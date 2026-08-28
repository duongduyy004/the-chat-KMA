using System;

namespace KMA.Gameplay
{
    public sealed class MinigameLifecycle
    {
        readonly float tutorialSeconds;
        readonly float countdownSeconds;
        float elapsed;

        public MinigamePhase Phase { get; private set; } = MinigamePhase.Tutorial;

        public event Action<MinigamePhase> PhaseChanged;

        public MinigameLifecycle(float tutorialSeconds, float countdownSeconds)
        {
            this.tutorialSeconds = tutorialSeconds;
            this.countdownSeconds = countdownSeconds;
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            if (Phase == MinigamePhase.Tutorial && elapsed >= tutorialSeconds)
            {
                var previousPhase = Phase;
                Phase = MinigamePhase.Countdown;
                elapsed = 0;
                if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
            }
            else if (Phase == MinigamePhase.Countdown && elapsed >= countdownSeconds)
            {
                var previousPhase = Phase;
                Phase = MinigamePhase.Play;
                elapsed = 0;
                if (Phase != previousPhase) PhaseChanged?.Invoke(Phase);
            }
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
    }
}
