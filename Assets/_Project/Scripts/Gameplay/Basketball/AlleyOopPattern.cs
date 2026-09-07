using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class AlleyOopPattern
    {
        public AlleyOopPattern(Vector2 passVector, float launchForce, float curvature, float apexMin, float apexMax, float velocityThreshold)
        {
            if (passVector.sqrMagnitude <= Mathf.Epsilon) throw new ArgumentException("An authored pass vector is required.", nameof(passVector));
            if (apexMin > apexMax || velocityThreshold < 0f) throw new ArgumentOutOfRangeException(nameof(apexMin));
            PassVector = passVector; LaunchForce = launchForce; Curvature = curvature;
            ApexMin = apexMin; ApexMax = apexMax; VelocityThreshold = velocityThreshold;
        }

        public Vector2 PassVector { get; }
        public float LaunchForce { get; }
        public float Curvature { get; }
        public float ApexMin { get; }
        public float ApexMax { get; }
        public float VelocityThreshold { get; }
        public Vector2 LaunchVelocity => PassVector.normalized * LaunchForce;
        public bool IsApexWindow(float ballY, float velocityY) => ballY >= ApexMin && ballY <= ApexMax && Mathf.Abs(velocityY) <= VelocityThreshold;

        public bool TryLaunch(BallRig ball)
        {
            if (!ball) return false;
            ball.Launch(PassVector, LaunchForce, Curvature);
            return true;
        }

        // Sized so the apex window lasts a few hundred milliseconds instead of a single physics
        // step: at 0.02s steps gravity changes velocityY by ~0.196 per step, so the old 0.1
        // threshold made FinishJudge.Perfect unreachable in the build. The 0.4-unit height band
        // is unchanged, so aiming and timing stay independent axes.
        public static AlleyOopPattern AuthoredDefault(Vector2? passVector = null) =>
            new AlleyOopPattern(passVector ?? new Vector2(1f, .75f), 8f, 0f, 2.8f, 3.2f, 1.5f);
    }
}
