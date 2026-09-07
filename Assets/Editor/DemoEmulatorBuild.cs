using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KMA.EditorTools
{
    /// <summary>Explicit emulator build. Shipping Android architecture remains ARM64.</summary>
    public static class DemoEmulatorBuild
    {
        public static void Build()
        {
            var previous = PlayerSettings.Android.targetArchitectures;
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
                Directory.CreateDirectory("Builds/Android");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    target = BuildTarget.Android,
                    locationPathName = "Builds/Android/kma-report-emulator.apk",
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException("Emulator APK build failed: " + report.summary.result);
                UnityEngine.Debug.Log("[KMA] Emulator APK succeeded: " + report.summary.totalSize + " bytes");
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = previous;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
