using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    [CreateAssetMenu(menuName = "KMA/Boss Sequence", fileName = "BossSequence")]
    public sealed class BossSequenceAsset : ScriptableObject
    {
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
            if (steps == null || steps.Length != 3)
                throw new InvalidOperationException("Boss sequence must contain exactly three steps.");

            var runtimeSteps = new ChallengeStep[steps.Length];
            for (var index = 0; index < steps.Length; index++)
            {
                runtimeSteps[index] = new ChallengeStep(steps[index].mechanic,
                    steps[index].duration, steps[index].target);
            }

            return new ChallengeSequence(runtimeSteps);
        }
    }
}
