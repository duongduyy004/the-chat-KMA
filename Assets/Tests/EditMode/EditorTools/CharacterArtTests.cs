#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using NUnit.Framework;
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
            foreach (string pose in CharacterArt.Poses)
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
    }
}
#endif
