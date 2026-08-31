using System.Reflection;
using KMA.Gameplay;
using KMA.Input;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Running
{
    public sealed class EnduranceRuntimeInputTests
    {
        [Test]
        public void RuntimeBridge_ExposesDetectorSubscriptionSeamWithoutAndroidInput()
        {
            var method = typeof(EnduranceInputBridge).GetMethod(
                "ConfigureDetectorsForTest", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null,
                "Endurance runtime input must be testable through detector events without an Android device.");
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(4));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(EnduranceController)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(RhythmBeatInputDetector)));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(HoldInputDetector)));
            Assert.That(parameters[3].ParameterType, Is.EqualTo(typeof(SwipeInputDetector)));
        }
    }
}
