#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KMA.EditorTools
{
    /// <summary>
    /// The one place the project character poses are imported and loaded. Every minigame draws
    /// its characters through here, so the hero keeps one size, pivot and filter in all scenes.
    /// </summary>
    public static class CharacterArt
    {
        public const string Root = "Assets/_Project/Art/Characters/";
        /// <summary>The player's character in every minigame.</summary>
        public const string Hero = "MaleAdventurer";
        /// <summary>192x256 HD poses at this density are 0.96 x 1.28 units, the Sprint runner size.</summary>
        public const float PixelsPerUnit = 200f;
        const int MaxTextureSize = 512;

        public static readonly string[] Characters = { Hero, "MalePerson", "FemalePerson", "FemaleAdventurer" };

        public static readonly string[] Poses =
        {
            "idle", "run0", "run1", "run2", "hit", "cheer0", "cheer1", "fallDown", "back",
            "climb0", "climb1", "hurt", "duck", "hold", "jump", "attack1", "slide", "fall"
        };

        public static string PosePath(string character, string pose) =>
            Root + character + "/" + character + "_" + pose + ".png";

        /// <summary>
        /// Imports every pose before anything is loaded: reimporting a texture invalidates Sprite
        /// references handed out earlier, so importing and loading must not interleave.
        /// </summary>
        public static void ImportAll()
        {
            foreach (string character in Characters)
                foreach (string pose in Poses)
                    Import(PosePath(character, pose));
        }

        public static Sprite Load(string character, string pose)
        {
            string path = PosePath(character, pose);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("[KMA] Character pose is missing or not imported: " + path);
            return sprite;
        }

        public static Sprite[] Frames(string character, params string[] poses) =>
            poses.Select(pose => Load(character, pose)).ToArray();

        public static bool IsPoseOf(Sprite sprite, string character) =>
            sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(Root + character + "/", StringComparison.Ordinal);

        static void Import(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "[KMA] Missing character pose: " + path + ". Every character folder needs all 18 poses.", path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var current = new TextureImporterSettings();
            importer.ReadTextureSettings(current);
            // Skipping an already-correct texture keeps earlier Sprite references alive.
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled &&
                importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.maxTextureSize == MaxTextureSize &&
                current.spriteMeshType == SpriteMeshType.FullRect &&
                current.spriteAlignment == (int)SpriteAlignment.BottomCenter)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = MaxTextureSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
#endif
