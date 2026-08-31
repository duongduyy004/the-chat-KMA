using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class SettingsScreen : ScreenBase
    {
        public event Action<Settings> SettingsChanged;

        public void Apply(float musicVol, float sfxVol, bool vibration, float rhythmOffsetMs)
        {
            SettingsChanged?.Invoke(new Settings
            {
                musicVol = Mathf.Clamp01(musicVol),
                sfxVol = Mathf.Clamp01(sfxVol),
                vibration = vibration,
                rhythmOffsetMs = rhythmOffsetMs
            });
        }
    }
}
