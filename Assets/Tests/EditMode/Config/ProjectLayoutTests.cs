using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
    }
}
