using System;
using UnityEngine;

namespace KMA.Gameplay.Core
{
    public sealed class HapticsService : MonoBehaviour, IGameSettingsService
    {
        [SerializeField] bool vibrationEnabled = true;

        public bool VibrationEnabled => vibrationEnabled;

        public void ApplySettings(Settings settings)
        {
            if (settings != null)
                vibrationEnabled = settings.vibration;
        }

        public void Light() => Vibrate(20L, 64);

        public void Medium() => Vibrate(45L, 128);

        public void Success() => Vibrate(70L, 192);

        public void Fail() => Vibrate(120L, 255);

        void Vibrate(long durationMilliseconds, int amplitude)
        {
            if (!vibrationEnabled)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>(
                    "getSystemService", "vibrator");
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    return;

                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    using AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMilliseconds, Mathf.Clamp(amplitude, 1, 255));
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMilliseconds);
                }
            }
            catch (Exception)
            {
                // Unsupported devices and unavailable platform services are intentional no-ops.
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                Handheld.Vibrate();
            }
            catch (Exception)
            {
                // Unsupported devices are intentional no-ops.
            }
#endif
        }
    }
}
