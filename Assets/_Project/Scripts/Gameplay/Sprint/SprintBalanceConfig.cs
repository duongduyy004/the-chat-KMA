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

        public SprintBalanceParameters(float initialStamina, float maxStamina, float correctImpulse,
            float wrongImpulseFactor, float speedCap, float correctTapCost, float wrongTapCost,
            float burstRateThreshold, float burstExtraCost, float activeDrainSpeedThreshold,
            float activeDrainPerSpeed, float restRegenPerSecond, float fatigueThreshold,
            float fatigueImpulseFactor, float fatigueSpeedCap, float dragPerSecond, float distanceScale)
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
        }

        public static SprintBalanceParameters Default => new SprintBalanceParameters(
            100f, 100f, 18f, .4f, 120f, .25f, 1.5f, 6f, .75f, 20f, .02f,
            6f, 30f, .75f, 90f, 15f, .08f);

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
            DistanceScale.Equals(other.DistanceScale);

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
        [SerializeField] float speedCap = 120f;
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

        public SprintBalanceParameters ToRuntime()
        {
            if (maxStamina <= 0f || float.IsNaN(maxStamina) || float.IsInfinity(maxStamina))
                return SprintBalanceParameters.Default;
            return new SprintBalanceParameters(initialStamina, maxStamina, correctImpulse,
                wrongImpulseFactor, speedCap, correctTapCost, wrongTapCost, burstRateThreshold,
                burstExtraCost, activeDrainSpeedThreshold, activeDrainPerSpeed, restRegenPerSecond,
                fatigueThreshold, fatigueImpulseFactor, fatigueSpeedCap, dragPerSecond, distanceScale);
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
            return config;
        }
    }
}
