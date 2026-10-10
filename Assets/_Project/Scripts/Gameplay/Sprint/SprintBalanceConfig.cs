using System;
using UnityEngine;

namespace KMA.Gameplay
{
    [Serializable]
    public readonly struct SprintBalanceParameters : IEquatable<SprintBalanceParameters>
    {
        public float InitialStamina { get; }
        public float MaxStamina { get; }
        public float CorrectImpulse { get; }
        public float WrongImpulseFactor { get; }
        public float SpeedCap { get; }
        public float CorrectTapCost { get; }
        public float WrongTapCost { get; }
        public float BurstRateThreshold { get; }
        public float BurstExtraCost { get; }
        public float ActiveDrainSpeedThreshold { get; }
        public float ActiveDrainPerSpeed { get; }
        public float RestRegenPerSecond { get; }
        public float FatigueThreshold { get; }
        public float FatigueImpulseFactor { get; }
        public float FatigueSpeedCap { get; }
        public float DragPerSecond { get; }
        public float DistanceScale { get; }
        public int ComboBoostStartStreak { get; }
        public int ComboBoostFullStreak { get; }
        public float ComboBoostMax { get; }
        public float IdleGraceSeconds { get; }
        public float IdleBrakeRampSeconds { get; }
        public float IdleBrakePerSecond { get; }

        public SprintBalanceParameters(float initialStamina, float maxStamina, float correctImpulse,
            float wrongImpulseFactor, float speedCap, float correctTapCost, float wrongTapCost,
            float burstRateThreshold, float burstExtraCost, float activeDrainSpeedThreshold,
            float activeDrainPerSpeed, float restRegenPerSecond, float fatigueThreshold,
            float fatigueImpulseFactor, float fatigueSpeedCap, float dragPerSecond, float distanceScale,
            int comboBoostStartStreak, int comboBoostFullStreak, float comboBoostMax,
            float idleGraceSeconds, float idleBrakeRampSeconds, float idleBrakePerSecond)
        {
            InitialStamina = initialStamina;
            MaxStamina = maxStamina;
            CorrectImpulse = correctImpulse;
            WrongImpulseFactor = wrongImpulseFactor;
            SpeedCap = speedCap;
            CorrectTapCost = correctTapCost;
            WrongTapCost = wrongTapCost;
            BurstRateThreshold = burstRateThreshold;
            BurstExtraCost = burstExtraCost;
            ActiveDrainSpeedThreshold = activeDrainSpeedThreshold;
            ActiveDrainPerSpeed = activeDrainPerSpeed;
            RestRegenPerSecond = restRegenPerSecond;
            FatigueThreshold = fatigueThreshold;
            FatigueImpulseFactor = fatigueImpulseFactor;
            FatigueSpeedCap = fatigueSpeedCap;
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
            100f, 100f, 18f, .4f, 150f, .25f, 1.5f, 6f, .75f, 20f, .02f,
            6f, 30f, .75f, 90f, 15f, .08f, 10, 30, .25f, .35f, .25f, 240f);

        public bool Equals(SprintBalanceParameters other) =>
            InitialStamina.Equals(other.InitialStamina) && MaxStamina.Equals(other.MaxStamina) &&
            CorrectImpulse.Equals(other.CorrectImpulse) && WrongImpulseFactor.Equals(other.WrongImpulseFactor) &&
            SpeedCap.Equals(other.SpeedCap) && CorrectTapCost.Equals(other.CorrectTapCost) &&
            WrongTapCost.Equals(other.WrongTapCost) && BurstRateThreshold.Equals(other.BurstRateThreshold) &&
            BurstExtraCost.Equals(other.BurstExtraCost) &&
            ActiveDrainSpeedThreshold.Equals(other.ActiveDrainSpeedThreshold) &&
            ActiveDrainPerSpeed.Equals(other.ActiveDrainPerSpeed) &&
            RestRegenPerSecond.Equals(other.RestRegenPerSecond) &&
            FatigueThreshold.Equals(other.FatigueThreshold) &&
            FatigueImpulseFactor.Equals(other.FatigueImpulseFactor) &&
            FatigueSpeedCap.Equals(other.FatigueSpeedCap) && DragPerSecond.Equals(other.DragPerSecond) &&
            DistanceScale.Equals(other.DistanceScale) &&
            ComboBoostStartStreak == other.ComboBoostStartStreak &&
            ComboBoostFullStreak == other.ComboBoostFullStreak && ComboBoostMax.Equals(other.ComboBoostMax) &&
            IdleGraceSeconds.Equals(other.IdleGraceSeconds) &&
            IdleBrakeRampSeconds.Equals(other.IdleBrakeRampSeconds) &&
            IdleBrakePerSecond.Equals(other.IdleBrakePerSecond);

        public override bool Equals(object obj) => obj is SprintBalanceParameters other && Equals(other);
        public override int GetHashCode() => InitialStamina.GetHashCode() ^ MaxStamina.GetHashCode() ^
            CorrectImpulse.GetHashCode() ^ SpeedCap.GetHashCode() ^ DistanceScale.GetHashCode();
    }

