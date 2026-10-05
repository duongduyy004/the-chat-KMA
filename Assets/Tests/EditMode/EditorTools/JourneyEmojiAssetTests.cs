using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class JourneyEmojiAssetTests
    {
        static TMP_SpriteAsset Load() => Resources.Load<TMP_SpriteAsset>("Journey/JourneyEmoji");

        [Test]
        public void EmojiSpriteAssetHasOneNamedGlyphPerKnownCode()
        {
            TMP_SpriteAsset asset = Load();
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.spriteSheet, Is.Not.Null);
            Assert.That(asset.material, Is.Not.Null);
            Assert.That(new SerializedObject(asset).FindProperty("m_Version").stringValue, Is.EqualTo("1.1.0"),
                "An empty version makes TMP upgrade the asset on load and wipe its glyphs.");
            Assert.That(asset.spriteCharacterTable.Select(character => character.name),
                Is.EqualTo(DialogueEmoji.KnownNames));
            Assert.That(asset.spriteGlyphTable.Count, Is.EqualTo(DialogueEmoji.KnownNames.Count));
            foreach (TMP_SpriteGlyph glyph in asset.spriteGlyphTable)
            {
                Assert.That(glyph.glyphRect.width, Is.EqualTo(72));
                Assert.That(glyph.glyphRect.height, Is.EqualTo(72));
                Assert.That(glyph.glyphRect.x + glyph.glyphRect.width, Is.LessThanOrEqualTo(asset.spriteSheet.width));
                Assert.That(glyph.glyphRect.y + glyph.glyphRect.height, Is.LessThanOrEqualTo(asset.spriteSheet.height));
            }
        }

        [Test]
        public void EveryEmojiCodeInTheScriptHasAGlyph()
        {
            TMP_SpriteAsset asset = Load();
            Assert.That(asset, Is.Not.Null);
            JourneyDialogueLibrary library = JourneyDialogueLibrary.LoadDefault();
            foreach (JourneyDialogueNode node in library.Nodes)
                foreach (JourneyDialogueLine line in node.Lines)
                    foreach (string code in DialogueEmoji.FindCodes(line.Text))
                        Assert.That(asset.GetSpriteIndexFromName(code), Is.GreaterThanOrEqualTo(0), $"{node.Id}: :{code}:");
        }
    }
}
