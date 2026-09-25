using System;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // A ball arc: linear over the ground from Start to Target, parabolic in height, landing
    // (height 0) exactly on Target at Duration. Solved once at launch so every landing point and
    // contact moment is known up front.
    public sealed class BallFlight
    {
        public const float Gravity = 12f;

        public BallFlight(Vector2 start, float startHeight, Vector2 target, float apexHeight, CourtSide hitter)
        {
            if (startHeight < 0f)
                throw new ArgumentOutOfRangeException(nameof(startHeight));
            if (apexHeight < startHeight || apexHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(apexHeight));

            Start = start;
            Target = target;
            StartHeight = startHeight;
            ApexHeight = apexHeight;
            Hitter = hitter;
            VerticalSpeed = Mathf.Sqrt(2f * Gravity * (apexHeight - startHeight));
            Duration = (VerticalSpeed + Mathf.Sqrt(VerticalSpeed * VerticalSpeed + 2f * Gravity * startHeight)) / Gravity;
        }

        public Vector2 Start { get; }
        public Vector2 Target { get; }
        public float StartHeight { get; }
        public float ApexHeight { get; }
        public float VerticalSpeed { get; }
        public float Duration { get; }
        public CourtSide Hitter { get; }
        public float ApexTime => VerticalSpeed / Gravity;
        public bool CrossesNet => (Start.x < 0f) != (Target.x < 0f);
        public float NetCrossTime => CrossesNet ? Duration * -Start.x / (Target.x - Start.x) : -1f;
        public bool ClearsNet => !CrossesNet || HeightAt(NetCrossTime) >= CourtSpace.NetHeight;

        public Vector2 GroundAt(float time) => Vector2.Lerp(Start, Target, Mathf.Clamp01(time / Duration));

        public float HeightAt(float time)
        {
            float t = Mathf.Clamp(time, 0f, Duration);
            return Mathf.Max(0f, StartHeight + VerticalSpeed * t - .5f * Gravity * t * t);
        }

        public bool IsDescending(float time) => time > ApexTime;

        public float TimeAtHeightDescending(float height)
        {
            if (height >= ApexHeight) return ApexTime;
            if (height <= 0f) return Duration;
            return (VerticalSpeed + Mathf.Sqrt(VerticalSpeed * VerticalSpeed + 2f * Gravity * (StartHeight - height))) / Gravity;
        }
    }
}