    [CreateAssetMenu(menuName = "KMA/Journey/Sprint Balance", fileName = "SprintBalance")]
    public sealed class SprintBalanceConfig : ScriptableObject
    {
        [SerializeField] float initialStamina = 100f;
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float correctImpulse = 18f;
        [SerializeField] float wrongImpulseFactor = .4f;
        [SerializeField] float speedCap = 150f;
        [SerializeField] float correctTapCost = .25f;
        [SerializeField] float wrongTapCost = 1.5f;
        [SerializeField] float burstRateThreshold = 6f;
        [SerializeField] float burstExtraCost = .75f;
        [SerializeField] float activeDrainSpeedThreshold = 20f;
        [SerializeField] float activeDrainPerSpeed = .02f;
        [SerializeField] float restRegenPerSecond = 6f;
        [SerializeField] float fatigueThreshold = 30f;
        [SerializeField] float fatigueImpulseFactor = .75f;
        [SerializeField] float fatigueSpeedCap = 90f;
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
            if (maxStamina <= 0f || float.IsNaN(maxStamina) || float.IsInfinity(maxStamina))
                return SprintBalanceParameters.Default;
            return new SprintBalanceParameters(initialStamina, maxStamina, correctImpulse,
                wrongImpulseFactor, speedCap, correctTapCost, wrongTapCost, burstRateThreshold,
                burstExtraCost, activeDrainSpeedThreshold, activeDrainPerSpeed, restRegenPerSecond,
                fatigueThreshold, fatigueImpulseFactor, fatigueSpeedCap, dragPerSecond, distanceScale,
                comboBoostStartStreak, comboBoostFullStreak, comboBoostMax, idleGraceSeconds,
                idleBrakeRampSeconds, idleBrakePerSecond);
        }

        public static SprintBalanceConfig LoadDefault()
        {
            SprintBalanceConfig config = Resources.Load<SprintBalanceConfig>("Journey/SprintBalance");
            if (config != null) return config;
            config = CreateInstance<SprintBalanceConfig>();
            SprintBalanceParameters defaults = SprintBalanceParameters.Default;
            config.initialStamina = defaults.InitialStamina;
            config.maxStamina = defaults.MaxStamina;
            config.correctImpulse = defaults.CorrectImpulse;
            config.wrongImpulseFactor = defaults.WrongImpulseFactor;
            config.speedCap = defaults.SpeedCap;
            config.correctTapCost = defaults.CorrectTapCost;
            config.wrongTapCost = defaults.WrongTapCost;
            config.burstRateThreshold = defaults.BurstRateThreshold;
            config.burstExtraCost = defaults.BurstExtraCost;
            config.activeDrainSpeedThreshold = defaults.ActiveDrainSpeedThreshold;
            config.activeDrainPerSpeed = defaults.ActiveDrainPerSpeed;
            config.restRegenPerSecond = defaults.RestRegenPerSecond;
            config.fatigueThreshold = defaults.FatigueThreshold;
            config.fatigueImpulseFactor = defaults.FatigueImpulseFactor;
            config.fatigueSpeedCap = defaults.FatigueSpeedCap;
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
