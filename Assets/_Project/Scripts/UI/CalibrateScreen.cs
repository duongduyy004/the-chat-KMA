using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class CalibrateScreen : ScreenBase
    {
        public event Action<float> OffsetChanged;
        public float RhythmOffsetMs { get; private set; }

        public void SetOffset(float offsetMs)
        {
            RhythmOffsetMs = Mathf.Clamp(offsetMs, -500f, 500f);
            OffsetChanged?.Invoke(RhythmOffsetMs);
        }
    }
}
