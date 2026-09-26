using System;

namespace KMA.Gameplay
{
    public readonly struct FootballTuning
    {
        public FootballTuning(float powerRiseSeconds,
            float keeperReactionSeconds, float keeperSpeed)
        {
            PowerRiseSeconds = PositiveFinite(powerRiseSeconds, nameof(powerRiseSeconds));
            KeeperReactionSeconds = PositiveFinite(keeperReactionSeconds, nameof(keeperReactionSeconds));
            KeeperSpeed = PositiveFinite(keeperSpeed, nameof(keeperSpeed));
        }

        public float PowerRiseSeconds { get; }
        public float KeeperReactionSeconds { get; }
        public float KeeperSpeed { get; }
        public bool IsValid => IsPositiveFinite(PowerRiseSeconds) &&
            IsPositiveFinite(KeeperReactionSeconds) && IsPositiveFinite(KeeperSpeed);

        public static FootballTuning For(FootballDifficulty difficulty) => difficulty switch
        {
            FootballDifficulty.Easy => new FootballTuning(2.4f, .38f, 1.9f),
            FootballDifficulty.Normal => new FootballTuning(2.042035f, .23f, 2.5f),
            FootballDifficulty.Hard => new FootballTuning(1.7f, .15f, 3.2f),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty,
                "Unknown Football difficulty.")
        };

        static float PositiveFinite(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and greater than zero.");
            return value;
        }

        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal void EnsureValid()
        {
            if (!IsValid)
                throw new ArgumentException("Football tuning must use finite, positive values.", nameof(FootballTuning));
        }

        static bool IsPositiveFinite(float value) => IsFinite(value) && value > 0f;
    }
}
