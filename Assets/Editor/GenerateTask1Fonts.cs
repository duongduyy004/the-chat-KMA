using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

internal static class GenerateTask1Fonts
{
    private const string FontDirectory = "Assets/_Project/Fonts";
    private const int AtlasSize = 4096;

    public static void Run()
    {
        EnsureTmpSettings();
        AssetDatabase.Refresh();
        var characters = BuildCharacterSource();
        var baloo = Create("Baloo2-ExtraBold", characters, true);
        var nunito = Create("Nunito-Bold", characters, true);
        var fallback = Create("VietnameseFallback", RequiredCharacters(), "Baloo2-ExtraBold", false);
        baloo.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        nunito.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        EditorUtility.SetDirty(baloo);
        EditorUtility.SetDirty(nunito);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated Task 1 font assets with {characters.Length} source characters.");
    }

    private static void EnsureTmpSettings()
    {
        const string settingsPath = "Assets/Resources/TMP Settings.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath) == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMP_Settings>(), settingsPath);
            AssetDatabase.SaveAssets();
        }
    }

    private static TMP_FontAsset Create(string assetName, string characters, bool freezeStatic)
    {
        var assetPath = $"{FontDirectory}/{assetName}.asset";
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>($"{FontDirectory}/{assetName}.ttf");
        if (sourceFont == null)
            throw new FileNotFoundException("Font source was not imported", assetName);
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = assetName;
        if (!fontAsset.TryAddCharacters(characters, out var missingCharacters))
            Debug.LogWarning($"{assetName} missing {missingCharacters.Length} requested characters; fallback coverage is serialized.");
        if (freezeStatic)
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        return fontAsset;
    }

    private static TMP_FontAsset Create(string assetName, string characters, string sourceAssetName, bool freezeStatic)
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>($"{FontDirectory}/{sourceAssetName}.ttf");
        if (sourceFont == null)
            throw new FileNotFoundException("Font source was not imported", sourceAssetName);
        var assetPath = $"{FontDirectory}/{assetName}.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA,
            AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = assetName;
        if (!fontAsset.TryAddCharacters(characters, out var missingCharacters))
            Debug.LogWarning($"{assetName} missing {missingCharacters.Length} requested characters; dynamic fallback remains enabled.");
        if (freezeStatic)
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        return fontAsset;
    }

    private static string RequiredCharacters()
    {
        var source = new StringBuilder();
        AppendRange(source, 0x1EA0, 0x1EF9);
        source.Append('\u0110').Append('\u0111');
        AppendRange(source, 0x01A0, 0x01B0);
        return source.ToString();
    }

    private static string BuildCharacterSource()
    {
        var source = new StringBuilder();
        var stableTextExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".txt", ".json", ".uxml", ".uss"
        };
        foreach (var path in Directory.GetFiles("Assets/_Project", "*", SearchOption.AllDirectories)
            .Where(path => stableTextExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => path, System.StringComparer.Ordinal))
        {
            if (path.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                continue;
            try { source.Append(File.ReadAllText(path)); }
            catch (IOException) { }
        }
        for (var codePoint = 0x20; codePoint <= 0x7E; codePoint++) source.Append((char)codePoint);
        AppendRange(source, 0x1EA0, 0x1EF9);
        source.Append('\u0110').Append('\u0111');
        AppendRange(source, 0x01A0, 0x01B0);
        return new string(source.ToString().Distinct().ToArray());
    }

    private static void AppendRange(StringBuilder source, int first, int last)
    {
        for (var codePoint = first; codePoint <= last; codePoint++) source.Append((char)codePoint);
    }
}
