namespace KMA.Gameplay
{
    public sealed class FootballMatchOptions
    {
        public int? MaxKicks { get; }
        public int RequiredGoals { get; }
        public bool KeeperEnabled { get; }

        public FootballMatchOptions(int? maxKicks, int requiredGoals, bool keeperEnabled)
        {
            MaxKicks = maxKicks;
            RequiredGoals = requiredGoals;
            KeeperEnabled = keeperEnabled;
        }
    }
}
