using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class CalibrateScreen : ScreenBase
    {
        public event Action<float> OffsetChanged;
        public event Action BackRequested;
        public float RhythmOffsetMs { get; private set; }

        public void SetOffset(float offsetMs)
        {
            RhythmOffsetMs = Mathf.Clamp(offsetMs, -500f, 500f);
            OffsetChanged?.Invoke(RhythmOffsetMs);
        }

        public void Configure(float offsetMs) => RhythmOffsetMs = Mathf.Clamp(offsetMs, -500f, 500f);
        public void AdjustOffset(float deltaMs) => SetOffset(RhythmOffsetMs + deltaMs);
        public void Back() => BackRequested?.Invoke();
    }
}
