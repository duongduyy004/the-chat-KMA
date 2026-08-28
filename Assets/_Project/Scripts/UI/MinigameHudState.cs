namespace KMA.Gameplay.UI
{
    public readonly struct MinigameHudState
    {
        public static readonly MinigameHudState Empty = new MinigameHudState(0f, 0f, string.Empty, 0f, string.Empty, string.Empty);

        public readonly float timeRemaining;
        public readonly float primary01;
        public readonly string primaryLabel;
        public readonly float secondary01;
        public readonly string secondaryLabel;
        public readonly string statusText;

        public MinigameHudState(
            float timeRemaining,
            float primary01,
            string primaryLabel,
            float secondary01,
            string secondaryLabel,
            string statusText)
        {
            this.timeRemaining = timeRemaining;
            this.primary01 = primary01;
            this.primaryLabel = primaryLabel;
            this.secondary01 = secondary01;
            this.secondaryLabel = secondaryLabel;
            this.statusText = statusText;
        }
    }

    public interface IMinigameHudStateSource
    {
        MinigameHudState ReadHudState();
    }
}
