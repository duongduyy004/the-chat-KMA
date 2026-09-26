using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public readonly struct FootballShot
    {
        public FootballShot(float aimX, float power)
        {
            if (!FootballTuning.IsFinite(aimX) || Mathf.Abs(aimX) > 1f)
                throw new ArgumentOutOfRangeException(nameof(aimX));
            if (!FootballTuning.IsFinite(power) || power < 0f || power > 1f)
                throw new ArgumentOutOfRangeException(nameof(power));
            AimX = aimX;
            Power = power;
            float theta = Mathf.Atan(aimX * 4.3f / FootballShotSolver.GoalDistance);
            float alpha = 22f * Mathf.Deg2Rad;
            float speed = 6f + 20f * power;
            Velocity = new Vector3(speed * Mathf.Cos(alpha) * Mathf.Sin(theta),
                speed * Mathf.Sin(alpha), speed * Mathf.Cos(alpha) * Mathf.Cos(theta));
        }
        public float AimX { get; }
        public float Power { get; }
        public Vector3 Velocity { get; }
    }

    public static class FootballShotSolver
    {
        public const float StepSeconds = 1f / 240f;
        public const float BallRadius = .11f;
        public const float Gravity = 9.81f;
        public const float GoalDistance = 11f;
        public const float GoalHalfWidth = 3.66f;
        public const float GoalHeight = 2.44f;
        public const float PresentationSpeed = .8f;
        public static FootballShot Create(float aimX, float power) => new FootballShot(aimX, power);

        // Pixel-space projection shared by sprites, trajectory dots and keeper contact shapes.
        public static Vector3 Project(Vector3 point)
        {
            float depth = 5f / (5f + Mathf.Max(-1f, point.z));
            float vertical = 149f / GoalHeight * depth / (5f / 16f);
            return new Vector3(600f + point.x * (300f / GoalHalfWidth) * depth / (5f / 16f),
                211.090909f + 290.909091f * depth - point.y * vertical, vertical * BallRadius / 17f);
        }

        public static int Predict(FootballShot shot, FootballTuning tuning, Vector3[] points)
        {
            if (points == null || points.Length == 0) return 0;
            var simulation = new FootballFlightSimulation(shot, tuning, false);
            int count = 0;
            for (int i = 0; i < 2200 && count < points.Length - 1; i++)
            {
                if (i % 16 == 0) points[count++] = simulation.Position;
                if (simulation.Outcome.HasValue) break;
                simulation.Step();
            }
            points[count++] = simulation.Position;
            return count;
        }
    }
}
