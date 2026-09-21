using System.Collections.Generic;
using System.Reflection;
using KMA.EditorTools;
using NUnit.Framework;

namespace KMA.Tests.EditorTools
{
    public sealed class AndroidBuildMatrixTests
    {
        [Test]
        public void ParseArchitectures_WithoutRequest_DefaultsToArm64Only()
        {
            Assert.That(Parse(null), Is.EqualTo(new[] { "arm64" }));
        }

        [TestCase("x86_64", "x86_64")]
        [TestCase("all", "arm64", "x86_64")]
        [TestCase("arm64,x86_64", "arm64", "x86_64")]
        public void ParseArchitectures_ExplicitRequest_PreservesRequestedModes(
            string requested, params string[] expected)
        {
            Assert.That(Parse(requested), Is.EqualTo(expected));
        }

        static IReadOnlyList<string> Parse(string requested)
        {
            var method = typeof(AndroidBuildMatrix).GetMethod(
                "ParseArchitectures", BindingFlags.Static | BindingFlags.NonPublic);
            return (IReadOnlyList<string>)method.Invoke(null, new object[] { requested });
        }
    }
}
