using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum Side
    {
        Left,
        Right
    }

    public readonly record struct SprintSnapshot(float Distance, float Speed, float Stamina, float Elapsed);

    public sealed class SprintRules
    {
        const float FullImpulse = 18f;
        const float SpeedCap = 120f;
        const float FinishDistance = 100f;

        readonly float timeLimit;
        readonly RivalPaceProfile[] rivalProfiles;
        Side? expected;
        int valid;
        int total;
        int currentRank = 1;
        float distance;
        float speed;
        float stamina = 100f;
        float elapsed;

        public SprintRules(float timeLimit = 14f, RivalPaceProfile[] rivalProfiles = null)
        {
            this.timeLimit = timeLimit;
            this.rivalProfiles = rivalProfiles == null ? Array.Empty<RivalPaceProfile>() : (RivalPaceProfile[])rivalProfiles.Clone();
        }

        public static SprintRules Default() => new SprintRules(14f);

        public static SprintRules ForTest(float distance, float elapsed, int rank)
        {
            var value = new SprintRules(14f);
            value.distance = distance;
            value.elapsed = elapsed;
            value.currentRank = rank;
            return value;
        }

        public float Distance => distance;
        public float Speed => speed;
        public float Stamina => stamina;
        public float Elapsed => elapsed;
        public float ValidTapRatio => total == 0 ? 0f : (float)valid / total;
        public int Rank => currentRank;
        public RivalPaceProfile[] RivalProfiles => (RivalPaceProfile[])rivalProfiles.Clone();
        public SprintSnapshot Snapshot => new SprintSnapshot(distance, speed, stamina, elapsed);

        public void Tap(Side side)
        {
            bool correct = expected == null || side == expected.Value;
            total++;
            if (correct)
                valid++;

            speed = Mathf.Min(SpeedCap, speed + FullImpulse * (correct ? 1f : .4f));
            expected = side == Side.Left ? Side.Right : Side.Left;
        }

        public void Tick(float dt)
        {
            elapsed += dt;
            speed = Mathf.Max(0f, speed - 15f * dt);
            distance += speed * dt * .08f;
            stamina = Mathf.Clamp(stamina + (speed > 20f ? -speed * .25f : 6f) * dt, 0f, 100f);
        }

        public MinigameResult BuildResult()
        {
            bool pass = distance >= FinishDistance && elapsed <= timeLimit && stamina > 0f;
            float accuracy = total == 0 ? 0f : 2f * valid / total;
            float efficiency = Mathf.Clamp01(stamina / 100f);
            float mastery = Mathf.Clamp01((timeLimit - elapsed) / 3f);
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }
    }
}
