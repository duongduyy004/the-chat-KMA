using System;

namespace KMA.Gameplay
{
    public sealed class RivalPaceProfile
    {
        public const float OpeningSeconds = 3f;
        // A rival counts as surging once their pace runs this far above their sustained speed.
        const float SurgingRatio = 1.06f;

        public string Name { get; }
        public float OpeningSpeed { get; }
        public float SustainedSpeed { get; }
        public float SurgeAmplitude { get; }
        public float SurgePeriod { get; }
        public float SurgePhase { get; }

        public RivalPaceProfile(string name, float openingSpeed, float sustainedSpeed,
            float surgeAmplitude = 0f, float surgePeriod = 0f, float surgePhase = 0f)
        {
            Name = name;
            OpeningSpeed = openingSpeed;
            SustainedSpeed = sustainedSpeed;
            SurgeAmplitude = surgeAmplitude;
            SurgePeriod = surgePeriod;
            SurgePhase = surgePhase;
        }

        // After the opening burst the pace swings around the sustained speed, so rivals visibly
        // surge, fade and swap places instead of gliding at one constant speed.
        public float SpeedAt(float elapsed)
        {
            if (elapsed <= OpeningSeconds) return OpeningSpeed;
            if (SurgeAmplitude <= 0f || SurgePeriod <= 0f) return SustainedSpeed;
            float cycle = 2f * MathF.PI * (elapsed - OpeningSeconds) / SurgePeriod + SurgePhase;
            return SustainedSpeed * (1f + SurgeAmplitude * MathF.Sin(cycle));
        }

        public bool IsSurgingAt(float elapsed) =>
            elapsed > OpeningSeconds && SpeedAt(elapsed) > SustainedSpeed * SurgingRatio;
    }
}
