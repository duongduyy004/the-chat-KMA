using System;
using UnityEngine;

namespace KMA.Gameplay.Boss
{
    public enum BossTapSide
    {
        Left,
        Right
    }

    public sealed class BossAlternateTapDetectorAdapter : MonoBehaviour
    {
        public event Action<BossTapSide> Tap;

        public void SubmitTap(BossTapSide side) => Tap?.Invoke(side);
    }
}
