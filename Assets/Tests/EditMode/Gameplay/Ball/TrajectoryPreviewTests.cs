using System;
using System.Linq;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class TrajectoryPreviewTests
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab";

        [Test]
        public void SampleLanding_WithCurvatureAndDrag_MatchesAuthoritativeBallistics()
        {
            var position = new Vector2(1.25f, 3.5f);
            var velocity = new Vector2(4.5f, 6.25f);
            var gravity = new Vector2(0f, -9.81f);

            Vector2 expected = Ballistics.PredictGround(position, velocity, gravity, 0f, .15f, .35f, .02f);
            Vector2 actual = TrajectoryPreview.SampleLanding(position, velocity, gravity, 0f, .15f, .35f, .02f);

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(.001f));
        }

        [Test]
        public void Refresh_EndsAtAuthoritativeProspectiveLanding_WithoutMutatingBall()
        {
            var profile = FlightProfile.Create(1f, .12f, 0f, .75f);
            var hand = new GameObject("PreviewHand");
            hand.transform.position = new Vector3(1.5f, 3f, 0f);
            PreviewFixture fixture = PreviewFixture.Create(profile, hand.transform);

            try
            {
                Vector2 direction = new Vector2(1f, .8f);
                const float force = 7.25f;
                const float curvature = .3f;
                Vector2 expected = Ballistics.PredictGround(
                    fixture.Rig.Body.position,
                    direction.normalized * force,
                    Physics2D.gravity * profile.GravityScale,
                    profile.GroundY,
                    profile.LinearDrag,
                    curvature,
                    Time.fixedDeltaTime);
                Vector2 bodyPosition = fixture.Rig.Body.position;
                Vector2 bodyVelocity = fixture.Rig.Body.velocity;
                BallFlightSnapshot snapshot = fixture.Rig.Snapshot;

                Vector2 actual = fixture.Preview.Refresh(direction, force, curvature);
                fixture.Preview.SetVisible(true);

                Assert.That(fixture.Line.enabled, Is.True, "a held ball with meaningful force should show its preview");
                Assert.That(fixture.Line.positionCount, Is.EqualTo(16));
                Vector3 endpoint = fixture.Line.GetPosition(fixture.Line.positionCount - 1);
                Assert.That(endpoint.x, Is.EqualTo(expected.x).Within(.001f));
                Assert.That(endpoint.y, Is.EqualTo(expected.y).Within(.001f));
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(.001f));
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(.001f));
                Assert.That(fixture.Rig.Body.position, Is.EqualTo(bodyPosition));
                Assert.That(fixture.Rig.Body.velocity, Is.EqualTo(bodyVelocity));
                AssertSnapshotUnchanged(snapshot, fixture.Rig.Snapshot);
            }
            finally
            {
                fixture.Dispose();
                UnityEngine.Object.DestroyImmediate(hand);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void RepeatedRefresh_ReusesLineAndChildren_AndLaunchHidesIt()
        {
            var hand = new GameObject("PreviewHand");
            hand.transform.position = Vector3.up * 2f;
            PreviewFixture fixture = PreviewFixture.Create(null, hand.transform);

            try
            {
                int childCount = fixture.Root.transform.childCount;
                int lineId = fixture.Line.GetInstanceID();
                fixture.Preview.SetVisible(true);
                fixture.Preview.Refresh(new Vector2(1f, 1f), 5f, 0f);
                fixture.Preview.Refresh(new Vector2(-1f, 1f), 6f, .2f);

                Assert.That(fixture.Root.transform.childCount, Is.EqualTo(childCount));
                Assert.That(fixture.Line.GetInstanceID(), Is.EqualTo(lineId));

                fixture.Rig.Launch(Vector2.right, 5f, 0f);
                fixture.Preview.Refresh(Vector2.right, 5f, 0f);

                Assert.That(fixture.Line.enabled, Is.False, "launch must hide the held-ball preview");
                Assert.That(fixture.Line.positionCount, Is.Zero);
            }
            finally
            {
                fixture.Dispose();
                UnityEngine.Object.DestroyImmediate(hand);
            }
        }

        [Test]
        public void InvalidConfiguration_DisablesAndLogsOneActionableError()
        {
            var root = new GameObject("InvalidPreview");
            root.SetActive(false);
            var line = root.AddComponent<LineRenderer>();
            var preview = root.AddComponent<TrajectoryPreview>();

            try
            {
                LogAssert.Expect(LogType.Error, "TrajectoryPreview requires a BallRig source, a LineRenderer, sampleCount >= 2, and sampleStep > 0.");
                preview.Configure(null, line, 1, 0f);

                Assert.That(preview.enabled, Is.False);
                Assert.That(line.enabled, Is.False);
                Assert.That(line.positionCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SampleLanding_WithInvalidStep_ReturnsFiniteCurrentPosition()
        {
            var position = new Vector2(2f, 4f);

            Vector2 actual = TrajectoryPreview.SampleLanding(position, Vector2.zero, Physics2D.gravity, 0f, 0f, 0f, 0f);

            Assert.That(actual, Is.EqualTo(position));
            Assert.That(float.IsNaN(actual.x) || float.IsInfinity(actual.x), Is.False);
            Assert.That(float.IsNaN(actual.y) || float.IsInfinity(actual.y), Is.False);
        }

        [TestCase(0f, 0f, 1f, .8f)]
        [TestCase(2f, 0f, .6f, .5f)]
        [TestCase(10f, 0f, .2f, .2f)]
        public void ShadowHeight_ClampsPositionScaleAndAlpha(float targetHeight, float expectedGroundY, float expectedScale, float expectedAlpha)
        {
            var target = new GameObject("ShadowTarget");
            var visual = new GameObject("ShadowVisual");
            target.transform.position = new Vector3(3.25f, targetHeight, 0f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var shadow = target.AddComponent<BallShadow>();

            try
            {
                shadow.Configure(target.transform, visual.transform, renderer, 0f, 4f, .2f, 1f, .2f, .8f);
                Vector3 targetBefore = target.transform.position;

                shadow.Refresh();

                Assert.That(visual.transform.position.x, Is.EqualTo(3.25f).Within(.001f));
                Assert.That(visual.transform.position.y, Is.EqualTo(expectedGroundY).Within(.001f));
                Assert.That(visual.transform.localScale.x, Is.EqualTo(expectedScale).Within(.001f));
                Assert.That(visual.transform.localScale.y, Is.EqualTo(expectedScale).Within(.001f));
                Assert.That(renderer.color.a, Is.EqualTo(expectedAlpha).Within(.001f));
                Assert.That(target.transform.position, Is.EqualTo(targetBefore));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(visual);
            }
        }

        [Test]
        public void BallPresentationPrefab_HasSinglePresentationOwnersAndValidReferences()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(prefab, Is.Not.Null, $"missing reusable presentation prefab at {PrefabPath}");
            Assert.That(prefab.GetComponentsInChildren<Transform>(true)
                .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)), Is.Zero);
            Assert.That(prefab.GetComponentsInChildren<BallRig>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<TrajectoryPreview>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<BallShadow>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<LineRenderer>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(1));

            MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(behaviours.All(value => value is BallRig || value is TrajectoryPreview || value is BallShadow),
                Is.True, "the shared prefab must not own subject input, rules, or controller behavior");

            var preview = new SerializedObject(prefab.GetComponentInChildren<TrajectoryPreview>(true));
            var shadow = new SerializedObject(prefab.GetComponentInChildren<BallShadow>(true));
            var rig = new SerializedObject(prefab.GetComponentInChildren<BallRig>(true));
            LineRenderer line = prefab.GetComponentInChildren<LineRenderer>(true);
            SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(rig.FindProperty("body").objectReferenceValue, Is.Not.Null);
            Assert.That(preview.FindProperty("source").objectReferenceValue, Is.Not.Null);
            Assert.That(preview.FindProperty("line").objectReferenceValue, Is.Not.Null);
            Assert.That(shadow.FindProperty("target").objectReferenceValue, Is.Not.Null);
            Assert.That(shadow.FindProperty("shadow").objectReferenceValue, Is.Not.Null);
            Assert.That(shadow.FindProperty("shadowRenderer").objectReferenceValue, Is.Not.Null);
            Assert.That(line.sharedMaterial, Is.Not.Null);
            Assert.That(line.sharedMaterial.mainTexture, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
        }

        static void AssertSnapshotUnchanged(BallFlightSnapshot before, BallFlightSnapshot after)
        {
            Assert.That(after.Position, Is.EqualTo(before.Position));
            Assert.That(after.Velocity, Is.EqualTo(before.Velocity));
            Assert.That(after.IsAttached, Is.EqualTo(before.IsAttached));
            Assert.That(after.IsInFlight, Is.EqualTo(before.IsInFlight));
            Assert.That(after.Curvature, Is.EqualTo(before.Curvature));
        }

        sealed class PreviewFixture : IDisposable
        {
            public GameObject Root { get; private set; }
            public BallRig Rig { get; private set; }
            public LineRenderer Line { get; private set; }
            public TrajectoryPreview Preview { get; private set; }

            public static PreviewFixture Create(FlightProfile profile, Transform attachment)
            {
                var fixture = new PreviewFixture { Root = new GameObject("PreviewFixture") };
                fixture.Root.SetActive(false);
                fixture.Root.AddComponent<Rigidbody2D>();
                fixture.Rig = fixture.Root.AddComponent<BallRig>();
                if (profile)
                    fixture.Rig.SetProfile(profile);

                var lineObject = new GameObject("PreviewLine");
                lineObject.transform.SetParent(fixture.Root.transform, false);
                fixture.Line = lineObject.AddComponent<LineRenderer>();
                fixture.Preview = fixture.Root.AddComponent<TrajectoryPreview>();
                fixture.Preview.Configure(fixture.Rig, fixture.Line, 16, .04f);
                fixture.Root.SetActive(true);
                fixture.Rig.AttachTo(attachment);
                return fixture;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
            }
        }
    }
}
