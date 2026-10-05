namespace KMA.Gameplay
{
    public sealed class FootballMatchOptions
    {
        public int? MaxKicks { get; }
        public int RequiredGoals { get; }
        public bool KeeperEnabled { get; }

        public bool StopAtRequiredGoals { get; }

        public FootballMatchOptions(int? maxKicks, int requiredGoals, bool keeperEnabled,
            bool stopAtRequiredGoals = false)
        {
            MaxKicks = maxKicks;
            RequiredGoals = requiredGoals;
            KeeperEnabled = keeperEnabled;
            StopAtRequiredGoals = stopAtRequiredGoals;
        }
    }
}
