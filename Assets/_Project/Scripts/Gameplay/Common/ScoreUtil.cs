using UnityEngine;

namespace KMA.Gameplay
{
    public static class ScoreUtil
    {
        public static Rank ToRank(float score) => score >= 9 ? Rank.S : score >= 8 ? Rank.A :
            score >= 7 ? Rank.B : score >= 6 ? Rank.C : score >= 5 ? Rank.D : Rank.F;

        public static int ToStars(Rank rank) => rank >= Rank.A ? 3 : rank >= Rank.C ? 2 : rank == Rank.D ? 1 : 0;

        public static MinigameResult Build(bool pass, float accuracy, float efficiency, float mastery)
        {
            if (!pass)
            {
                return new MinigameResult(false, 0, Rank.F);
            }

            float raw = 6f + SanitizeComponent(accuracy, 0f, 2f) +
                SanitizeComponent(efficiency, 0f, 1f) + SanitizeComponent(mastery, 0f, 1f);
            float rounded = Mathf.Round(Mathf.Clamp(raw, 0, 10) * 10f) / 10f;
            return new MinigameResult(true, rounded, ToRank(rounded));
        }

        private static float SanitizeComponent(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, min, max);
        }
    }
}
