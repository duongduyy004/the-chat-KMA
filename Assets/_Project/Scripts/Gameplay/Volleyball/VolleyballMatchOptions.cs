namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballMatchOptions
    {
        public int PointsToWin { get; }
        public float TimeLimit { get; }
        public bool OpponentAlwaysServes { get; }
        public bool RequirePointsToWin { get; }
        /// Ends the match once the opponent reaches this many points, even when PointsToWin is 0 (0 = off).
        public int OpponentPointLimit { get; }

        public VolleyballMatchOptions(int pointsToWin = 5, float timeLimit = 120f,
            bool opponentAlwaysServes = false, bool requirePointsToWin = false, int opponentPointLimit = 0)
        {
            PointsToWin = pointsToWin;
            TimeLimit = timeLimit;
            OpponentAlwaysServes = opponentAlwaysServes;
            RequirePointsToWin = requirePointsToWin;
            OpponentPointLimit = opponentPointLimit;
        }
    }
}
