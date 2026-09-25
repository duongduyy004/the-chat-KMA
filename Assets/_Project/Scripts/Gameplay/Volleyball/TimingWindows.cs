using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum TimingGrade
    {
        Miss,
        Late,
        Good,
        Perfect
    }

    // Late covers both early and late presses; the sign of the offset tells them apart.
    public static class TimingWindows
    {
        public const float Perfect = .08f;
        public const float Good = .18f;
        public const float Late = .30f;
        public const float ServePerfect = .12f;

        public static TimingGrade Grade(float offset, float perfectWindow = Perfect)
        {
            float distance = Mathf.Abs(offset);
            if (distance <= perfectWindow) return TimingGrade.Perfect;
            if (distance <= Good) return TimingGrade.Good;
            if (distance <= Late) return TimingGrade.Late;
            return TimingGrade.Miss;
        }

        public static float Quality(TimingGrade grade) => grade switch
        {
            TimingGrade.Perfect => 1f,
            TimingGrade.Good => .6f,
            TimingGrade.Late => .25f,
            _ => 0f
        };
    }
}
