using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum GameSound
    {
        Click, Countdown, Whistle, Win, Lose, Cheer, Point, Miss,
        RunStep, SandStep, VolleyHit, Kick, Save, Post
    }

    // Presentation requests only: rules and input never depend on an audio device.
    public static class GameAudio
    {
        public static event Action<GameSound> Requested;
        public static void Play(GameSound sound) => Requested?.Invoke(sound);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Requested = null;
    }
}
