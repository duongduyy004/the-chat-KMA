using System.Collections;
using System.Linq;
using System.Reflection;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class AudioPlaybackTests
    {
        GameObject root;
        AudioManager manager;
        GameAudioLibrary testLibrary;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var old in Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None))
                Object.Destroy(old);
            yield return null;
            Time.timeScale = 1f;
            root = new GameObject("AudioPlaybackTests");
            manager = root.AddComponent<AudioManager>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(root);
            if (testLibrary) Object.Destroy(testLibrary);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneMusicUsesDistinctTracksAndMapDoesNotRestartMenu()
        {
            PlayScene("Menu");
            yield return null;
            AudioSource music = Music();
            Assert.That(music.clip, Is.Not.Null);
            Assert.That(music.clip.name, Is.EqualTo("Move Forward"));
            music.time = 5f;
            PlayScene("Map");
            Assert.That(music.time, Is.GreaterThan(4f), "Map must keep the shared menu music playing.");
            foreach (var scene in new[] { "MG_Sprint", "MG_Volleyball", "MG_Football" })
            {
                AudioClip previous = music.clip;
                PlayScene(scene);
                Assert.That(music.clip, Is.Not.Null);
                Assert.That(music.clip, Is.Not.SameAs(previous));
            }
            PlayScene("UnrelatedTestScene");
            Assert.That(music.clip, Is.Null, "Leaving game scenes must release the music.");
        }

        [UnityTest]
        public IEnumerator PauseFreezesMusicPositionAndResumeKeepsIt()
        {
            PlayScene("MG_Sprint");
            yield return null;
            var music = Music();
            music.time = 4f;
            Time.timeScale = 0f;
            yield return null;
            float paused = music.time;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(music.time, Is.EqualTo(paused).Within(.03f));
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(music.time, Is.GreaterThan(paused));
        }

        [UnityTest]
        public IEnumerator RepeatedCueIsThrottledAndPauseAllowsOnlyInterfaceSounds()
        {
            manager.PlayCue(GameSound.Kick);
            manager.PlayCue(GameSound.Kick);
            Assert.That(EffectClips("kick-").Length, Is.EqualTo(1), "Duplicate requests in a frame must not stack.");
            Time.timeScale = 0f;
            yield return null;
            manager.PlayCue(GameSound.VolleyHit);
            Assert.That(EffectClips("volley-hit"), Is.Empty);
            manager.PlayCue(GameSound.Click);
            Assert.That(EffectClips("click_003").Length, Is.EqualTo(1), "Pause menu must retain click feedback.");
            Assert.That(EffectClips("kick-").Single().isPlaying, Is.False, "Gameplay effects stop when paused.");
        }

        [UnityTest]
        public IEnumerator MusicAndSfxSettingsRemainIndependentDuringSceneChanges()
        {
            manager.SetMusicVolume(.25f);
            manager.SetSfxVolume(.6f);
            PlayScene("MG_Volleyball");
            yield return null;
            var mixer = Music().outputAudioMixerGroup.audioMixer;
            Assert.That(mixer.GetFloat("MusicVolume", out float music), Is.True);
            Assert.That(mixer.GetFloat("SfxVolume", out float sfx), Is.True);
            Assert.That(music, Is.EqualTo(-12.0412f).Within(.01f));
            Assert.That(sfx, Is.EqualTo(-4.43697f).Within(.01f));
            manager.SetMusicVolume(0f);
            PlayScene("Map");
            mixer.GetFloat("MusicVolume", out music);
            mixer.GetFloat("SfxVolume", out sfx);
            Assert.That(music, Is.EqualTo(-80f));
            Assert.That(sfx, Is.EqualTo(-4.43697f).Within(.01f));
        }

        [UnityTest]
        public IEnumerator ChangingSceneStopsGameplayEffectsAndDisabledManagerDoesNotReceiveCues()
        {
            GameAudio.Play(GameSound.Cheer);
            Assert.That(EffectClips("cheer").Length, Is.EqualTo(1));
            PlayScene("Map");
            Assert.That(EffectClips("cheer").Single().isPlaying, Is.False);
            manager.enabled = false;
            GameAudio.Play(GameSound.Kick);
            Assert.That(EffectClips("kick-"), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseTransitionDoesNotCutOffTheInterfaceClick()
        {
            testLibrary = Object.Instantiate(Resources.Load<GameAudioLibrary>("GameAudioLibrary"));
            // Long known clip isolates pause routing from the length of the actual tiny click.
            testLibrary.Find(GameSound.Click).clips = testLibrary.Find(GameSound.Cheer).clips;
            typeof(AudioManager).GetField("library", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, testLibrary);
            Time.timeScale = 0f;
            manager.PlayCue(GameSound.Click);
            var click = EffectClips("cheer").Single();
            Assert.That(click.isPlaying, Is.True);
            yield return null;
            Assert.That(click.isPlaying, Is.True, "Entering pause must not cut off the click that opened its menu.");
        }

        AudioSource[] EffectClips(string prefix) => root.GetComponents<AudioSource>()
            .Where(source => !source.loop && source.clip && source.clip.name.StartsWith(prefix)).ToArray();

        void PlayScene(string scene)
        {
            var method = typeof(AudioManager).GetMethod("PlayMusicForScene", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Scene music playback has not been implemented.");
            method.Invoke(manager, new object[] { scene });
        }

        AudioSource Music() => root.GetComponents<AudioSource>().Single(source => source.loop);
    }
}
