using System;

namespace KMA.Gameplay
{
    public static class RivalPaceRandomizer
    {
        // Rolls a sustained speed in [minSpeed, maxSpeed] and scales the opening speed with it,
        // so a fast starter still bursts off the line and a finisher still closes strong.
        public static RivalPaceProfile Roll(RivalPaceProfile authored, float minSpeed, float maxSpeed, Random random)
        {
            if (authored == null) return null;
            if (minSpeed > maxSpeed) (minSpeed, maxSpeed) = (maxSpeed, minSpeed);

            float openingRatio = authored.SustainedSpeed > 0f ? authored.OpeningSpeed / authored.SustainedSpeed : 1f;
            float sustained = minSpeed + (maxSpeed - minSpeed) * (float)random.NextDouble();
            return new RivalPaceProfile(authored.Name, sustained * openingRatio, sustained);
        }
    }
}
