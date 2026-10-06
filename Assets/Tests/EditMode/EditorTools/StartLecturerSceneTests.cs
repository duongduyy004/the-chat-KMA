using KMA.EditorTools;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class StartLecturerSceneTests
    {
        [OneTimeSetUp]
        public void ImportOnce() => CharacterArt.ImportAll();

        // The lecturer faces the athletes: left of her in Football and Frog Jump, right of her in Sprint and Volleyball.
        [TestCase("MG_Sprint", false)]
        [TestCase("MG_Football", true)]
        [TestCase("MG_FrogJump", true)]
        [TestCase("MG_Volleyball", false)]
        public void EverySportsSceneHasOneRedShirtLecturerFacingTheAthletes(string scene, bool facesLeft)
        {
            EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{scene}.unity", OpenSceneMode.Single);
            StartLecturer[] lecturers = Object.FindObjectsByType<StartLecturer>(FindObjectsSortMode.None);
            Assert.That(lecturers, Has.Length.EqualTo(1));
            var body = lecturers[0].GetComponent<SpriteRenderer>();
            Assert.That(CharacterArt.IsPoseOf(body.sprite, CharacterArt.Boss), Is.True);
            Assert.That(body.flipX, Is.EqualTo(facesLeft));
            Assert.That(lecturers[0].FacesLeft, Is.EqualTo(facesLeft));
        }

        [Test]
        public void SheRaisesTheWhistleInTheCountdownAndBlowsItWhenPlayStarts()
        {
            var go = new GameObject("lecturer");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var lecturer = go.AddComponent<StartLecturer>();
                Sprite wait = CharacterArt.Load(CharacterArt.Boss, "idleBoss");
                Sprite raise = CharacterArt.Load(CharacterArt.Boss, "whistle0");
                Sprite blow = CharacterArt.Load(CharacterArt.Boss, "whistle1");
                lecturer.Configure(renderer, wait, raise, blow, true, 1f);

                lecturer.OnPhaseChanged(MinigamePhase.Tutorial);
                Assert.That(renderer.sprite, Is.SameAs(wait));
                lecturer.OnPhaseChanged(MinigamePhase.Countdown);
                Assert.That(renderer.sprite, Is.SameAs(raise));
                lecturer.OnPhaseChanged(MinigamePhase.Play);
                Assert.That(renderer.sprite, Is.SameAs(blow));
                lecturer.Tick(StartLecturer.BlastSeconds + .01f);
                Assert.That(renderer.sprite, Is.SameAs(wait));
                lecturer.Tick(2f);
                Assert.That(lecturer.Alpha, Is.EqualTo(0f));
                lecturer.OnPhaseChanged(MinigamePhase.Countdown);
                Assert.That(lecturer.Alpha, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
