using UnityEngine;

namespace KMA.Gameplay
{
    public static class ScoreUtil
    {
        public static Rank ToRank(float score) => score >= 9 ? Rank.S : score >= 8 ? Rank.A :
            score >= 7 ? Rank.B : score >= 6 ? Rank.C : score >= 5 ? Rank.D : Rank.F;

        public static MinigameResult Build(bool pass, float accuracy, float efficiency, float mastery)
        {
            if (!pass)
            {
                return new MinigameResult(false, 0, Rank.F);
            }

            float raw = 6f + Mathf.Clamp(accuracy, 0, 2) +
                Mathf.Clamp01(efficiency) + Mathf.Clamp01(mastery);
            float rounded = Mathf.Round(Mathf.Clamp(raw, 0, 10) * 10f) / 10f;
            return new MinigameResult(true, rounded, ToRank(rounded));
        }
    }
}
