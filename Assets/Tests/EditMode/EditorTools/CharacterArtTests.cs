#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class CharacterArtTests
    {
        [Test]
        public void HeroIsTheMaleAdventurer()
        {
            Assert.That(CharacterArt.Hero, Is.EqualTo("MaleAdventurer"));
            Assert.That(CharacterArt.Characters, Does.Contain(CharacterArt.Hero));
        }

        [Test]
        public void EveryPoseImportsAtRunnerSize()
        {
            CharacterArt.ImportAll();
            foreach (string character in CharacterArt.Characters)
                AssertRunnerSize(character, CharacterArt.Poses);
        }

        [Test]
        public void SupplementPosesImportAtRunnerSize()
        {
            CharacterArt.ImportAll();
            AssertRunnerSize(CharacterArt.Hero, CharacterArt.HeroPoses);
            AssertRunnerSize(CharacterArt.Boss, CharacterArt.BossPoses);
            AssertRunnerSize(CharacterArt.Keeper, CharacterArt.KeeperPoses);
        }

        [Test]
        public void PoseListsCoverTheSupplement()
        {
            Assert.That(CharacterArt.HeroPoses, Has.Length.EqualTo(54).And.Unique);
            Assert.That(CharacterArt.HeroPoses, Does.Contain("footballBackKick").And.Contain("volleySpikeContact")
                .And.Contain("frogAirRight").And.Contain("disappointed"));
            Assert.That(CharacterArt.BossPoses, Has.Length.EqualTo(38).And.Unique);
            Assert.That(CharacterArt.BossPoses, Does.Contain("scoreWrite").And.Contain("congratulate"));
            Assert.That(CharacterArt.KeeperPoses, Has.Length.EqualTo(12).And.Unique);
            Assert.That(CharacterArt.KeeperPoses, Does.Contain("ready").And.Contain("holdBall"));
            Assert.That(CharacterArt.Keeper, Is.EqualTo("StudentKeeper"));
            Assert.That(CharacterArt.Characters, Does.Not.Contain(CharacterArt.Keeper),
                "Characters lists only the folders holding all 18 shared poses.");
        }

        [Test]
        public void PortraitsImportAsUiSprites()
        {
            CharacterArt.ImportAll();
            Assert.That(CharacterArt.Portraits, Has.Length.EqualTo(12));
            foreach (string name in CharacterArt.Portraits)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterArt.PortraitPath(name));
                Assert.That(sprite, Is.Not.Null, name);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(256f, 256f)), name);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(128f, 128f)), name);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
            }
        }

        [Test]
        public void IsPoseOfTellsCharactersApart()
        {
            CharacterArt.ImportAll();
            Sprite hero = CharacterArt.Load(CharacterArt.Hero, "idle");
            Assert.That(CharacterArt.IsPoseOf(hero, "MalePerson"), Is.False);
            Assert.That(CharacterArt.IsPoseOf(null, CharacterArt.Hero), Is.False);
        }

        [Test]
        public void MissingPoseNamesThePath()
        {
            var error = Assert.Throws<InvalidOperationException>(
                () => CharacterArt.Load(CharacterArt.Hero, "notAPose"));
            Assert.That(error.Message, Does.Contain("MaleAdventurer/MaleAdventurer_notAPose.png"));
        }

        static void AssertRunnerSize(string character, string[] poses)
        {
            foreach (string pose in poses)
            {
                Sprite sprite = CharacterArt.Load(character, pose);
                string label = character + " " + pose;
                Assert.That(sprite.texture.width, Is.EqualTo(192), label);
                Assert.That(sprite.texture.height, Is.EqualTo(256), label);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(CharacterArt.PixelsPerUnit), label);
                // The Sprint lanes were tuned for 0.96 x 1.28 runners standing on their pivot.
                Assert.That(sprite.bounds.size.x, Is.EqualTo(.96f).Within(.001f), label);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(1.28f).Within(.001f), label);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)), label);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), label);
                Assert.That(CharacterArt.IsPoseOf(sprite, character), Is.True, label);
            }
        }
    }
}
#endif
