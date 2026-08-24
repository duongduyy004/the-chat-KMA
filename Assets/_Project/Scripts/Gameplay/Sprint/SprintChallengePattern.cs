using System;
using UnityEngine;

namespace KMA.Gameplay
{
    [Serializable]
    public sealed class SprintChallengePattern
    {
        [SerializeField] float windCueDistance = 30f;
        [SerializeField] float windActivationDistance = 30f;
        [SerializeField] float windCueLeadSeconds = .8f;
        [SerializeField] float windWindowDuration = 1.2f;

        public float WindCueDistance => windCueDistance;
        public float WindActivationDistance => windActivationDistance;
        public float WindCueLeadSeconds => windCueLeadSeconds;
        public float WindWindowDuration => windWindowDuration;

        public static SprintChallengePattern AuthoredDefault() => new SprintChallengePattern();

        public void ConfigureForTest(float cueLeadSeconds)
        {
            windCueLeadSeconds = cueLeadSeconds;
        }
    }
}
