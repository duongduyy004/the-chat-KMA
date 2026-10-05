using System;

namespace KMA.Gameplay.Chess
{
    public sealed class ThinkClock
    {
        public ThinkClock(float limitSeconds)
        {
            if (!(limitSeconds > 0f)) throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            Limit = limitSeconds;
        }

        public float Limit { get; }
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public float Remaining => Math.Max(0f, Limit - Elapsed);
        public bool Expired => Elapsed >= Limit;

        public void Start()
        {
            if (!Expired) Running = true;
        }

        public void Stop() => Running = false;

        public void Reset()
        {
            Elapsed = 0f;
            Running = false;
        }

        /// Returns true on the tick that runs the clock out.
        public bool Tick(float dt)
        {
            if (!Running || !(dt > 0f)) return false;
            Elapsed = Math.Min(Limit, Elapsed + dt);
            if (!Expired) return false;
            Running = false;
            return true;
        }
    }
}
