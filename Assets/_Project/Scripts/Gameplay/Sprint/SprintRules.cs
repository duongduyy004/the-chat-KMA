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
        int sequenceIndex;
        int valid;
        int total;
        int currentRank = 1;
        float distance;
        float speed;
        float stamina = 100f;
        float elapsed;

        public SprintRules(float timeLimit = 14f, RivalPaceProfile[] rivalProfiles = null, Side[] authoredSequence = null)
        {
            this.timeLimit = timeLimit;
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
        public float ValidTapRatio => total == 0 ? 0f : (float)valid / total;
        public int Rank => currentRank;
        public Side ExpectedSide => authoredSequence[sequenceIndex];
        public Side[] AuthoredSequence => (Side[])authoredSequence.Clone();
        public StaminaBand StaminaBand => ClassifyStamina(stamina);
        public RivalPaceProfile[] RivalProfiles => (RivalPaceProfile[])rivalProfiles.Clone();
        public float[] RivalDistances => (float[])rivalDistances.Clone();
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
                sequenceIndex = (sequenceIndex + 1) % authoredSequence.Length;
            }

            speed = Mathf.Min(SpeedCap, speed + FullImpulse * (correct ? 1f : .4f));
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            speed = Mathf.Max(0f, speed - 15f * dt);
            distance += speed * dt * .08f;
            stamina = Mathf.Clamp(stamina + (speed > 20f ? -speed * .25f : 6f) * dt, 0f, 100f);

            for (int i = 0; i < rivalProfiles.Length; i++)
            {
                float rivalSpeed = elapsed <= 3f ? rivalProfiles[i].OpeningSpeed : rivalProfiles[i].SustainedSpeed;
                rivalDistances[i] += rivalSpeed * dt;
            }

            UpdateRank();
        }

        public MinigameResult BuildResult()
        {
            bool pass = distance >= FinishDistance && elapsed <= timeLimit;
            float accuracy = total == 0 ? 0f : 2f * valid / total;
            float efficiency = Mathf.Clamp01(stamina / 100f);
            float mastery = Mathf.Clamp01((timeLimit - elapsed) / 3f);
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
