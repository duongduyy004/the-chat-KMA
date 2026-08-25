using System;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    public sealed class BossTapMashDetectorAdapter : MonoBehaviour
    {
        public event Action Tap;

        public void SubmitTap() => Tap?.Invoke();
    }
}
