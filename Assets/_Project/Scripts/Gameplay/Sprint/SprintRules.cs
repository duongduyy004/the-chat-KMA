using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum Side
    {
        Left,
        Right
    }

    public enum StaminaBand
    {
        Low,
        Mid,
        High
    }

    public readonly struct SprintSnapshot : System.IEquatable<SprintSnapshot>
    {
        public float Distance { get; }
        public float Speed { get; }
        public float Stamina { get; }
        public float Elapsed { get; }

        public SprintSnapshot(float distance, float speed, float stamina, float elapsed)
        {
            Distance = distance;
            Speed = speed;
            Stamina = stamina;
            Elapsed = elapsed;
        }

        public bool Equals(SprintSnapshot other) =>
            Distance.Equals(other.Distance) && Speed.Equals(other.Speed) &&
            Stamina.Equals(other.Stamina) && Elapsed.Equals(other.Elapsed);

        public override bool Equals(object obj) => obj is SprintSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Distance.GetHashCode();
                hash = hash * 397 ^ Speed.GetHashCode();
                hash = hash * 397 ^ Stamina.GetHashCode();
                return hash * 397 ^ Elapsed.GetHashCode();
            }
        }
    }

    public sealed class SprintRules
    {
        const float FullImpulse = 18f;
        const float SpeedCap = 120f;
        const float FinishDistance = 100f;
        public const float LowStaminaThreshold = 30f;
        public const float HighStaminaThreshold = 70f;

        readonly float timeLimit;
        readonly Side[] authoredSequence;
        readonly RivalPaceProfile[] rivalProfiles;
        readonly float[] rivalDistances;
        readonly SprintBalanceParameters? balance;
        int sequenceIndex;
        int valid;
        int total;
        int correctStreak;
        int currentRank = 1;
        float lastTapElapsed;
        bool hasTapped;
        float distance;
        float speed;
        float stamina;
        float elapsed;

        public SprintRules(float timeLimit = 14f, RivalPaceProfile[] rivalProfiles = null,
            Side[] authoredSequence = null, SprintBalanceParameters? balance = null)
        {
            this.timeLimit = timeLimit;
            this.balance = balance;
            stamina = balance.HasValue ? Mathf.Clamp(balance.Value.InitialStamina, 0f, balance.Value.MaxStamina) : 100f;
            this.authoredSequence = authoredSequence == null ? new[] { Side.Left, Side.Right } : (Side[])authoredSequence.Clone();
            if (this.authoredSequence.Length == 0)
                throw new System.ArgumentException("Sprint authored sequence must contain at least one side.", nameof(authoredSequence));

            this.rivalProfiles = rivalProfiles == null ? Array.Empty<RivalPaceProfile>() : (RivalPaceProfile[])rivalProfiles.Clone();
            rivalDistances = new float[this.rivalProfiles.Length];
        }

        public static SprintRules Default() => new SprintRules(14f);

        public static SprintRules ForTest(float distance, float elapsed, int rank, float stamina = 100f,
            RivalPaceProfile[] rivalProfiles = null, Side[] authoredSequence = null)
        {
            _ = rank;
            var value = new SprintRules(14f, rivalProfiles, authoredSequence);
            value.distance = distance;
            value.elapsed = elapsed;
            value.stamina = Mathf.Clamp(stamina, 0f, 100f);
            value.UpdateRank();
            return value;
        }

        public float Distance => distance;
        public float Speed => speed;
        public float Stamina => stamina;
        public float Elapsed => elapsed;
        public float TimeLimit => timeLimit;
        public int CorrectStreak => correctStreak;
        public float ValidTapRatio => total == 0 ? 0f : (float)valid / total;
        public int Rank => currentRank;
        public Side ExpectedSide => authoredSequence[sequenceIndex];
        public Side[] AuthoredSequence => (Side[])authoredSequence.Clone();
        public StaminaBand StaminaBand => ClassifyStamina(stamina);
        public RivalPaceProfile[] RivalProfiles => (RivalPaceProfile[])rivalProfiles.Clone();
        public float[] RivalDistances => (float[])rivalDistances.Clone();
        public int RivalCount => rivalDistances.Length;
        public float GetRivalDistance(int index) => index < 0 || index >= rivalDistances.Length ? 0f : rivalDistances[index];
        public SprintSnapshot Snapshot => new SprintSnapshot(distance, speed, stamina, elapsed);

        public static StaminaBand ClassifyStamina(float value) =>
            value < LowStaminaThreshold ? StaminaBand.Low :
            value < HighStaminaThreshold ? StaminaBand.Mid : StaminaBand.High;

        public void Tap(Side side)
        {
            bool correct = side == ExpectedSide;
            total++;
            if (correct)
            {
                valid++;
                correctStreak++;
                sequenceIndex = (sequenceIndex + 1) % authoredSequence.Length;
            }
            else
                correctStreak = 0;

            if (balance.HasValue)
            {
                SprintBalanceParameters tuning = balance.Value;
                stamina = Mathf.Clamp(stamina - (correct ? tuning.CorrectTapCost : tuning.WrongTapCost),
                    0f, tuning.MaxStamina);
                bool burst = hasTapped && elapsed > lastTapElapsed &&
                    1f / (elapsed - lastTapElapsed) > tuning.BurstRateThreshold;
                if (burst) stamina = Mathf.Max(0f, stamina - tuning.BurstExtraCost);
                lastTapElapsed = elapsed;
                hasTapped = true;
                float impulse = correct ? tuning.CorrectImpulse : tuning.CorrectImpulse * tuning.WrongImpulseFactor;
                bool fatigued = stamina <= tuning.FatigueThreshold;
                if (fatigued) impulse *= tuning.FatigueImpulseFactor;
                float cap = fatigued ? Mathf.Min(tuning.SpeedCap, tuning.FatigueSpeedCap) : tuning.SpeedCap;
                speed = Mathf.Min(cap, speed + impulse);
            }
            else
                speed = Mathf.Min(SpeedCap, speed + FullImpulse * (correct ? 1f : .4f));
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            elapsed += dt;
            float drag = balance.HasValue ? balance.Value.DragPerSecond : 15f;
            speed = Mathf.Max(0f, speed - drag * dt);
            float scale = balance.HasValue ? balance.Value.DistanceScale : .08f;
            distance += speed * dt * scale;
            if (balance.HasValue)
            {
                SprintBalanceParameters tuning = balance.Value;
                float staminaDelta = speed > tuning.ActiveDrainSpeedThreshold
                    ? -(speed - tuning.ActiveDrainSpeedThreshold) * tuning.ActiveDrainPerSpeed
                    : tuning.RestRegenPerSecond;
                stamina = Mathf.Clamp(stamina + staminaDelta * dt, 0f, tuning.MaxStamina);
                if (stamina <= tuning.FatigueThreshold)
                    speed = Mathf.Min(speed, tuning.FatigueSpeedCap);
            }
            else
                stamina = Mathf.Clamp(stamina + (speed > 20f ? -speed * .25f : 6f) * dt, 0f, 100f);

            for (int i = 0; i < rivalProfiles.Length; i++)
            {
                float rivalSpeed = elapsed <= 3f ? rivalProfiles[i].OpeningSpeed : rivalProfiles[i].SustainedSpeed;
                rivalDistances[i] += rivalSpeed * dt;
            }

            UpdateRank();
        }

        public MinigameResult BuildResult() => BuildResult(FinishDistance, timeLimit);

        public MinigameResult BuildResult(float goalDistance, float deadline)
        {
            bool pass = distance >= Mathf.Max(0f, goalDistance) && elapsed <= deadline;
            float accuracy = total == 0 ? 0f : 2f * valid / total;
            float maxStamina = balance.HasValue ? balance.Value.MaxStamina : 100f;
            float efficiency = Mathf.Clamp01(maxStamina <= 0f ? 0f : stamina / maxStamina);
            float mastery = deadline <= 0f ? 0f : Mathf.Clamp01((deadline - elapsed) / 3f);
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }

        void UpdateRank()
        {
            currentRank = 1;
            for (int i = 0; i < rivalDistances.Length; i++)
            {
                if (rivalDistances[i] > distance)
                    currentRank++;
            }
        }
    }
}
