using UnityEngine;
using UnityEngine.Audio;

namespace KMA.Gameplay.Core
{
    public sealed class AudioManager : MonoBehaviour, IGameSettingsService
    {
        const float MinimumDecibels = -80f;
        const string MusicVolumeParameter = "MusicVolume";
        const string SfxVolumeParameter = "SfxVolume";

        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField] AudioSource sfxSource;

        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        public void ApplySettings(Settings settings)
        {
            if (settings == null)
                return;

            SetMusicVolume(settings.musicVol);
            SetSfxVolume(settings.sfxVol);
        }

        public void SetMusicVolume(float linearVolume)
        {
            MusicVolume = Mathf.Clamp01(linearVolume);
            SetGroupVolume(musicGroup, MusicVolumeParameter, MusicVolume);
        }

        public void SetSfxVolume(float linearVolume)
        {
            SfxVolume = Mathf.Clamp01(linearVolume);
            SetGroupVolume(sfxGroup, SfxVolumeParameter, SfxVolume);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxGroup == null || sfxSource == null)
                return;

            if (sfxSource.outputAudioMixerGroup != sfxGroup)
                sfxSource.outputAudioMixerGroup = sfxGroup;
            sfxSource.PlayOneShot(clip);
        }

        static void SetGroupVolume(AudioMixerGroup group, string parameterName, float linearVolume)
        {
            if (group == null || group.audioMixer == null)
                return;

            group.audioMixer.SetFloat(parameterName, LinearToDecibels(linearVolume));
        }

        static float LinearToDecibels(float linearVolume)
        {
            float clamped = Mathf.Clamp01(linearVolume);
            return clamped <= 0f ? MinimumDecibels : Mathf.Log10(clamped) * 20f;
        }
    }
}
