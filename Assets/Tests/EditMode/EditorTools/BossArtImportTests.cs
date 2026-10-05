using KMA.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class BossArtImportTests
    {
        [OneTimeSetUp]
        public void ImportOnce() => ChessArtImporter.ImportAll();

        [TestCase("idleBoss")]
        [TestCase("chessThink")]
        [TestCase("chessMove")]
        [TestCase("whistle0")]
        [TestCase("whistle1")]
        [TestCase("strictLook")]
        [TestCase("taunt")]
        [TestCase("cheer0")]
        public void BossPosesImportAsBottomCenterSprites(string pose)
        {
            Sprite sprite = CharacterArt.Load(CharacterArt.Boss, pose);
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(CharacterArt.PixelsPerUnit));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(96f, 0f)));
            Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(192f, 256f)));
        }

        [Test]
        public void ChessPiecesAndIconAreUiSprites()
        {
            foreach (string name in new[] { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" })
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Chess/Pieces/{name}.png"),
                    Is.Not.Null, name);
            Assert.That(Resources.Load<Sprite>("Icons/SportIcon_Chess"), Is.Not.Null);
        }
    }
}
