#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class ChessArtImporter
    {
        public const string PieceFolder = "Assets/_Project/Art/Chess/Pieces";
        public const string IconPath = "Assets/_Project/Resources/Icons/SportIcon_Chess.png";
        /// Order matches ChessBoardView.pieceSprites: White P N B R Q K, then Black.
        public static readonly string[] PieceNames =
            { "wP", "wN", "wB", "wR", "wQ", "wK", "bP", "bN", "bB", "bR", "bQ", "bK" };

        [MenuItem("KMA/Chess Final/Import Art")]
        public static void ImportAll()
        {
            CharacterArt.ImportAll();
            foreach (string name in PieceNames) EnsureUiSprite($"{PieceFolder}/{name}.png");
            EnsureUiSprite(IconPath);
        }

        public static Sprite[] LoadPieces() => PieceNames
            .Select(name => AssetDatabase.LoadAssetAtPath<Sprite>($"{PieceFolder}/{name}.png") ??
                throw new InvalidOperationException($"[KMA] Missing chess piece sprite {name}."))
            .ToArray();

        static void EnsureUiSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("[KMA] Missing texture " + path);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled &&
                importer.filterMode == FilterMode.Bilinear && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.alphaIsTransparency)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
