using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class TrajectoryPreviewVisibilityTests
    {
        // Catches removing the automatic state invalidation that must observe BallRig.Launch
        // after a caller has made the preview visible, without another preview API call.
        [UnityTest]
        public IEnumerator VisiblePreview_HidesOnNextFrameAfterSourceLaunchWithoutPreviewCall()
        {
            PreviewFixture fixture = PreviewFixture.Create();
            fixture.Preview.Refresh(new Vector2(1f, 1f), 5f, 0f);
            fixture.Preview.SetVisible(true);
            Assert.That(fixture.Line.enabled, Is.True);

            fixture.Rig.Launch(Vector2.right, 5f, 0f);
            yield return null;

            Assert.That(fixture.Line.enabled, Is.False);
            Assert.That(fixture.Line.positionCount, Is.Zero);
            fixture.Dispose();
        }

        // Catches removing the automatic state invalidation that must hide a visible preview
        // when its independently-owned BallRig source is destroyed without another preview API call.
        [UnityTest]
        public IEnumerator VisiblePreview_HidesOnNextFrameAfterSourceDestructionWithoutPreviewCall()
        {
            PreviewFixture fixture = PreviewFixture.Create();
            fixture.Preview.Refresh(new Vector2(1f, 1f), 5f, 0f);
            fixture.Preview.SetVisible(true);
            Assert.That(fixture.Line.enabled, Is.True);

            Object.Destroy(fixture.SourceRoot);
            yield return null;

            Assert.That(fixture.Line.enabled, Is.False);
            Assert.That(fixture.Line.positionCount, Is.Zero);
            fixture.DisposePreviewAndHand();
        }

        sealed class PreviewFixture
        {
            public GameObject SourceRoot { get; private set; }
            public GameObject PreviewRoot { get; private set; }
            public GameObject Hand { get; private set; }
            public BallRig Rig { get; private set; }
            public LineRenderer Line { get; private set; }
            public TrajectoryPreview Preview { get; private set; }

            public static PreviewFixture Create()
            {
                var fixture = new PreviewFixture
                {
                    SourceRoot = new GameObject("PreviewSource"),
                    PreviewRoot = new GameObject("PreviewRoot"),
                    Hand = new GameObject("PreviewHand")
                };
                fixture.SourceRoot.SetActive(false);
                fixture.PreviewRoot.SetActive(false);
                fixture.Hand.transform.position = Vector3.up * 2f;
                fixture.SourceRoot.AddComponent<Rigidbody2D>();
                fixture.Rig = fixture.SourceRoot.AddComponent<BallRig>();
                var lineObject = new GameObject("PreviewLine");
                lineObject.transform.SetParent(fixture.PreviewRoot.transform, false);
                fixture.Line = lineObject.AddComponent<LineRenderer>();
                fixture.Preview = fixture.PreviewRoot.AddComponent<TrajectoryPreview>();
                fixture.Preview.Configure(fixture.Rig, fixture.Line, 16, .04f);
                fixture.SourceRoot.SetActive(true);
                fixture.Rig.AttachTo(fixture.Hand.transform);
                fixture.PreviewRoot.SetActive(true);
                return fixture;
            }

            public void Dispose()
            {
                Object.Destroy(SourceRoot);
                DisposePreviewAndHand();
            }

            public void DisposePreviewAndHand()
            {
                Object.Destroy(PreviewRoot);
                Object.Destroy(Hand);
            }
        }
    }
}
