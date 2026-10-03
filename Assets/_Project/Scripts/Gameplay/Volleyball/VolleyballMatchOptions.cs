namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballMatchOptions
    {
        public int PointsToWin { get; }
        public float TimeLimit { get; }
        public bool OpponentAlwaysServes { get; }
        public bool RequirePointsToWin { get; }

        public VolleyballMatchOptions(int pointsToWin = 5, float timeLimit = 120f,
            bool opponentAlwaysServes = false, bool requirePointsToWin = false)
        {
            PointsToWin = pointsToWin;
            TimeLimit = timeLimit;
            OpponentAlwaysServes = opponentAlwaysServes;
            RequirePointsToWin = requirePointsToWin;
        }
    }
}
