using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    // Run summary shown on the Game Over screen, derived from the session that just ended.
    public readonly struct GameOverStats
    {
        public int SubjectsPassed { get; }
        public int SubjectsTotal { get; }
        public int FailedVisits { get; }
        public int TotalScore { get; }

        GameOverStats(int passed, int total, int failed, int score)
        {
            SubjectsPassed = passed;
            SubjectsTotal = total;
            FailedVisits = failed;
            TotalScore = score;
        }

        public static GameOverStats From(GameSession session)
        {
            int total = Enum.GetValues(typeof(SubjectId)).Length;
            if (session == null)
                return new GameOverStats(0, total, 0, 0);

            int passed = 0, failed = 0;
            float score = 0f;
            foreach (var record in session.Records.Values)
            {
                failed += record.FailedVisits;
                if (!record.Passed) continue;
                passed++;
                score += record.BestScore;
            }
            return new GameOverStats(passed, total, failed, Mathf.RoundToInt(score));
        }
    }
}
