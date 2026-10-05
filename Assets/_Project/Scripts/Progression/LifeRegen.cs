using System;

namespace KMA.Gameplay
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// One life every Interval of real time while below max. The mark is the UTC tick at which
    /// the next life arrives; 0 means the clock is not running.
    public static class LifeRegen
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public static int Advance(ref int lives, ref long nextLifeAtUtcTicks, DateTime utcNow, int maxLives)
        {
            lives = Math.Max(0, Math.Min(maxLives, lives));
            if (lives >= maxLives)
            {
                nextLifeAtUtcTicks = 0;
                return 0;
            }

            long now = utcNow.Ticks;
            // A mark further away than one interval means the device clock went backwards.
            if (nextLifeAtUtcTicks <= 0 || now < nextLifeAtUtcTicks - Interval.Ticks)
            {
                nextLifeAtUtcTicks = now + Interval.Ticks;
                return 0;
            }

            int gained = 0;
            while (lives < maxLives && now >= nextLifeAtUtcTicks)
            {
                lives++;
                gained++;
                nextLifeAtUtcTicks += Interval.Ticks;
            }
            if (lives >= maxLives)
                nextLifeAtUtcTicks = 0;
            return gained;
        }

        public static TimeSpan? Remaining(int lives, long nextLifeAtUtcTicks, DateTime utcNow, int maxLives) =>
            lives >= maxLives || nextLifeAtUtcTicks <= 0
                ? (TimeSpan?)null
                : TimeSpan.FromTicks(Math.Max(0L, nextLifeAtUtcTicks - utcNow.Ticks));
    }
}
