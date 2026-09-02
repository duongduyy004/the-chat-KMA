using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace KMA.Tests.Config
{
    public sealed class PackageManifestTests
    {
        const string ManifestPath = "Packages/manifest.json";
        const string LockPath = "Packages/packages-lock.json";

        [TestCase("com.unity.render-pipelines.universal", "17.3.0")]
        [TestCase("com.unity.ugui", "2.0.0")]
        [TestCase("com.unity.2d.sprite", "1.0.0")]
        public void RequiredPackageIsPinnedInManifestAndLock(string packageName, string expectedVersion)
        {
            Assert.That(ReadManifestVersion(packageName), Is.EqualTo(expectedVersion),
                $"{packageName} must be pinned in {ManifestPath}.");
            Assert.That(ReadLockVersion(packageName), Is.EqualTo(expectedVersion),
                $"{packageName} must resolve to the pinned version in {LockPath}.");
        }

        [Test]
        public void MultiplayerCenterIsAbsentFromManifestAndLock()
        {
            Assert.That(File.ReadAllText(ManifestPath), Does.Not.Contain("com.unity.multiplayer.center"));
            Assert.That(File.ReadAllText(LockPath), Does.Not.Contain("com.unity.multiplayer.center"));
        }

        static string ReadManifestVersion(string packageName)
        {
            var pattern = $"\"{Regex.Escape(packageName)}\"\\s*:\\s*\"([^\"]+)\"";
            var match = Regex.Match(File.ReadAllText(ManifestPath), pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        static string ReadLockVersion(string packageName)
        {
            var pattern = $"\"{Regex.Escape(packageName)}\"\\s*:\\s*\\{{\\s*\"version\"\\s*:\\s*\"([^\"]+)\"";
            var match = Regex.Match(File.ReadAllText(LockPath), pattern);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
