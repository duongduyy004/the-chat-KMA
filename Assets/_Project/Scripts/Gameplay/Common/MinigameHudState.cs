namespace KMA.Gameplay.UI
{
    public readonly struct MinigameHudState
    {
        public readonly string phase;
        public readonly float timeRemaining;
        public readonly float progress01;
        public readonly float stamina01;
        public readonly float score;
        public readonly string statusText;

        public static MinigameHudState Empty => new MinigameHudState(
            string.Empty,
            0f,
            0f,
            0f,
            0f,
            string.Empty);

        public MinigameHudState(
            string phase,
            float timeRemaining,
            float progress01,
            float stamina01,
            float score,
            string statusText)
        {
            this.phase = phase;
            this.timeRemaining = timeRemaining;
            this.progress01 = progress01;
            this.stamina01 = stamina01;
            this.score = score;
            this.statusText = statusText;
        }
    }

    public interface IMinigameHudStateSource
    {
        MinigameHudState ReadHudState();
    }
}
