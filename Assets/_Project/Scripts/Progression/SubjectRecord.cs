using System;

namespace KMA.Gameplay
{
    [Serializable]
    public sealed class SubjectRecord
    {
        public bool Passed { get; private set; }
        public float BestScore { get; private set; }
        public Rank BestRank { get; private set; }
        public int FailedVisits { get; private set; }

        public void Accept(MinigameResult result)
        {
            bool hadResult = Passed;
            Passed = true;
            if (!hadResult || result.Score > BestScore)
            {
                BestScore = result.Score;
                BestRank = result.Rank;
            }
        }

        public void RecordFailedVisit() => FailedVisits++;
    }
}
