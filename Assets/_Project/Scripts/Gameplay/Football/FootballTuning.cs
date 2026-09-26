using System;

namespace KMA.Gameplay
{
    public readonly struct FootballTuning
    {
        public FootballTuning(float aimTraverseSeconds, float powerRiseSeconds,
            float keeperReactionSeconds, float keeperSpeed)
        {
            AimTraverseSeconds = PositiveFinite(aimTraverseSeconds, nameof(aimTraverseSeconds));
            PowerRiseSeconds = PositiveFinite(powerRiseSeconds, nameof(powerRiseSeconds));
            KeeperReactionSeconds = PositiveFinite(keeperReactionSeconds, nameof(keeperReactionSeconds));
            KeeperSpeed = PositiveFinite(keeperSpeed, nameof(keeperSpeed));
        }

        public float AimTraverseSeconds { get; }
        public float PowerRiseSeconds { get; }
        public float KeeperReactionSeconds { get; }
        public float KeeperSpeed { get; }
        public bool IsValid => IsPositiveFinite(AimTraverseSeconds) && IsPositiveFinite(PowerRiseSeconds) &&
            IsPositiveFinite(KeeperReactionSeconds) && IsPositiveFinite(KeeperSpeed);

        public static FootballTuning For(FootballDifficulty difficulty) => difficulty switch
        {
            FootballDifficulty.Easy => new FootballTuning(2.4f, 1.4f, .40f, .75f),
            FootballDifficulty.Normal => new FootballTuning(1.7f, 1.4f, .27f, 1.20f),
            FootballDifficulty.Hard => new FootballTuning(1.1f, .9f, .15f, 1.70f),
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
