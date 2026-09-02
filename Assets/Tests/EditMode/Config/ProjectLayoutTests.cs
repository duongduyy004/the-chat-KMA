using System;
using System.Linq;
using System.Reflection;
using KMA.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace KMA.Tests.Config
{
    public sealed class ProjectLayoutTests
    {
        [Test]
        public void AudioDspBufferUsesBestLatencySize()
        {
            Assert.That(AudioSettings.GetConfiguration().dspBufferSize, Is.EqualTo(256));
        }

        [TestCase("KMA.EditorTools.ProjectConfigurator", "Apply")]
        [TestCase("KMA.EditorTools.UrpBootstrap", "CreateOrRepair")]
        [TestCase("KMA.EditorTools.BuildScript", "BuildAndroid")]
        public void HeadlessEntryPointIsPublicAndStatic(string typeName, string methodName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, $"Missing editor utility {typeName}.");

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Missing public static entry point {typeName}.{methodName}.");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(method.GetParameters(), Is.Empty);
        }

        [Test]
        public void AndroidBuildWithoutArchitectureOverrideForcesArm64()
        {
            var originalArchitecture = PlayerSettings.Android.targetArchitectures;
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;

                InvokeArchitecturePolicy(null);

                Assert.That(PlayerSettings.Android.targetArchitectures,
                    Is.EqualTo(AndroidArchitecture.ARM64));
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = originalArchitecture;
            }
        }

        [Test]
        public void X86ArchitectureOverrideIsRejectedBeforeBuild()
        {
            var originalArchitecture = PlayerSettings.Android.targetArchitectures;
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                var exception = Assert.Throws<TargetInvocationException>(
                    () => InvokeArchitecturePolicy("x86_64"));

                Assert.That(exception.InnerException, Is.TypeOf<BuildFailedException>());
                Assert.That(PlayerSettings.Android.targetArchitectures,
                    Is.EqualTo(AndroidArchitecture.ARM64));
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = originalArchitecture;
            }
        }

        static void InvokeArchitecturePolicy(string requestedArchitecture)
        {
            var method = typeof(BuildScript).GetMethod(
                "ConfigureAndroidArchitecture",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null,
                "BuildScript needs a testable architecture-policy boundary before BuildPipeline.BuildPlayer.");
            method.Invoke(null, new object[] { requestedArchitecture });
        }
    }
}
