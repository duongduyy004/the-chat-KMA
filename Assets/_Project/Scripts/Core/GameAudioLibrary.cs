using System;
using UnityEngine;
using UnityEngine.Audio;

namespace KMA.Gameplay.Core
{
    [CreateAssetMenu(menuName = "KMA/Audio Library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Cue
        {
            public GameSound sound;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = .65f;
            [Min(0f)] public float minimumInterval = .06f;
        }

        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioClip menu, sprint, volleyball, football;
        public Cue[] cues = Array.Empty<Cue>();

        public AudioClip MusicFor(string scene) => scene switch
        {
            "Menu" or "Map" or "Punishment" or "GameOver" => menu,
            "MG_Sprint" => sprint,
            "MG_Volleyball" => volleyball,
            "MG_Football" => football,
            _ => null
        };

        public Cue Find(GameSound sound) => Array.Find(cues, cue => cue.sound == sound);
    }
}
