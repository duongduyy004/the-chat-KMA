using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KMA.EditorTools
{
    /// <summary>
    /// Headless Android build across one or more ABIs in a single Editor session.
    /// ARM64 remains the project's shipping architecture: the run restores
    /// <see cref="PlayerSettings.Android.targetArchitectures"/> and writes ProjectSettings.asset
    /// back, so a batch build cannot leave X86_64 as the committed default.
    /// </summary>
    public static class AndroidBuildMatrix
    {
        const string DefaultOutputDirectory = "Builds/Android";
        const string DefaultBaseName = "kma";
        const string DefaultAbiList = "arm64,x86_64";

        public static void Build()
        {
            var outputDirectory = ReadArgument("-buildOutputDir") ?? DefaultOutputDirectory;
            var baseName = ReadArgument("-buildName") ?? DefaultBaseName;
            var architectures = ParseArchitectures(ReadArgument("-androidAbi"));

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("[KMA] EditorBuildSettings has no enabled scenes.");

            Directory.CreateDirectory(outputDirectory);

            var previous = PlayerSettings.Android.targetArchitectures;
            var failures = new List<string>();
            try
            {
                foreach (var abi in architectures)
                {
                    var outputPath = Path.Combine(outputDirectory, $"{baseName}-{abi}.apk");
                    PlayerSettings.Android.targetArchitectures = ToArchitecture(abi);

                    var summary = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = scenes,
                        locationPathName = outputPath,
                        target = BuildTarget.Android,
                        targetGroup = BuildTargetGroup.Android,
                        options = BuildOptions.None
                    }).summary;

                    UnityEngine.Debug.Log(
                        $"[KMA] {abi} build {summary.result} -> {outputPath}, {summary.totalSize} bytes, " +
                        $"{summary.totalErrors} errors, {summary.totalWarnings} warnings.");
                    if (summary.result != BuildResult.Succeeded)
                        failures.Add($"{abi}: {summary.result}");
                }
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = previous;
                AssetDatabase.SaveAssets();
                // SaveAssets does not write ProjectSettings.asset; without this a batch run would
                // quit leaving the last built architecture as the project's shipping default.
                EditorApplication.ExecuteMenuItem("File/Save Project");
            }

            if (failures.Count > 0)
                throw new BuildFailedException("[KMA] Android build failed for " + string.Join(", ", failures));
        }

        /// <summary>Normalises the -androidAbi argument into an ordered, de-duplicated ABI list.</summary>
        internal static IReadOnlyList<string> ParseArchitectures(string requested)
        {
            if (string.IsNullOrWhiteSpace(requested))
                requested = DefaultAbiList;
            if (string.Equals(requested.Trim(), "all", StringComparison.OrdinalIgnoreCase))
                requested = DefaultAbiList;

            var result = new List<string>();
            foreach (var token in requested.Split(new[] { ',', ';', '+', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var abi = Normalise(token);
                if (!result.Contains(abi))
                    result.Add(abi);
            }
            if (result.Count == 0)
                throw new BuildFailedException($"[KMA] -androidAbi '{requested}' selected no architecture.");

            // ARM64 first so a partial failure still leaves the shipping APK on disk.
            return result.OrderBy(abi => abi == "arm64" ? 0 : 1).ToList();
        }

        static string Normalise(string token)
        {
            switch (token.Trim().ToLowerInvariant())
            {
                case "arm64":
                case "arm64-v8a":
                case "armv8":
                    return "arm64";
                case "x86_64":
                case "x86-64":
                case "x64":
                    return "x86_64";
                default:
                    throw new BuildFailedException(
                        $"[KMA] Unsupported Android ABI '{token}'. Use arm64, x86_64 or all.");
            }
        }

        static AndroidArchitecture ToArchitecture(string abi)
        {
            return abi == "arm64" ? AndroidArchitecture.ARM64 : AndroidArchitecture.X86_64;
        }

        static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < args.Length; index++)
            {
                if (string.Equals(args[index], name, StringComparison.Ordinal))
                    return args[index + 1];
            }
            return null;
        }
    }
}
