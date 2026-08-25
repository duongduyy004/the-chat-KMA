using System;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    public sealed class BossRhythmHoldDetectorAdapter : MonoBehaviour
    {
        public event Action<float> HoldCompleted;

        public void SubmitHold(float secondsHeld) => HoldCompleted?.Invoke(secondsHeld);
    }
}
