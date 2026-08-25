using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    [CreateAssetMenu(menuName = "KMA/Boss Sequence", fileName = "BossSequence")]
    public sealed class BossSequenceAsset : ScriptableObject
    {
        public const int CanonicalStepCount = 3;

        [Serializable]
        struct AuthoredStep
        {
            public ChallengeMechanic mechanic;
            public float duration;
            public float target;
        }

        [SerializeField] AuthoredStep[] steps =
        {
            new AuthoredStep { mechanic = ChallengeMechanic.TapMash, duration = 10f, target = 40f },
            new AuthoredStep { mechanic = ChallengeMechanic.RhythmHold, duration = 12f, target = 16f },
            new AuthoredStep { mechanic = ChallengeMechanic.AlternateTap, duration = 10f, target = 32f }
        };

        public ChallengeSequence CreateRuntimeSequence()
        {
            if (steps == null || steps.Length != CanonicalStepCount)
                throw new InvalidOperationException("Boss sequence must contain exactly three steps.");

            var runtimeSteps = new ChallengeStep[steps.Length];
            for (var index = 0; index < steps.Length; index++)
            {
                runtimeSteps[index] = new ChallengeStep(steps[index].mechanic,
                    steps[index].duration, steps[index].target);
            }

            ValidateCanonical(runtimeSteps);
            return new ChallengeSequence(runtimeSteps);
        }

        public static void ValidateCanonical(ChallengeSequence sequence)
        {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            var steps = new ChallengeStep[sequence.Count];
            for (var index = 0; index < sequence.Count; index++)
                steps[index] = sequence.GetStep(index);
            ValidateCanonical(steps);
        }

        static void ValidateCanonical(ChallengeStep[] authoredSteps)
        {
            if (authoredSteps.Length != CanonicalStepCount ||
                authoredSteps[0].Mechanic != ChallengeMechanic.TapMash ||
                authoredSteps[1].Mechanic != ChallengeMechanic.RhythmHold ||
                authoredSteps[2].Mechanic != ChallengeMechanic.AlternateTap)
            {
                throw new InvalidOperationException(
                    "The boss sequence must be TapMash, RhythmHold, AlternateTap in that order.");
            }
        }
    }
}
