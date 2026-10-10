#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.Tests.EditorTools
{
    /// <summary>Which sports-supplement pose each saved scene and asset draws.</summary>
    public sealed class SupplementPoseWiringTests
    {
        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void FootballKicksFromBehindAgainstTheStudentKeeper()
        {
            EditorSceneManager.OpenScene(FootballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            FootballPoseSprites poses = Object.FindFirstObjectByType<FootballPresentation>().Poses;
            Assert.That(PoseNames(new[] { poses.kickerReady, poses.kickerRunUp, poses.kickerStrike, poses.kickerCelebrate }),
                Is.EqualTo(new[]
                {
                    "MaleAdventurer_footballBackIdle", "MaleAdventurer_footballBackApproach",
                    "MaleAdventurer_footballBackKick", "MaleAdventurer_footballBackCelebrate"
                }));
            string[] keeper = PoseNames(new[] { poses.keeperReady, poses.keeperSave, poses.keeperBeaten });
            Assert.That(keeper, Is.EqualTo(new[] { "StudentKeeper_idle", "StudentKeeper_cheer", "StudentKeeper_recover" }));
            // The live ball stays visible on a save, so a keeper drawn holding one would show two balls.
            Assert.That(keeper, Has.None.EndWith("Ball"));
            Assert.That(PoseName(GameObject.Find("FootballWorld/Goalkeeper").GetComponent<SpriteRenderer>().sprite),
                Is.EqualTo("StudentKeeper_idle"));
        }

        [Test]
        public void VolleyballPlayerUsesVolleyballPosesAndTheOpponentKeepsHers()
        {
            EditorSceneManager.OpenScene(VolleyballSceneConfigurator.ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<VolleyballController>();
            string[] Player(AthleteAction action) => PoseNames(controller.PlayerView.FramesFor(action));
            string[] Opponent(AthleteAction action) => PoseNames(controller.OpponentView.FramesFor(action));

            Assert.That(Player(AthleteAction.Idle), Is.EqualTo(new[] { "MaleAdventurer_volleyReady" }));
            Assert.That(Player(AthleteAction.Run),
                Is.EqualTo(new[] { "MaleAdventurer_volleyShuffleRight", "MaleAdventurer_volleyShuffleLeft" }));
            Assert.That(Player(AthleteAction.Receive),
                Is.EqualTo(new[] { "MaleAdventurer_volleyDig", "MaleAdventurer_volleyRecover" }));
            Assert.That(Player(AthleteAction.Smash),
                Is.EqualTo(new[] { "MaleAdventurer_volleySpikeWindup", "MaleAdventurer_volleySpikeContact" }));
            Assert.That(Player(AthleteAction.Block),
                Is.EqualTo(new[] { "MaleAdventurer_volleyJumpLoad", "MaleAdventurer_volleySet" }));
            Assert.That(Player(AthleteAction.Dive),
                Is.EqualTo(new[] { "MaleAdventurer_fallForwardRight", "MaleAdventurer_fallSitRight" }));

            Assert.That(Opponent(AthleteAction.Idle), Is.EqualTo(new[] { "FemaleAdventurer_idle" }));
            Assert.That(Opponent(AthleteAction.Run), Is.EqualTo(new[]
                { "FemaleAdventurer_run0", "FemaleAdventurer_run1", "FemaleAdventurer_run2", "FemaleAdventurer_run1" }));
            Assert.That(Opponent(AthleteAction.Smash), Is.EqualTo(new[] { "FemaleAdventurer_jump", "FemaleAdventurer_attack1" }));
        }

        internal static string PoseName(Sprite sprite) =>
            sprite == null ? "<null>" : Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(sprite));

        internal static string[] PoseNames(IEnumerable<Sprite> sprites) => sprites.Select(PoseName).ToArray();
    }
}
#endif
