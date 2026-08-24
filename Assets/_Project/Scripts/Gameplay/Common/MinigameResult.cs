namespace KMA.Gameplay
{
    public enum Rank
    {
        F,
        D,
        C,
        B,
        A,
        S
    }

    [System.Serializable]
    public sealed class MinigameResult
    {
        public bool Pass;
        public float Score;
        public Rank Rank;

        public MinigameResult(bool pass, float score, Rank rank)
        {
            Pass = pass;
            Score = score;
            Rank = rank;
        }
    }
}
