using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KMA.Gameplay.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class FontCoverageChecker
    {
        [Serializable]
        public sealed class Report
        {
            public int scenes, prefabs, texts, scriptableObjects, dataFiles, sourceFiles;
            public List<string> missing = new List<string>();
            public List<string> sourceCandidates = new List<string>();
        }

        [MenuItem("Tools/KMA/Check Vietnamese Font Coverage")]
        public static void Run()
        {
            var report = Scan();
            Directory.CreateDirectory("Builds/FontQA");
            File.WriteAllText("Builds/FontQA/coverage.json", JsonUtility.ToJson(report, true));
            Debug.Log($"[VietFont] Coverage: {report.scenes} scenes, {report.prefabs} prefabs, {report.texts} serialized TMP texts, " +
                $"{report.scriptableObjects} ScriptableObjects, {report.dataFiles} data files, {report.sourceFiles} runtime/Editor source files. " +
                $"Missing UI/data characters: {report.missing.Count}; unresolved source candidates: {report.sourceCandidates.Count}.");
            foreach (string issue in report.missing) Debug.LogError("[VietFont] " + issue);
            foreach (string issue in report.sourceCandidates) Debug.Log("[VietFont] Source literal candidate (verify its display font): " + issue);
            if (report.missing.Count != 0) throw new InvalidOperationException("Font coverage failed. See Builds/FontQA/coverage.json.");
        }

        public static Report Scan()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Coverage scan cancelled.");
            var report = new Report();
            var fonts = VietTypography.Library;
            if (fonts == null) throw new InvalidOperationException("Run Vietnamese font setup first.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects()) CheckHierarchy(root, path, report);
                    report.scenes++;
                }
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try { CheckHierarchy(root, path, report); }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    report.prefabs++;
                }
            }
            finally { VietFontMigration.RestoreScenes(previous); }
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || asset is TMP_FontAsset || asset is TMP_Settings || asset is TMP_ColorGradient) continue;
                var property = new SerializedObject(asset).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.String && !property.name.StartsWith("m_"))
                        Check(property.stringValue, fonts.regular, path + ":" + property.propertyPath, report.missing);
                report.scriptableObjects++;
            }
            foreach (string path in Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Where(p => new[] { ".json", ".csv", ".tsv", ".txt" }.Contains(System.IO.Path.GetExtension(p).ToLowerInvariant())))
            {
                string data = File.ReadAllText(path);
                // Decode JSON escapes as well as literal UTF-8 without changing source data.
                if (path.EndsWith(".json"))
                    data = Regex.Replace(data, @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                Check(data, fonts.regular, path, report.missing);
                report.dataFiles++;
            }
            foreach (string path in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories).Where(p => !p.Contains("/Tests/")))
            {
                string source = File.ReadAllText(path);
                // Runtime factories have no serialized component yet. Check their literals,
                // including interpolated text, against the body source or the Saira arrows.
                foreach (Match literal in Regex.Matches(source, "\"(?:\\\\.|[^\"\\\\])*\""))
                {
                    string value;
                    try { value = Regex.Unescape(literal.Value.Substring(1, literal.Length - 2)); }
                    catch (ArgumentException) { continue; }
                    if (!value.Any(c => c >= 128)) continue;
                    var font = value.Contains("←") || value.Contains("→") ? fonts.title : fonts.regular;
                    Check(value, font, path, report.sourceCandidates);
                }
                report.sourceFiles++;
            }
            foreach (var font in new[] { fonts.regular, fonts.bold }) EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return report;
        }

        static void CheckHierarchy(GameObject root, string path, Report report)
        {
            foreach (var legacy in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                report.missing.Add(path + ":" + Hierarchy(legacy.transform) + " still uses legacy Text.");
            foreach (var legacy in root.GetComponentsInChildren<TextMesh>(true))
                report.missing.Add(path + ":" + Hierarchy(legacy.transform) + " still uses legacy TextMesh.");
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Check(text.text, text.font, path + ":" + Hierarchy(text.transform), report.missing);
                report.texts++;
            }
        }

        static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;

        public static void Check(string value, TMP_FontAsset font, string location, List<string> issues)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (font == null) { issues.Add(location + ": no font assigned."); return; }
            value = VietText.Fix(value);
            // Rich text tags do not contribute rendered characters.
            value = Regex.Replace(value, @"<[^>]*>", "");
            var missing = new HashSet<uint>();
            for (int i = 0; i < value.Length; i++)
            {
                uint c = value[i];
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                    c = (uint)char.ConvertToUtf32(value[i], value[++i]);
                if (c < 32 || c == 127 || c == 0xFEFF || c == 0x200B || c == 0x200D) continue;
                if (!font.HasCharacters(char.ConvertFromUtf32((int)c), out uint[] _, true, true)) missing.Add(c);
            }
            if (missing.Count != 0)
                issues.Add(location + " [" + font.name + "]: " + string.Join(", ", missing.OrderBy(c => c)
                    .Select(c => "'" + char.ConvertFromUtf32((int)c) + $"' U+{c:X4}")));
        }
    }
}
