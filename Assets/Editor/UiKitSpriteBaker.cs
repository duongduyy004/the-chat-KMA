#if UNITY_EDITOR
using System;
using System.IO;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    /// Writes the UI kit's white, anti-aliased sprites once and points UiKitAssets at them.
    /// Re-running rewrites the pixels in place, so GUIDs and scene references survive.
    public static class UiKitSpriteBaker
    {
        public const string SpriteDir = "Assets/_Project/Art/UI/Kit";
        public const int CircleDiameter = 128;
        public const float RingWidth = CircleDiameter * 3f / 64f;
        const string FontPath = "Assets/Fonts/TMP/BarlowSemiCondensed-Bold.asset";
        const string OutlineMaterialPath = "Assets/Fonts/TMP/Materials/Button_Primary.mat";

        [MenuItem("KMA/UI/Bake UI Kit Sprites")]
        public static void Bake()
        {
            Directory.CreateDirectory(SpriteDir);
            Directory.CreateDirectory(Path.GetDirectoryName(UiKitAssets.AssetPath));

            Sprite roundRect20 = WriteRoundRect(20);
            Sprite roundRect24 = WriteRoundRect(24);
            Sprite roundRect36 = WriteRoundRect(36);
            Sprite circle = Write("Circle", CircleDiameter, UiShapeRaster.Circle(CircleDiameter), Vector4.zero);
            Sprite ring = Write("Ring", CircleDiameter, UiShapeRaster.Ring(CircleDiameter, RingWidth), Vector4.zero);

            var assets = AssetDatabase.LoadAssetAtPath<UiKitAssets>(UiKitAssets.AssetPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<UiKitAssets>();
                AssetDatabase.CreateAsset(assets, UiKitAssets.AssetPath);
            }

            assets.Configure(roundRect20, roundRect24, roundRect36, circle, ring,
                Load<TMP_FontAsset>(FontPath), Load<Material>(OutlineMaterialPath));
            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] UI kit sprites baked.");
        }

        static Sprite WriteRoundRect(int radius) =>
            Write("RoundRect" + radius, UiShapeRaster.RoundedRectSize(radius), UiShapeRaster.RoundedRect(radius),
                new Vector4(radius, radius, radius, radius));

        static Sprite Write(string name, int size, Color32[] pixels, Vector4 border)
        {
            string path = $"{SpriteDir}/{name}.png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return Load<Sprite>(path);
        }

        static T Load<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("[KMA] Missing " + path);
    }
}
#endif
