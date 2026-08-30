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
        public MinigameResult BestResult => bestResult == null ? null : Copy(bestResult);

        MinigameResult bestResult;

        public void Accept(MinigameResult result)
        {
            if (result == null || !result.Pass)
            {
                throw new ArgumentException("Only a passing result can be accepted.", nameof(result));
            }

            bool hadResult = Passed;
            Passed = true;
            if (!hadResult || result.Score > BestScore)
            {
                bestResult = Copy(result);
                BestScore = result.Score;
                BestRank = result.Rank;
            }
        }

        public void RecordFailedVisit() => FailedVisits++;

        public static SubjectRecord FromData(SubjectRecordData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var record = new SubjectRecord
            {
                Passed = data.passed,
                BestScore = data.bestScore,
                BestRank = data.bestRank,
                FailedVisits = data.failedVisits
            };
            if (record.Passed)
            {
                record.bestResult = new MinigameResult(true, record.BestScore, record.BestRank);
            }

            return record;
        }

        static MinigameResult Copy(MinigameResult result) =>
            new MinigameResult(result.Pass, result.Score, result.Rank);
    }
}
