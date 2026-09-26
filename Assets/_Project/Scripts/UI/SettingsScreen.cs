using System;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class SettingsScreen : ScreenBase
    {
        Slider musicSlider;
        Slider sfxSlider;
        Toggle vibrationToggle;
        Text musicValue;
        Text sfxValue;
        Text vibrationValue;

        public event Action<Settings> SettingsChanged;
        public event Action CalibrateRequested;
        public event Action BackRequested;
        public Settings CurrentSettings { get; private set; } = Settings.CreateDefault();

        public void Configure(Settings value)
        {
            CurrentSettings = value == null ? Settings.CreateDefault() : Copy(value);
            RefreshControls();
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
            RefreshControls();
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

        internal void BindControls(Slider music, Slider sfx, Toggle vibration,
            Text musicLabel, Text sfxLabel, Text vibrationLabel)
        {
            musicSlider = music;
            sfxSlider = sfx;
            vibrationToggle = vibration;
            musicValue = musicLabel;
            sfxValue = sfxLabel;
            vibrationValue = vibrationLabel;
            music.onValueChanged.AddListener(value => Apply(value, CurrentSettings.sfxVol,
                CurrentSettings.vibration, CurrentSettings.rhythmOffsetMs));
            sfx.onValueChanged.AddListener(value => Apply(CurrentSettings.musicVol, value,
                CurrentSettings.vibration, CurrentSettings.rhythmOffsetMs));
            vibration.onValueChanged.AddListener(value => Apply(CurrentSettings.musicVol,
                CurrentSettings.sfxVol, value, CurrentSettings.rhythmOffsetMs));
            RefreshControls();
        }

        void RefreshControls()
        {
            if (musicSlider == null)
                return;
            musicSlider.SetValueWithoutNotify(CurrentSettings.musicVol);
            sfxSlider.SetValueWithoutNotify(CurrentSettings.sfxVol);
            vibrationToggle.SetIsOnWithoutNotify(CurrentSettings.vibration);
            musicValue.text = $"{Mathf.RoundToInt(CurrentSettings.musicVol * 100f)}%";
            sfxValue.text = $"{Mathf.RoundToInt(CurrentSettings.sfxVol * 100f)}%";
            vibrationValue.text = CurrentSettings.vibration ? "BẬT" : "TẮT";
        }

        static Settings Copy(Settings value) => new Settings
        {
            musicVol = value.musicVol,
            sfxVol = value.sfxVol,
            vibration = value.vibration,
            rhythmOffsetMs = value.rhythmOffsetMs
        };
    }
}
