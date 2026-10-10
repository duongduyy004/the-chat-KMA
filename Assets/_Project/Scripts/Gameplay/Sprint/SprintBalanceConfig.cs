using System;
using UnityEngine;

namespace KMA.Gameplay
{
    [Serializable]
    public readonly struct SprintBalanceParameters : IEquatable<SprintBalanceParameters>
    {
        public float CorrectImpulse { get; }
        public float WrongImpulseFactor { get; }
        public float SpeedCap { get; }
        public float DragPerSecond { get; }
        public float DistanceScale { get; }
        public int ComboBoostStartStreak { get; }
        public int ComboBoostFullStreak { get; }
        public float ComboBoostMax { get; }
        public float IdleGraceSeconds { get; }
        public float IdleBrakeRampSeconds { get; }
        public float IdleBrakePerSecond { get; }

        public SprintBalanceParameters(float correctImpulse, float wrongImpulseFactor, float speedCap,
            float dragPerSecond, float distanceScale, int comboBoostStartStreak, int comboBoostFullStreak,
            float comboBoostMax, float idleGraceSeconds, float idleBrakeRampSeconds, float idleBrakePerSecond)
        {
            CorrectImpulse = correctImpulse;
            WrongImpulseFactor = wrongImpulseFactor;
            SpeedCap = speedCap;
            DragPerSecond = dragPerSecond;
            DistanceScale = distanceScale;
            ComboBoostStartStreak = comboBoostStartStreak;
            ComboBoostFullStreak = comboBoostFullStreak;
            ComboBoostMax = comboBoostMax;
            IdleGraceSeconds = idleGraceSeconds;
            IdleBrakeRampSeconds = idleBrakeRampSeconds;
            IdleBrakePerSecond = idleBrakePerSecond;
        }

        public static SprintBalanceParameters Default => new SprintBalanceParameters(
            18f, .4f, 150f, 15f, .08f, 10, 30, .25f, .35f, .25f, 240f);

        public bool Equals(SprintBalanceParameters other) =>
            CorrectImpulse.Equals(other.CorrectImpulse) && WrongImpulseFactor.Equals(other.WrongImpulseFactor) &&
            SpeedCap.Equals(other.SpeedCap) && DragPerSecond.Equals(other.DragPerSecond) &&
            DistanceScale.Equals(other.DistanceScale) &&
            ComboBoostStartStreak == other.ComboBoostStartStreak &&
            ComboBoostFullStreak == other.ComboBoostFullStreak && ComboBoostMax.Equals(other.ComboBoostMax) &&
            IdleGraceSeconds.Equals(other.IdleGraceSeconds) &&
            IdleBrakeRampSeconds.Equals(other.IdleBrakeRampSeconds) &&
            IdleBrakePerSecond.Equals(other.IdleBrakePerSecond);

        public override bool Equals(object obj) => obj is SprintBalanceParameters other && Equals(other);
        public override int GetHashCode() => CorrectImpulse.GetHashCode() ^ SpeedCap.GetHashCode() ^
            DragPerSecond.GetHashCode() ^ DistanceScale.GetHashCode();
    }

    [CreateAssetMenu(menuName = "KMA/Journey/Sprint Balance", fileName = "SprintBalance")]
    public sealed class SprintBalanceConfig : ScriptableObject
    {
        [SerializeField] float correctImpulse = 18f;
        [SerializeField] float wrongImpulseFactor = .4f;
        [SerializeField] float speedCap = 150f;
        [SerializeField] float dragPerSecond = 15f;
        [SerializeField] float distanceScale = .08f;
        [Tooltip("Correct-tap streak where the combo speed boost starts.")]
        [SerializeField, Min(0)] int comboBoostStartStreak = 10;
        [Tooltip("Correct-tap streak where the combo speed boost reaches its maximum.")]
        [SerializeField, Min(0)] int comboBoostFullStreak = 30;
        [Tooltip("Maximum extra impulse and speed cap from a long combo (.25 = +25%).")]
        [SerializeField, Min(0f)] float comboBoostMax = .25f;
        [Tooltip("Seconds without a tap before the runner starts braking hard.")]
        [SerializeField, Min(0f)] float idleGraceSeconds = .35f;
        [Tooltip("Seconds over which the hard brake eases in, so the stop keeps some inertia.")]
        [SerializeField, Min(0f)] float idleBrakeRampSeconds = .25f;
        [Tooltip("Extra deceleration once the runner has stopped tapping.")]
        [SerializeField, Min(0f)] float idleBrakePerSecond = 240f;

        public SprintBalanceParameters ToRuntime()
        {
            if (speedCap <= 0f || float.IsNaN(speedCap) || float.IsInfinity(speedCap))
                return SprintBalanceParameters.Default;
            return new SprintBalanceParameters(correctImpulse, wrongImpulseFactor, speedCap, dragPerSecond,
                distanceScale, comboBoostStartStreak, comboBoostFullStreak, comboBoostMax, idleGraceSeconds,
                idleBrakeRampSeconds, idleBrakePerSecond);
        }

        public static SprintBalanceConfig LoadDefault()
        {
            SprintBalanceConfig config = Resources.Load<SprintBalanceConfig>("Journey/SprintBalance");
            if (config != null) return config;
            config = CreateInstance<SprintBalanceConfig>();
            SprintBalanceParameters defaults = SprintBalanceParameters.Default;
            config.correctImpulse = defaults.CorrectImpulse;
            config.wrongImpulseFactor = defaults.WrongImpulseFactor;
            config.speedCap = defaults.SpeedCap;
            config.dragPerSecond = defaults.DragPerSecond;
            config.distanceScale = defaults.DistanceScale;
            config.comboBoostStartStreak = defaults.ComboBoostStartStreak;
            config.comboBoostFullStreak = defaults.ComboBoostFullStreak;
            config.comboBoostMax = defaults.ComboBoostMax;
            config.idleGraceSeconds = defaults.IdleGraceSeconds;
            config.idleBrakeRampSeconds = defaults.IdleBrakeRampSeconds;
            config.idleBrakePerSecond = defaults.IdleBrakePerSecond;
            return config;
        }
    }
}
