using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class FlightProfileTests
    {
        const string ProfileRoot = "Assets/_Project/ScriptableObjects/Ball/";

        [TestCase("Volleyball", 1.00f, .05f, 0f, .75f)]
        [TestCase("Basketball", 1.00f, .02f, 0f, .80f)]
        [TestCase("PingPong", .85f, .08f, 0f, .65f)]
        [TestCase("Shuttle", .90f, 4.00f, 0f, 0f)]
        [TestCase("Football", 1.10f, .03f, 0f, .60f)]
        public void AuthoredProfile_HasExactApprovedValues(
            string sport,
            float gravityScale,
            float linearDrag,
            float groundY,
            float bounceDamping)
        {
            string path = $"{ProfileRoot}FlightProfile_{sport}.asset";
            FlightProfile profile = AssetDatabase.LoadAssetAtPath<FlightProfile>(path);

            Assert.That(profile, Is.Not.Null, $"missing authored profile at {path}");
            Assert.That(profile.GravityScale, Is.EqualTo(gravityScale).Within(.0001f));
            Assert.That(profile.LinearDrag, Is.EqualTo(linearDrag).Within(.0001f));
            Assert.That(profile.GroundY, Is.EqualTo(groundY).Within(.0001f));
            Assert.That(profile.BounceDamping, Is.EqualTo(bounceDamping).Within(.0001f));
            Assert.That(IsFinite(profile.GravityScale), Is.True);
            Assert.That(IsFinite(profile.LinearDrag), Is.True);
            Assert.That(IsFinite(profile.GroundY), Is.True);
            Assert.That(IsFinite(profile.BounceDamping), Is.True);
            Assert.That(profile.LinearDrag, Is.GreaterThanOrEqualTo(0f));
            Assert.That(profile.BounceDamping, Is.InRange(0f, 1f));
        }

        [Test]
        public void ShuttleProfile_HasHigherDragThanVolleyballAndNoBounce()
        {
            FlightProfile shuttle = Load("Shuttle");
            FlightProfile volleyball = Load("Volleyball");

            Assert.That(shuttle, Is.Not.Null);
            Assert.That(volleyball, Is.Not.Null);
            Assert.That(shuttle.LinearDrag, Is.GreaterThan(volleyball.LinearDrag));
            Assert.That(shuttle.BounceDamping, Is.Zero);
        }

        static FlightProfile Load(string sport)
        {
            return AssetDatabase.LoadAssetAtPath<FlightProfile>($"{ProfileRoot}FlightProfile_{sport}.asset");
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
