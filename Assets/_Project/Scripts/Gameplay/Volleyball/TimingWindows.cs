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
        public const float Perfect = .1f;
        public const float Good = .24f;
        public const float Late = .4f;
        public const float ServePerfect = .12f;
        // A smash already took a timed jump to set up, so its hit is graded more generously.
        public const float SmashPerfect = .15f;
        public const float SmashGood = .30f;

        public static TimingGrade Grade(float offset, float perfectWindow = Perfect, float goodWindow = Good,
            float lateWindow = Late)
        {
            float distance = Mathf.Abs(offset);
            if (distance <= perfectWindow) return TimingGrade.Perfect;
            if (distance <= goodWindow) return TimingGrade.Good;
            if (distance <= lateWindow) return TimingGrade.Late;
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
