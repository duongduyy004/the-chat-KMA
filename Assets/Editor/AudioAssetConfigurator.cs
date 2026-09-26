using System;
using System.IO;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace KMA.EditorTools
{
    public static class AudioAssetConfigurator
    {
        const string ThirdParty = "Assets/_Project/Audio/ThirdParty/";
        const string Prepared = "Assets/_Project/Audio/Prepared/";
        const string LibraryPath = "Assets/_Project/Resources/GameAudioLibrary.asset";

        [MenuItem("KMA/Audio/Configure Downloaded Audio")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
            AssetDatabase.Refresh();
            var library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath);
            if (!library)
            {
                library = ScriptableObject.CreateInstance<GameAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Settings/Audio/KMA-AudioMixer.mixer");
            library.musicGroup = mixer.FindMatchingGroups("Music").Single();
            library.sfxGroup = mixer.FindMatchingGroups("SFX").Single();
            library.menu = Music("Move Forward");
            library.sprint = Music("Cipher2");
            library.volleyball = Music("Beachfront Celebration");
            library.football = Music("Winner Winner");
            library.cues = new[]
            {
                Cue(GameSound.Click, .4f, .06f, Interface("click_003")),
                Cue(GameSound.Countdown, .55f, .5f, Interface("select_002")),
                Cue(GameSound.Whistle, .55f, .3f, Sample("whistle")),
                Cue(GameSound.Win, .55f, 1f, Jingle("jingles_PIZZI01")),
                Cue(GameSound.Lose, .5f, 1f, Jingle("jingles_PIZZI04")),
                Cue(GameSound.Cheer, .6f, 1f, Sample("cheer")),
                Cue(GameSound.Point, .55f, .3f, Interface("confirmation_002")),
                Cue(GameSound.Miss, .35f, .3f, Interface("error_002")),
                Cue(GameSound.RunStep, .6f, .1f, Sample("run-step-1"), Sample("run-step-2"), Sample("run-step-3"), Sample("run-step-4")),
                Cue(GameSound.SandStep, .5f, .16f, Sample("sand-step-1"), Sample("sand-step-2"), Sample("sand-step-3"), Sample("sand-step-4")),
                Cue(GameSound.VolleyHit, .85f, .07f, Sample("volley-hit")),
                Cue(GameSound.Kick, .9f, .12f, Sample("kick-1"), Sample("kick-2"), Sample("kick-3")),
                Cue(GameSound.Save, .55f, .2f, Impact("impactSoft_medium_001")),
                Cue(GameSound.Post, .6f, .2f, Impact("impactMetal_light_001"))
            };
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA Audio] Configured 4 music tracks and 14 cues; originals preserved.");
        }

        static GameAudioLibrary.Cue Cue(GameSound sound, float volume, float interval, params AudioClip[] clips) =>
            new GameAudioLibrary.Cue { sound = sound, clips = clips, volume = volume, minimumInterval = interval };

        static AudioClip Music(string name) => Load(ThirdParty + "KevinMacLeod/" + name + ".mp3", true);
        static AudioClip Sample(string name) => Load(Prepared + name + ".wav", false);
        static AudioClip Interface(string name) => Load(ThirdParty + "Kenney/interface-sounds/Audio/" + name + ".ogg", false);
        static AudioClip Impact(string name) => Load(ThirdParty + "Kenney/impact-sounds/Audio/" + name + ".ogg", false);
        static AudioClip Jingle(string name) => Load(ThirdParty + "Kenney/music-jingles/Audio/Pizzicato jingles/" + name + ".ogg", false);

        static AudioClip Load(string path, bool music)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (!importer) throw new InvalidOperationException("Missing audio: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = .7f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !music;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
