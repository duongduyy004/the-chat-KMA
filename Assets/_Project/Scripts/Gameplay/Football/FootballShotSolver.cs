using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public readonly struct FootballShot
    {
        internal FootballShot(float aimX, float power, float targetX, float flightSeconds, bool isShort)
        {
            AimX = aimX;
            Power = power;
            TargetX = targetX;
            FlightSeconds = flightSeconds;
            IsShort = isShort;
        }

        public float AimX { get; }
        public float Power { get; }
        public float TargetX { get; }
        public float FlightSeconds { get; }
        public bool IsShort { get; }
    }

    public static class FootballShotSolver
    {
        const float AimLimit = .9f;
        const float PowerAccuracyLimit = .85f;
        const float ShortShotLimit = .12f;
        const float BallRadius = .04f;
        const float KeeperReach = .18f;
        const float KeeperLimit = .82f;

        public static FootballShot Create(float aimX, float power, float signedNoise)
        {
            ValidateRange(aimX, -AimLimit, AimLimit, nameof(aimX));
            ValidateRange(power, 0f, 1f, nameof(power));
            ValidateRange(signedNoise, -1f, 1f, nameof(signedNoise));

            bool isShort = power < ShortShotLimit;
            float flightSeconds = isShort
                ? .9f
                : Mathf.Lerp(.90f, .40f, (power - ShortShotLimit) / (1f - ShortShotLimit));
            float targetX = aimX;
            if (power > PowerAccuracyLimit)
            {
                float errorRadius = .30f * (power - PowerAccuracyLimit) / (1f - PowerAccuracyLimit);
                targetX += signedNoise * errorRadius;
            }

            return new FootballShot(aimX, power, targetX, flightSeconds, isShort);
        }

        public static float KeeperX(FootballShot shot, FootballTuning tuning, float flightTime)
        {
            tuning.EnsureValid();
            if (!FootballTuning.IsFinite(flightTime) || flightTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(flightTime));

            float reactionTime = Mathf.Max(0f, flightTime - tuning.KeeperReactionSeconds);
            float target = Mathf.Clamp(shot.TargetX, -KeeperLimit, KeeperLimit);
            float distance = Mathf.Min(Mathf.Abs(target), tuning.KeeperSpeed * reactionTime);
            return Mathf.Sign(target) * distance;
        }

        public static FootballOutcome Resolve(FootballShot shot, FootballTuning tuning)
        {
            if (shot.IsShort)
                return FootballOutcome.Miss;

            if (Mathf.Abs(shot.TargetX) + BallRadius > 1f + 1e-6f)
                return FootballOutcome.Miss;

            float keeperX = KeeperX(shot, tuning, shot.FlightSeconds);
            if (Mathf.Abs(shot.TargetX - keeperX) <= KeeperReach + BallRadius)
                return FootballOutcome.Saved;

            return FootballOutcome.Goal;
        }

        static void ValidateRange(float value, float min, float max, string name)
        {
            if (!FootballTuning.IsFinite(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name, value,
                    $"Value must be finite and within [{min}, {max}].");
        }
    }
}
