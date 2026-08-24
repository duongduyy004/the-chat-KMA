using UnityEngine;

namespace KMA.Gameplay
{
    public readonly struct TimingWindow
    {
        private readonly float maxError;

        public TimingWindow(float maxErrorMs)
        {
            maxError = maxErrorMs;
        }

        public float Evaluate(float errorMs) =>
            Mathf.Clamp01(1f - Mathf.Abs(errorMs) / maxError);
    }
}
