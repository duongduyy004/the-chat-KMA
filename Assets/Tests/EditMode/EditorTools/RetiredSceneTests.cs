using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.EditorTools
{
    public sealed class RetiredSceneTests
    {
        [Test]
        public void PunishmentSceneIsGoneFromTheProjectAndTheBuild()
        {
            Assert.That(System.IO.File.Exists("Assets/_Project/Scenes/Punishment.unity"), Is.False);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path.EndsWith("/Punishment.unity")), Is.False);
        }
    }
}
