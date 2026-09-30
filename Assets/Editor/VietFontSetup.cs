using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace KMA.EditorTools
{
    public static class VietFontSetup
    {
        public const string Root = "Assets/Fonts/TMP";
        public const string Sample = "Thể Chất KMA – Nguyễn Quỳnh Ầ Ẫ Ệ Ộ Ữ Ứ Ằ Ẳ Ự Ị Ọ Ổ ơ ư đ Đ";
        public const string Ranges = "20-7E,A0-FF,100-17F,180-24F,1E00-1EFF,2000-206F,300-36F";

        [MenuItem("Tools/KMA/Setup Vietnamese Fonts")]
        public static void Setup()
        {
            // Validate every source before creating anything; never silently substitute a family.
            string titlePath = Source("SairaCondensed", "Black", "ExtraBold");
            string buttonPath = Source("BarlowSemiCondensed", "Bold", "SemiBold");
            string regularPath = Source("BeVietnamPro", "Regular");
            string boldPath = Source("BeVietnamPro", "Bold");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var title = FontAsset(titlePath, false, true);
            var button = FontAsset(buttonPath, false);
            var regular = FontAsset(regularPath, true);
            var bold = FontAsset(boldPath, true);
            title.fallbackFontAssetTable = new List<TMP_FontAsset> { regular, bold };
            button.fallbackFontAssetTable = new List<TMP_FontAsset> { regular, bold };
            regular.fallbackFontAssetTable = new List<TMP_FontAsset> { bold };
            bold.fallbackFontAssetTable = new List<TMP_FontAsset>();
            foreach (var font in new[] { title, button, regular, bold })
            {
                CheckSample(font);
                EditorUtility.SetDirty(font);
            }

            var library = AssetDatabase.LoadAssetAtPath<VietFontLibrary>("Assets/Resources/VietFontLibrary.asset");
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<VietFontLibrary>();
                AssetDatabase.CreateAsset(library, "Assets/Resources/VietFontLibrary.asset");
            }
            library.title = title; library.buttonHud = button; library.regular = regular; library.bold = bold;
            library.titleMaterial = Preset("Title_KMA", title, new Color32(11, 42, 74, 255), .15f, true);
            library.primaryMaterial = Preset("Button_Primary", button, new Color32(122, 31, 16, 255), .1f);
            library.secondaryMaterial = Preset("Button_Secondary", button, Color.clear, 0f);
            library.bodyMaterial = Preset("Body", regular, Color.clear, 0f);
            library.bodyBoldMaterial = Preset("Body_Bold", bold, Color.clear, 0f);
            EditorUtility.SetDirty(library);

            string gradientPath = Root + "/Materials/Title_KMA_Gradient.asset";
            var gradient = AssetDatabase.LoadAssetAtPath<TMP_ColorGradient>(gradientPath);
            if (gradient == null)
            {
                gradient = ScriptableObject.CreateInstance<TMP_ColorGradient>();
                AssetDatabase.CreateAsset(gradient, gradientPath);
            }
            gradient.colorMode = ColorMode.VerticalGradient;
            gradient.topLeft = gradient.topRight = new Color32(255, 224, 102, 255);
            gradient.bottomLeft = gradient.bottomRight = new Color32(255, 180, 0, 255);
            EditorUtility.SetDirty(gradient);

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/Resources/TMP Settings.asset");
            if (settings == null) throw new InvalidOperationException("Missing Assets/Resources/TMP Settings.asset.");
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
            var fallbacks = serialized.FindProperty("m_fallbackFontAssets");
            fallbacks.arraySize = 2;
            fallbacks.GetArrayElementAtIndex(0).objectReferenceValue = regular;
            fallbacks.GetArrayElementAtIndex(1).objectReferenceValue = bold;
            serialized.FindProperty("m_warningsDisabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Keep the existing UI kit resource in use, with the new button/HUD family.
            var kit = AssetDatabase.LoadAssetAtPath<UiKitAssets>(UiKitAssets.AssetPath);
            var kitSettings = new SerializedObject(kit);
            kitSettings.FindProperty("font").objectReferenceValue = button;
            kitSettings.FindProperty("outlineMaterial").objectReferenceValue = library.primaryMaterial;
            kitSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[VietFont] Setup complete; existing font/material GUIDs retained. Warnings enabled.");
        }

        static string Source(string family, params string[] weights)
        {
            string Compact(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            var files = Directory.Exists("Assets/Fonts") ? Directory.GetFiles("Assets/Fonts", "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)).ToArray() : Array.Empty<string>();
            foreach (string weight in weights)
            {
                var matches = files.Where(p => Compact(Path.GetFileNameWithoutExtension(p)) == Compact(family + weight)).ToArray();
                if (matches.Length > 1) throw new InvalidOperationException("Ambiguous font source: " + string.Join(", ", matches));
                if (matches.Length == 0) continue;
                var bytes = File.ReadAllBytes(matches[0]);
                int count = (bytes[4] << 8) | bytes[5];
                for (int i = 0; i < count; i++)
                    if (System.Text.Encoding.ASCII.GetString(bytes, 12 + i * 16, 4) == "fvar")
                        throw new InvalidOperationException("Variable font is not supported: " + matches[0]);
                if (matches[0].Contains("[wght]")) throw new InvalidOperationException("Variable font: " + matches[0]);
                return matches[0].Replace('\\', '/');
            }
            throw new FileNotFoundException("Install a static font: " + family + "-" + string.Join(" or ", weights) + ".ttf in Assets/Fonts/.");
        }

        static TMP_FontAsset FontAsset(string sourcePath, bool dynamic, bool arrows = false)
        {
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string path = Root + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                if (existing.atlasWidth != 2048 || existing.atlasHeight != 2048 || existing.atlasPadding != 9 ||
                    existing.faceInfo.pointSize != 90 || existing.atlasRenderMode != GlyphRenderMode.SDFAA ||
                    existing.isMultiAtlasTexturesEnabled != dynamic || existing.creationSettings.packingMode != 4 ||
                    existing.creationSettings.sourceFontFileGUID != AssetDatabase.AssetPathToGUID(sourcePath) ||
                    existing.atlasPopulationMode != (dynamic ? AtlasPopulationMode.Dynamic : AtlasPopulationMode.Static))
                    throw new InvalidOperationException("Existing font has incompatible settings; review before replacing: " + path);
                Debug.Log("[VietFont] Reused " + path);
                return existing;
            }
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null) throw new InvalidOperationException("Unity could not import " + sourcePath);
            var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, dynamic);
            font.name = name;
            font.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileGUID = AssetDatabase.AssetPathToGUID(sourcePath),
                pointSizeSamplingMode = 1, pointSize = 90, padding = 9, packingMode = 4,
                atlasWidth = 2048, atlasHeight = 2048, characterSetSelectionMode = 7,
                characterSequence = Ranges, renderMode = (int)GlyphRenderMode.SDFAA
            };
            if (dynamic)
            {
                // Prewarm Vietnamese and common Latin text; retain the TTF for future input.
                font.TryAddCharacters(UnicodeRange().ToArray(), out uint[] missing);
                LogSourceGaps(name, missing);
            }
            else BakeStatic(font, source, arrows);
            font.atlasPopulationMode = dynamic ? AtlasPopulationMode.Dynamic : AtlasPopulationMode.Static;
            font.atlasTextures[0].name = name + " Atlas";
            font.material.name = name + " Material";
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            Debug.Log("[VietFont] Created " + path);
            return font;
        }

        public static IEnumerable<uint> UnicodeRange()
        {
            foreach (string range in Ranges.Split(','))
            {
                string[] ends = range.Split('-');
                for (uint c = Convert.ToUInt32(ends[0], 16); c <= Convert.ToUInt32(ends[1], 16); c++) yield return c;
            }
        }

        static void BakeStatic(TMP_FontAsset font, Font source, bool arrows)
        {
            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(source, 90) != FontEngineError.Success)
                throw new InvalidOperationException("FontEngine failed to load " + source.name);
            var chars = UnicodeRange().Concat(arrows ? new uint[] { 0x2190, 0x2192 } : Array.Empty<uint>()).Distinct().ToArray();
            var glyphs = new Dictionary<uint, Glyph>();
            var characters = new Dictionary<uint, uint>();
            var missing = new List<uint>();
            foreach (uint c in chars)
            {
                if (!FontEngine.TryGetGlyphWithUnicodeValue(c, GlyphLoadFlags.LOAD_NO_HINTING | GlyphLoadFlags.LOAD_NO_BITMAP, out var glyph) || glyph.index == 0)
                { missing.Add(c); continue; }
                uint index = glyph.index;
                if (!glyphs.ContainsKey(index))
                {
                    glyphs.Add(index, glyph);
                }
                characters.Add(c, index);
            }
            var toPack = glyphs.Values.Where(g => g.glyphRect.width > 0 && g.glyphRect.height > 0).ToList();
            var packed = glyphs.Values.Where(g => g.glyphRect.width == 0 || g.glyphRect.height == 0).ToList();
            var free = new List<GlyphRect> { new GlyphRect(0, 0, 2047, 2047) };
            var used = new List<GlyphRect>();
            // Same Optimum mode (4) used by TMP's Font Asset Creator with custom point size.
            FontEngineCall("TryPackGlyphsInAtlas", toPack, packed, 9, (GlyphPackingMode)4,
                GlyphRenderMode.SDFAA, 2048, 2048, free, used);
            if (toPack.Count != 0) throw new InvalidOperationException(source.name + ": atlas overflow at 90 pt; " + toPack.Count + " glyphs did not fit.");
            var pixels = new byte[2048 * 2048];
            var renderable = packed.Where(g => g.glyphRect.width > 0 && g.glyphRect.height > 0).ToList();
            if (Convert.ToInt32(FontEngineCall("RenderGlyphsToTexture", renderable, 9, GlyphRenderMode.SDFAA, pixels, 2048, 2048)) != 0)
                throw new InvalidOperationException("Atlas rendering failed for " + source.name);
            var atlas = font.atlasTextures[0];
            atlas.Reinitialize(2048, 2048, TextureFormat.Alpha8, false);
            atlas.LoadRawTextureData(pixels);
            atlas.Apply(false, false);
            font.glyphTable.Clear(); font.glyphTable.AddRange(packed);
            font.characterTable.Clear();
            foreach (var c in characters) font.characterTable.Add(new TMP_Character(c.Key, font, glyphs[c.Value]));
            font.ReadFontAssetDefinition();
            LogSourceGaps(source.name, missing.ToArray());
        }

        // Unity 6000.3 exposes the exact Optimum packer/buffer renderer only to TMP's
        // friend Editor assembly. Resolve those verified signatures instead of using
        // TryAddCharacters (which hardcodes BestShortSideFit, not Optimum).
        static object FontEngineCall(string methodName, params object[] args)
        {
            var method = typeof(FontEngine).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, args.Select(a => a.GetType()).ToArray(), null);
            if (method == null) throw new NotSupportedException("FontEngine API changed: " + methodName + ". Review for this Unity version.");
            return method.Invoke(null, args);
        }

        static void LogSourceGaps(string name, uint[] missing)
        {
            if (missing == null || missing.Length == 0) return;
            // Requested blocks include code points absent from these font families, including unassigned ones.
            // They are not packing failures; actual project text is checked separately with fallbacks.
            Debug.Log($"[VietFont] {name}: {missing.Length} code points in requested blocks absent from source: " +
                string.Join(" ", missing.Select(c => $"U+{c:X4}")));
        }

        public static void CheckSample(TMP_FontAsset font)
        {
            var missing = Sample.Distinct().Where(c => !font.HasCharacter(c, false, true)).ToArray();
            if (missing.Length != 0)
            {
                string message = "[VietFont] " + font.name + " missing: " + string.Join(", ", missing.Select(c => $"'{c}' U+{(uint)c:X4}"));
                Debug.LogError(message, font);
                throw new InvalidOperationException(message);
            }
            Debug.Log("[VietFont] " + font.name + ": sample coverage PASS (no fallback needed).");
        }

        static Material Preset(string name, TMP_FontAsset font, Color outline, float width, bool shadow = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null;
            if (created) material = new Material(font.material);
            else material.CopyPropertiesFromMaterial(font.material);
            material.name = name;
            material.shader = Shader.Find("TextMeshPro/Distance Field");
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            material.SetColor(ShaderUtilities.ID_OutlineColor, outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            if (width > 0) material.EnableKeyword("OUTLINE_ON"); else material.DisableKeyword("OUTLINE_ON");
            if (shadow)
            {
                material.EnableKeyword("UNDERLAY_ON");
                material.SetColor(ShaderUtilities.ID_UnderlayColor, outline);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, .5f);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -.5f);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            }
            else material.DisableKeyword("UNDERLAY_ON");
            if (created) AssetDatabase.CreateAsset(material, path);
            EditorUtility.SetDirty(material);
            Debug.Log("[VietFont] " + (created ? "Created " : "Updated ") + path);
            return material;
        }
    }
}
