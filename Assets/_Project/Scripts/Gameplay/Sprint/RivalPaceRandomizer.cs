using System;

namespace KMA.Gameplay
{
    public static class RivalPaceRandomizer
    {
        // How far a rival's pace swings above and below their sustained speed mid-race.
        public const float SurgeAmplitude = .2f;
        public const float MinSurgePeriod = 2.5f;
        public const float MaxSurgePeriod = 4.5f;
        // Each rival rolls inside the middle of their own band, so neighbouring bands never touch.
        const float BandMargin = .15f;

        // Rolls a sustained speed in [minSpeed, maxSpeed] and scales the opening speed with it,
        // so a fast starter still bursts off the line and a finisher still closes strong.
        public static RivalPaceProfile Roll(RivalPaceProfile authored, float minSpeed, float maxSpeed, Random random)
        {
            if (authored == null) return null;
            if (minSpeed > maxSpeed) (minSpeed, maxSpeed) = (maxSpeed, minSpeed);

            float openingRatio = authored.SustainedSpeed > 0f ? authored.OpeningSpeed / authored.SustainedSpeed : 1f;
            float sustained = minSpeed + (maxSpeed - minSpeed) * (float)random.NextDouble();
            float period = MinSurgePeriod + (MaxSurgePeriod - MinSurgePeriod) * (float)random.NextDouble();
            float phase = 2f * MathF.PI * (float)random.NextDouble();
            return new RivalPaceProfile(authored.Name, sustained * openingRatio, sustained,
                SurgeAmplitude, period, phase);
        }

        // Rolls the whole field at once: the speed range is split into one band per rival and the
        // bands are shuffled, so every race has a clearly fast, middle and slow rival.
        public static RivalPaceProfile[] RollField(RivalPaceProfile[] authored, float minSpeed, float maxSpeed,
            Random random)
        {
            if (authored == null || authored.Length == 0) return Array.Empty<RivalPaceProfile>();
            if (minSpeed > maxSpeed) (minSpeed, maxSpeed) = (maxSpeed, minSpeed);

            // Empty slots stay empty and take no band, so the real rivals still spread across the range.
            int count = 0;
            foreach (RivalPaceProfile profile in authored)
                if (profile != null) count++;
            var rolled = new RivalPaceProfile[authored.Length];
            if (count == 0) return rolled;

            var bands = new int[count];
            for (int i = 0; i < count; i++) bands[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (bands[i], bands[j]) = (bands[j], bands[i]);
            }

            float width = (maxSpeed - minSpeed) / count;
            for (int i = 0, band = 0; i < authored.Length; i++)
            {
                if (authored[i] == null) continue;
                float low = minSpeed + width * (bands[band] + BandMargin);
                float high = minSpeed + width * (bands[band] + 1 - BandMargin);
                rolled[i] = Roll(authored[i], low, high, random);
                band++;
            }
            return rolled;
        }
    }
}
