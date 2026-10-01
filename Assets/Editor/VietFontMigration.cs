using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KMA.Gameplay.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public static class VietFontMigration
    {
        const string LegacyGuid = "5f7201a12d95ffc409449d95f23cf332";
        const string TmpGuid = "f4688fdb7df04437aeb418b961361dc5";

        [MenuItem("Tools/KMA/Apply Vietnamese Fonts to Project")]
        public static void ApplyProject()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before migrating fonts.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VietFontSetup.Setup();
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            int converted = 0, worldConverted = 0, scenes = 0, prefabs = 0;
            try
            {
                // Only change the component payload; keep its fileID, GameObject and RectTransform.
                var files = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
                    .OrderBy(p => p).ToArray();
                foreach (string rawPath in files)
                {
                    string path = rawPath.Replace('\\', '/');
                    converted += ConvertLegacyInPlace(path);
                    worldConverted += ConvertWorldTextInPlace(path);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (string rawPath in files.Where(p => p.EndsWith(".prefab")))
                {
                    string path = rawPath.Replace('\\', '/');
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        if (Style(root) == 0) continue;
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        prefabs++;
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
                foreach (string rawPath in files.Where(p => p.EndsWith(".unity")))
                {
                    string path = rawPath.Replace('\\', '/');
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int textCount = scene.GetRootGameObjects().Sum(Style);
                    if (textCount == 0) continue;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    scenes++;
                }
                AssetDatabase.SaveAssets();
            }
            finally { RestoreScenes(sceneSetup); }
            Debug.Log($"[VietFont] Migration: {converted} legacy Text and {worldConverted} TextMesh converted, {scenes} text-bearing scenes, {prefabs} text-bearing prefabs processed.");
        }

        internal static void RestoreScenes(SceneSetup[] setup)
        {
            if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static int Style(GameObject root)
        {
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                VietTypography.Apply(text);
                // TMP SDF materials own text outline/shadow now. Mesh duplication by uGUI
                // effects expands stacked Vietnamese marks and can cause material artifacts.
                foreach (var effect in text.GetComponents<Shadow>()) UnityEngine.Object.DestroyImmediate(effect);
                EditorUtility.SetDirty(text);
            }
            foreach (var field in root.GetComponentsInChildren<TMP_InputField>(true))
                if (field.GetComponent<VietInputNormalizer>() == null) field.gameObject.AddComponent<VietInputNormalizer>();
            return texts.Length;
        }

        public static int ConvertLegacyInPlace(string path)
        {
            string source = File.ReadAllText(path);
            int count = 0;
            string result = Regex.Replace(source, @"^--- !u!114 &[^\n]+\n(?:(?!^--- !u!).)*", match =>
            {
                string block = match.Value;
                if (!block.Contains("guid: " + LegacyGuid)) return block;
                string Value(string name, string fallback) => Regex.Match(block, @"^\s+" + name + @": (.*)$", RegexOptions.Multiline)
                    is var value && value.Success ? value.Groups[1].Value.TrimEnd('\r') : fallback;
                int alignment = int.Parse(Value("m_Alignment", "4"));
                int[] horizontal = { 1, 2, 4 };
                int[] vertical = { 256, 512, 1024 };
                string fontData = Regex.Match(block, @"^  m_FontData:\n(?:    .*\n)+", RegexOptions.Multiline).Value;
                if (fontData.Length == 0) throw new InvalidDataException("Missing legacy FontData in " + path);
                string fields = "  m_text: " + Value("m_Text", "") + "\n" +
                    "  m_fontColor: " + Value("m_Color", "{r: 1, g: 1, b: 1, a: 1}") + "\n" +
                    "  m_fontSize: " + Value("m_FontSize", "14") + "\n" +
                    "  m_fontSizeBase: " + Value("m_FontSize", "14") + "\n" +
                    "  m_fontStyle: " + Value("m_FontStyle", "0") + "\n" +
                    "  m_enableAutoSizing: " + Value("m_BestFit", "0") + "\n" +
                    "  m_fontSizeMin: " + Value("m_MinSize", "10") + "\n" +
                    "  m_fontSizeMax: " + Value("m_MaxSize", "40") + "\n" +
                    "  m_HorizontalAlignment: " + horizontal[alignment % 3] + "\n" +
                    "  m_VerticalAlignment: " + vertical[alignment / 3] + "\n" +
                    "  m_isRichText: " + Value("m_RichText", "1") + "\n" +
                    "  m_TextWrappingMode: " + (Value("m_HorizontalOverflow", "0") == "0" ? "1" : "0") + "\n" +
                    "  m_overflowMode: 0\n  m_enableExtraPadding: 1\n  m_lineSpacing: 15\n";
                // Keep multiline text values intact, including Unity's quoted YAML escapes.
                string content = Regex.Match(block, @"^  m_Text:(.*(?:\n(?!  \w+:|---)[^\n]*)*)", RegexOptions.Multiline).Groups[1].Value;
                if (content.Contains('\n')) fields = Regex.Replace(fields, @"^  m_text:.*$", "  m_text:" + content, RegexOptions.Multiline);
                block = block.Replace("guid: " + LegacyGuid, "guid: " + TmpGuid);
                block = Regex.Replace(block, @"^  m_EditorClassIdentifier:.*$", "  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI", RegexOptions.Multiline);
                block = block.Replace(fontData, "");
                block = Regex.Replace(block, @"^  m_Text:.*(?:\n(?!  \w+:|---)[^\n]*)*", "", RegexOptions.Multiline);
                count++;
                return block.TrimEnd() + "\n" + fields;
            }, RegexOptions.Multiline | RegexOptions.Singleline);
            if (count > 0) File.WriteAllText(path, result);
            return count;
        }

        static int ConvertWorldTextInPlace(string path)
        {
            string source = File.ReadAllText(path);
            var gameObjects = new System.Collections.Generic.HashSet<string>();
            int count = 0;
            string result = Regex.Replace(source, @"^--- !u!102 &[^\n]+\n(?:(?!^--- !u!).)*", match =>
            {
                string block = match.Value;
                string Value(string name, string fallback) => Regex.Match(block, @"^  " + name + @": (.*)$", RegexOptions.Multiline)
                    is var value && value.Success ? value.Groups[1].Value : fallback;
                string fileId = Regex.Match(block, @"&(-?\d+)").Groups[1].Value;
                string gameObject = Value("m_GameObject", "{fileID: 0}");
                gameObjects.Add(gameObject);
                uint rgba = uint.Parse(Regex.Match(block, @"^    rgba: (\d+)", RegexOptions.Multiline).Groups[1].Value);
                string Channel(int shift) => ((rgba >> shift & 255) / 255f).ToString(System.Globalization.CultureInfo.InvariantCulture);
                string color = "{r: " + Channel(0) + ", g: " + Channel(8) + ", b: " + Channel(16) + ", a: " + Channel(24) + "}";
                string renderer = Regex.Matches(source, @"^--- !u!23 &[^\n]+\n(?:(?!^--- !u!).)*", RegexOptions.Multiline | RegexOptions.Singleline)
                    .Cast<Match>().Select(m => m.Value).FirstOrDefault(b => b.Contains("  m_GameObject: " + gameObject));
                string RendererValue(string key) => renderer == null ? "0" : Regex.Match(renderer, @"^  " + key + @": (.*)$", RegexOptions.Multiline).Groups[1].Value;
                float size = float.Parse(Value("m_FontSize", "48"), System.Globalization.CultureInfo.InvariantCulture) *
                    float.Parse(Value("m_CharacterSize", "1"), System.Globalization.CultureInfo.InvariantCulture);
                int anchor = int.Parse(Value("m_Anchor", "4"));
                int[] horizontal = { 1, 2, 4 }; int[] vertical = { 256, 512, 1024 };
                count++;
                return "--- !u!114 &" + fileId + "\nMonoBehaviour:\n" +
                    "  m_ObjectHideFlags: " + Value("m_ObjectHideFlags", "0") + "\n" +
                    "  m_CorrespondingSourceObject: " + Value("m_CorrespondingSourceObject", "{fileID: 0}") + "\n" +
                    "  m_PrefabInstance: " + Value("m_PrefabInstance", "{fileID: 0}") + "\n" +
                    "  m_PrefabAsset: " + Value("m_PrefabAsset", "{fileID: 0}") + "\n" +
                    "  m_GameObject: " + gameObject + "\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n" +
                    "  m_Script: {fileID: 11500000, guid: 9541d86e2fd84c1d9990edf0852d74ab, type: 3}\n" +
                    "  m_Name:\n  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshPro\n" +
                    "  m_text: " + Value("m_Text", "") + "\n  m_fontSize: " + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                    "  m_fontSizeBase: " + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                    "  m_fontColor: " + color + "\n" +
                    "  _SortingOrder: " + RendererValue("m_SortingOrder") + "\n  _SortingLayerID: " + RendererValue("m_SortingLayerID") + "\n" +
                    "  m_HorizontalAlignment: " + horizontal[anchor % 3] + "\n" +
                    "  m_VerticalAlignment: " + vertical[anchor / 3] + "\n" +
                    "  m_TextWrappingMode: 0\n  m_overflowMode: 0\n  m_enableExtraPadding: 1\n  m_lineSpacing: 15\n";
            }, RegexOptions.Multiline | RegexOptions.Singleline);
            if (count == 0) return 0;
            result = Regex.Replace(result, @"^--- !u!4 &[^\n]+\n(?:(?!^--- !u!).)*", match =>
            {
                string block = match.Value;
                var gameObject = Regex.Match(block, @"^  m_GameObject: (.*)$", RegexOptions.Multiline);
                if (!gameObject.Success || !gameObjects.Contains(gameObject.Groups[1].Value)) return block;
                var position = Regex.Match(block, @"^  m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: [^}]+\}", RegexOptions.Multiline);
                if (!position.Success) throw new InvalidDataException("World label position missing in " + path);
                return block.Replace("--- !u!4 &", "--- !u!224 &").Replace("\nTransform:\n", "\nRectTransform:\n").TrimEnd() +
                    "\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n" +
                    "  m_AnchoredPosition: {x: " + position.Groups[1].Value + ", y: " + position.Groups[2].Value + "}\n" +
                    "  m_SizeDelta: {x: 2, y: 0.6}\n  m_Pivot: {x: 0.5, y: 0.5}\n";
            }, RegexOptions.Multiline | RegexOptions.Singleline);
            File.WriteAllText(path, result);
            return count;
        }
    }
}
