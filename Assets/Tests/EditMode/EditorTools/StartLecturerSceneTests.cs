using System.Collections.Generic;
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
            var wiring = new SerializedObject(lecturers[0]);
            foreach (string gesture in new[] { "count", "command", "strictLook", "taunt", "cheer", "cheerBothArms", "penalty", "penaltySide" })
                Assert.That(CharacterArt.IsPoseOf((Sprite)wiring.FindProperty(gesture).objectReferenceValue, CharacterArt.Boss),
                    Is.True, gesture);
        }

        [Test]
        public void SheCountsDownRaisesTheWhistleAndBlowsItWhenPlayStarts()
        {
            var go = new GameObject("lecturer");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var lecturer = go.AddComponent<StartLecturer>();
                Sprite wait = Boss("idleBoss");
                Sprite raise = Boss("whistle0");
                Sprite blow = Boss("whistle1");
                lecturer.Configure(renderer, wait, raise, blow, true, 3f);
                ConfigureGestures(lecturer);

                lecturer.OnPhaseChanged(MinigamePhase.Tutorial);
                Assert.That(renderer.sprite, Is.SameAs(wait));
                lecturer.OnPhaseChanged(MinigamePhase.Countdown);
                Assert.That(renderer.sprite, Is.SameAs(Boss("count")));
                lecturer.Tick(StartLecturer.CountSeconds + .01f);
                Assert.That(renderer.sprite, Is.SameAs(raise));
                lecturer.Tick(5f);
                Assert.That(renderer.sprite, Is.SameAs(raise), "she holds the whistle up until play starts");
                lecturer.OnPhaseChanged(MinigamePhase.Play);
                Assert.That(renderer.sprite, Is.SameAs(blow));
                lecturer.Tick(StartLecturer.BlastSeconds + .01f);
                Assert.That(renderer.sprite, Is.SameAs(Boss("command")), "she points the athletes off");
                lecturer.Tick(3f);
                Assert.That(lecturer.Alpha, Is.EqualTo(0f));
                lecturer.OnPhaseChanged(MinigamePhase.Countdown);
                Assert.That(lecturer.Alpha, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SheKeepsGesturingThroughTheTutorialAndPlay()
        {
            var go = new GameObject("lecturer");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var lecturer = go.AddComponent<StartLecturer>();
                lecturer.Configure(renderer, Boss("idleBoss"), Boss("whistle0"), Boss("whistle1"), false, 0f);
                ConfigureGestures(lecturer);

                foreach (MinigamePhase phase in new[] { MinigamePhase.Tutorial, MinigamePhase.Play })
                {
                    lecturer.OnPhaseChanged(phase);
                    var seen = new HashSet<StartLecturer.Pose>();
                    for (int i = 0; i < 300; i++)
                    {
                        lecturer.Tick(.1f);
                        seen.Add(lecturer.Current);
                    }
                    Assert.That(seen, Is.SupersetOf(new[]
                    {
                        StartLecturer.Pose.Waiting, StartLecturer.Pose.Command,
                        StartLecturer.Pose.StrictLook, StartLecturer.Pose.Taunt
                    }), phase.ToString());
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [TestCase(true, "cheer0", "cheer1")]
        [TestCase(false, "penalty0", "penalty1")]
        public void SheCheersAPassAndPointsDownAtAFail(bool passed, string first, string second)
        {
            var go = new GameObject("lecturer");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var lecturer = go.AddComponent<StartLecturer>();
                lecturer.Configure(renderer, Boss("idleBoss"), Boss("whistle0"), Boss("whistle1"), false, 1f);
                ConfigureGestures(lecturer);
                lecturer.OnPhaseChanged(MinigamePhase.Play);
                lecturer.Tick(2f);

                lecturer.OnPhaseChanged(MinigamePhase.Resolve);
                lecturer.OnResultDecided(passed);
                Assert.That(lecturer.Alpha, Is.EqualTo(1f));
                Assert.That(renderer.sprite, Is.SameAs(Boss(first)));
                lecturer.Tick(.65f);
                Assert.That(renderer.sprite, Is.SameAs(Boss(second)));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AMissingGestureFallsBackToTheWaitingPose()
        {
            var go = new GameObject("lecturer");
            try
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                var lecturer = go.AddComponent<StartLecturer>();
                Sprite wait = Boss("idleBoss");
                lecturer.Configure(renderer, wait, Boss("whistle0"), Boss("whistle1"), false, 0f);
                lecturer.OnResultDecided(true);
                Assert.That(renderer.sprite, Is.SameAs(wait));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static Sprite Boss(string pose) => CharacterArt.Load(CharacterArt.Boss, pose);

        static void ConfigureGestures(StartLecturer lecturer) =>
            lecturer.ConfigureGestures(Boss("count"), Boss("command"), Boss("strictLook"), Boss("taunt"),
                Boss("cheer0"), Boss("cheer1"), Boss("penalty0"), Boss("penalty1"));
    }
}
