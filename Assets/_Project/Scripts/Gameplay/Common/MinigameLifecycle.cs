namespace KMA.Gameplay
{
    public sealed class MinigameLifecycle
    {
        readonly float tutorialSeconds;
        readonly float countdownSeconds;
        float elapsed;

        public MinigamePhase Phase { get; private set; } = MinigamePhase.Tutorial;

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
                Phase = MinigamePhase.Countdown;
                elapsed = 0;
            }
            else if (Phase == MinigamePhase.Countdown && elapsed >= countdownSeconds)
            {
                Phase = MinigamePhase.Play;
                elapsed = 0;
            }
        }

        public bool BeginResolve()
        {
            if (Phase != MinigamePhase.Play)
                return false;

            Phase = MinigamePhase.Resolve;
            return true;
        }
    }
}
