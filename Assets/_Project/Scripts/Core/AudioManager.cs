using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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
        [SerializeField] GameAudioLibrary library;
        AudioSource musicSource, uiSource;
        AudioListener fallbackListener;
        readonly List<AudioSource> voices = new List<AudioSource>();
        readonly Dictionary<GameSound, float> lastPlayed = new Dictionary<GameSound, float>();
        readonly Dictionary<GameSound, int> variants = new Dictionary<GameSound, int>();
        bool paused, appPaused, focusLost;
        float duckUntil;
        int nextVoice;
        GameManager settingsOwner;

        public static AudioManager Instance { get; private set; }

        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= EnsureForScene;
            SceneManager.sceneLoaded += EnsureForScene;
        }

        static void EnsureForScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Bootstrap" && scene.name != "Menu" && scene.name != "Map" &&
                scene.name != "MG_Sprint" && scene.name != "MG_Volleyball" && scene.name != "MG_Football" &&
                scene.name != "Punishment" && scene.name != "GameOver")
            {
                if (Instance) Instance.PlayMusicForScene(scene.name);
                return;
            }
            if (!Instance)
                new GameObject("GameAudio").AddComponent<AudioManager>();
            Instance.BindSettings();
            Instance.RefreshListener();
            Instance.PlayMusicForScene(scene.name);
        }

        void Awake()
        {
            if (!Application.isPlaying) return;
            if (Instance && Instance != this)
            {
                // Bootstrap shares its object with GameManager and other services.
                Destroy(this);
                return;
            }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            library = library ? library : Resources.Load<GameAudioLibrary>("GameAudioLibrary");
            if (library)
            {
                musicGroup = library.musicGroup;
                sfxGroup = library.sfxGroup;
            }
            musicSource = MakeSource(musicGroup, true);
            musicSource.volume = .22f;
            uiSource = MakeSource(sfxGroup, false);
            if (!sfxSource) sfxSource = MakeSource(sfxGroup, false);
            for (int i = 0; i < 8; i++) voices.Add(MakeSource(sfxGroup, false));
        }

        void OnEnable()
        {
            if (Instance != this) return;
            RefreshListener();
            GameAudio.Requested += PlayCue;
        }

        void RefreshListener()
        {
            bool sceneHasListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener != fallbackListener && listener.isActiveAndEnabled) sceneHasListener = true;
            if (!fallbackListener) fallbackListener = gameObject.AddComponent<AudioListener>();
            fallbackListener.enabled = !sceneHasListener;
        }

        void Start()
        {
            if (Instance != this) return;
            BindSettings();
            // AudioMixer values must also be applied after Awake.
            SetMusicVolume(MusicVolume);
            SetSfxVolume(SfxVolume);
            PlayMusicForScene(SceneManager.GetActiveScene().name);
        }

        void BindSettings()
        {
            var owner = GameManager.Instance;
            if (owner == settingsOwner) return;
            if (settingsOwner) settingsOwner.UnregisterSettingsService(this);
            settingsOwner = owner;
            if (settingsOwner) settingsOwner.RegisterSettingsService(this);
        }

        void OnDisable()
        {
            GameAudio.Requested -= PlayCue;
            if (fallbackListener) fallbackListener.enabled = false;
            if (musicSource) musicSource.Stop();
            StopEffects();
        }

        void OnDestroy()
        {
            if (fallbackListener) Destroy(fallbackListener);
            if (settingsOwner) settingsOwner.UnregisterSettingsService(this);
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            RefreshPause();
            if (musicSource)
                musicSource.volume = Mathf.MoveTowards(musicSource.volume,
                    Time.unscaledTime < duckUntil ? .07f : .22f, Time.unscaledDeltaTime * .4f);
        }

        void OnApplicationPause(bool value) { appPaused = value; RefreshPause(); }
        void OnApplicationFocus(bool value) { focusLost = !value && !Application.isBatchMode; RefreshPause(); }

        void RefreshPause()
        {
            bool value = appPaused || focusLost || Time.timeScale == 0f;
            if (value == paused) return;
            paused = value;
            if (paused)
            {
                if (musicSource) musicSource.Pause();
                StopEffects(false);
            }
            else if (musicSource) musicSource.UnPause();
        }

        AudioSource MakeSource(AudioMixerGroup group, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        void StopEffects(bool includeInterface = true)
        {
            if (sfxSource) sfxSource.Stop();
            if (includeInterface && uiSource) uiSource.Stop();
            foreach (var voice in voices) if (voice) voice.Stop();
        }

        public void PlayMusicForScene(string scene)
        {
            if (!musicSource) return;
            StopEffects();
            lastPlayed.Clear();
            duckUntil = 0f;
            AudioClip clip = library ? library.MusicFor(scene) : null;
            if (musicSource.clip == clip) return;
            musicSource.Stop();
            musicSource.clip = clip;
            if (clip)
            {
                musicSource.Play();
                if (paused || Time.timeScale == 0f) musicSource.Pause();
            }
        }

        public void PlayCue(GameSound sound)
        {
            if (!isActiveAndEnabled || !library || appPaused || focusLost) return;
            if (sound != GameSound.Click && (paused || Time.timeScale == 0f)) return;
            var cue = library.Find(sound);
            if (cue == null || cue.clips == null || cue.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(sound, out float last) && now - last < cue.minimumInterval) return;
            variants.TryGetValue(sound, out int index);
            var clip = cue.clips[index % cue.clips.Length];
            if (!clip) return;
            variants[sound] = index + 1;
            lastPlayed[sound] = now;
            AudioSource source = sound == GameSound.Click ? uiSource : voices[nextVoice++ % voices.Count];
            source.Stop();
            source.clip = clip;
            source.volume = cue.volume;
            source.Play();
            if (sound == GameSound.Win || sound == GameSound.Lose || sound == GameSound.Cheer)
                duckUntil = now + clip.length;
            if (sound == GameSound.Win) PlayCue(GameSound.Cheer);
        }

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
