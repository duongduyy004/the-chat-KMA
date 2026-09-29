#if UNITY_EDITOR
using System;
using KMA.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class ToonCharacterArtTests
    {
        [Test]
        public void HeroIsTheMaleAdventurer()
        {
            Assert.That(ToonCharacterArt.Hero, Is.EqualTo("MaleAdventurer"));
            Assert.That(ToonCharacterArt.Characters, Does.Contain(ToonCharacterArt.Hero));
        }

        [Test]
        public void EveryPoseImportsAtRunnerSize()
        {
            ToonCharacterArt.ImportAll();
            foreach (string character in ToonCharacterArt.Characters)
            foreach (string pose in ToonCharacterArt.Poses)
            {
                Sprite sprite = ToonCharacterArt.Load(character, pose);
                string label = character + " " + pose;
                Assert.That(sprite.texture.width, Is.EqualTo(192), label);
                Assert.That(sprite.texture.height, Is.EqualTo(256), label);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(ToonCharacterArt.PixelsPerUnit), label);
                // The Sprint lanes were tuned for 0.96 x 1.28 runners standing on their pivot.
                Assert.That(sprite.bounds.size.x, Is.EqualTo(.96f).Within(.001f), label);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(1.28f).Within(.001f), label);
                Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)), label);
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear), label);
                Assert.That(ToonCharacterArt.IsPoseOf(sprite, character), Is.True, label);
            }
        }

        [Test]
        public void IsPoseOfTellsCharactersApart()
        {
            ToonCharacterArt.ImportAll();
            Sprite hero = ToonCharacterArt.Load(ToonCharacterArt.Hero, "idle");
            Assert.That(ToonCharacterArt.IsPoseOf(hero, "MalePerson"), Is.False);
            Assert.That(ToonCharacterArt.IsPoseOf(null, ToonCharacterArt.Hero), Is.False);
        }

        [Test]
        public void MissingPoseNamesThePath()
        {
            var error = Assert.Throws<InvalidOperationException>(
                () => ToonCharacterArt.Load(ToonCharacterArt.Hero, "notAPose"));
            Assert.That(error.Message, Does.Contain("MaleAdventurer/MaleAdventurer_notAPose.png"));
        }
    }
}
#endif
