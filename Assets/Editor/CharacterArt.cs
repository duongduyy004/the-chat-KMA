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

        public const string Boss = "BossPE";

        /// BossPE has the 18 shared poses plus 20 of its own.
        public static readonly string[] BossPoses = Poses.Concat(new[]
        {
            "idleBoss", "whistle0", "whistle1", "command", "ready", "taunt",
            "chessThink", "chessMove", "strictLook", "penalty0", "penalty1", "count",
            "angry", "clap0", "clap1", "congratulate", "handsOnHips", "scoreHold", "scoreWrite", "think"
        }).ToArray();

        /// The hero has the 18 shared poses plus the sports supplement's 36.
        public static readonly string[] HeroPoses = Poses.Concat(new[]
        {
            "sprintStart", "sprintLaunch", "sprintFinish", "fallForwardRight", "fallSitRight", "getUpRight",
            "frogReadyRight", "frogTakeoffRight", "frogAirRight", "frogLandRight",
            "volleyReady", "volleyReceive", "volleyDig", "volleySet", "volleyRecover", "volleyJumpLoad",
            "volleySpikeWindup", "volleySpikeContact", "volleySpikeFollow", "volleyLand",
            "volleyShuffleLeft", "volleyShuffleRight",
            "footballBackIdle", "footballBackApproach", "footballBackWindup", "footballBackKick",
            "footballBackFollow", "footballBackCelebrate",
            "happy", "celebrate", "thumbsUp", "fear", "pokerFace", "surprise", "think", "disappointed"
        }).ToArray();

        /// <summary>The football goalkeeper: a different student, with only goalkeeping poses.</summary>
        public const string Keeper = "StudentKeeper";

        /// Left/Right mean screen left/right. The *Ball poses have a ball drawn in.
        public static readonly string[] KeeperPoses =
        {
            "idle", "ready", "reachLeftEmpty", "reachRightEmpty", "diveLeftEmpty", "diveRightEmpty",
            "catchLeftBall", "catchRightBall", "holdBall", "highCatchBall", "recover", "cheer"
        };

        public const string PortraitRoot = "Assets/_Project/Art/UI/Portraits/";
        const float PortraitPixelsPerUnit = 100f;
        const int PortraitMaxTextureSize = 256;

        /// 256x256 head-and-shoulders portraits for UI; nothing references them yet.
        public static readonly string[] Portraits =
        {
            "BossPE_angry", "BossPE_congratulate", "BossPE_handsOnHips", "BossPE_think",
            "Hero_celebrate", "Hero_disappointed", "Hero_fear", "Hero_happy",
            "Hero_pokerFace", "Hero_surprise", "Hero_think", "Hero_thumbsUp"
        };

        public static string PosePath(string character, string pose) =>
            Root + character + "/" + character + "_" + pose + ".png";

        public static string PortraitPath(string name) => PortraitRoot + name + ".png";

        /// <summary>
        /// Imports every pose before anything is loaded: reimporting a texture invalidates Sprite
        /// references handed out earlier, so importing and loading must not interleave.
        /// </summary>
        public static void ImportAll()
        {
            foreach (string character in Characters)
                foreach (string pose in character == Hero ? HeroPoses : Poses)
                    Import(PosePath(character, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string pose in BossPoses)
                Import(PosePath(Boss, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string pose in KeeperPoses)
                Import(PosePath(Keeper, pose), PixelsPerUnit, MaxTextureSize, SpriteAlignment.BottomCenter);
            foreach (string name in Portraits)
                Import(PortraitPath(name), PortraitPixelsPerUnit, PortraitMaxTextureSize, SpriteAlignment.Center);
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

        static void Import(string path, float pixelsPerUnit, int maxTextureSize, SpriteAlignment alignment)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("[KMA] Missing character art: " + path, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var current = new TextureImporterSettings();
            importer.ReadTextureSettings(current);
            // Skipping an already-correct texture keeps earlier Sprite references alive.
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) &&
                importer.filterMode == FilterMode.Bilinear && !importer.mipmapEnabled &&
                importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp &&
                importer.maxTextureSize == maxTextureSize &&
                current.spriteMeshType == SpriteMeshType.FullRect &&
                current.spriteAlignment == (int)alignment)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = alignment == SpriteAlignment.BottomCenter ? new Vector2(.5f, 0f) : new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
#endif
