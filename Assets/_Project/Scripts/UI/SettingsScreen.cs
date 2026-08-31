using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class SettingsScreen : ScreenBase
    {
        public event Action<Settings> SettingsChanged;
        public event Action CalibrateRequested;
        public event Action BackRequested;
        public Settings CurrentSettings { get; private set; } = Settings.CreateDefault();

        public void Configure(Settings value)
        {
            CurrentSettings = value == null ? Settings.CreateDefault() : Copy(value);
        }

        public void Apply(float musicVol, float sfxVol, bool vibration, float rhythmOffsetMs)
        {
            CurrentSettings = new Settings
            {
                musicVol = Mathf.Clamp01(musicVol),
                sfxVol = Mathf.Clamp01(sfxVol),
                vibration = vibration,
                rhythmOffsetMs = rhythmOffsetMs
            };
            SettingsChanged?.Invoke(Copy(CurrentSettings));
        }

        public void AdjustMusic(float delta) => Apply(CurrentSettings.musicVol + delta,
            CurrentSettings.sfxVol, CurrentSettings.vibration, CurrentSettings.rhythmOffsetMs);
        public void AdjustSfx(float delta) => Apply(CurrentSettings.musicVol,
            CurrentSettings.sfxVol + delta, CurrentSettings.vibration, CurrentSettings.rhythmOffsetMs);
        public void ToggleVibration() => Apply(CurrentSettings.musicVol, CurrentSettings.sfxVol,
            !CurrentSettings.vibration, CurrentSettings.rhythmOffsetMs);
        public void OpenCalibrate() => CalibrateRequested?.Invoke();
        public void Back() => BackRequested?.Invoke();

        static Settings Copy(Settings value) => new Settings
        {
            musicVol = value.musicVol,
            sfxVol = value.sfxVol,
            vibration = value.vibration,
            rhythmOffsetMs = value.rhythmOffsetMs
        };
    }
}
