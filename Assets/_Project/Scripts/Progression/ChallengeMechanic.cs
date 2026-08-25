using System;

namespace KMA.Gameplay
{
    public enum ChallengeMechanic
    {
        TapMash,
        RhythmHold,
        AlternateTap
    }

    public readonly struct ChallengeStep
    {
        public ChallengeStep(ChallengeMechanic mechanic, float duration, float target)
        {
            if (!Enum.IsDefined(typeof(ChallengeMechanic), mechanic))
                throw new ArgumentOutOfRangeException(nameof(mechanic));
            if (duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            if (target <= 0f)
                throw new ArgumentOutOfRangeException(nameof(target));

            Mechanic = mechanic;
            Duration = duration;
            Target = target;
        }

        public ChallengeMechanic Mechanic { get; }
        public float Duration { get; }
        public float Target { get; }
    }
}
