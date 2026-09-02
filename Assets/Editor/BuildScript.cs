using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KMA.EditorTools
{
    public static class BuildScript
    {
        const string DefaultOutputPath = "Builds/Android/kma.apk";

        public static void BuildAndroid()
        {
            var outputPath = ReadArgument("-buildOutput") ?? DefaultOutputPath;
            var architecture = ReadArgument("-androidArchitecture");
            ConfigureAndroidArchitecture(architecture);
            var fullPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("[KMA] EditorBuildSettings has no enabled scenes.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var summary = BuildPipeline.BuildPlayer(options).summary;
            UnityEngine.Debug.Log($"[KMA] Build {summary.result}, {summary.totalSize} bytes, " +
                                  $"{summary.totalErrors} errors, {summary.totalWarnings} warnings.");
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException(summary.ToString());
        }

        internal static void ConfigureAndroidArchitecture(string requestedArchitecture)
        {
            if (!string.IsNullOrEmpty(requestedArchitecture) &&
                !string.Equals(requestedArchitecture, "arm64", StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"[KMA] Unsupported Android architecture '{requestedArchitecture}'. ARM64 is required.");
            }

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
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
