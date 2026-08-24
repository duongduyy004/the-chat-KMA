using System.Collections;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Ball
{
    public sealed class BallRigTests
    {
        [UnityTest]
        public IEnumerator AttachTo_MakesBodyKinematicAndTracksHand()
        {
            var hand = new GameObject("Hand").transform;
            hand.position = new Vector3(2f, 3f);
            var rig = BallTestFactory.Create();

            rig.AttachTo(hand);
            yield return new WaitForFixedUpdate();
            Assert.That(rig.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(rig.transform.position, Is.EqualTo(hand.position));
            Assert.That(rig.Snapshot.IsAttached, Is.True);

            Object.Destroy(hand.gameObject);
            Object.Destroy(rig.gameObject);
        }

        [Test]
        public void IsNearApex_UsesAbsoluteVerticalVelocity()
        {
            var rig = BallTestFactory.Create();
            rig.Body.velocity = new Vector2(4f, -.09f);

            Assert.That(rig.IsNearApex(.1f), Is.True);

            Object.DestroyImmediate(rig.gameObject);
        }

        [Test]
        public void PredictGround_IsDeterministicForSameInputs()
        {
            var position = new Vector2(1.5f, 4f);
            var velocity = new Vector2(3f, 5f);

            var first = Ballistics.PredictGround(position, velocity, -9.81f, 0f);
            var second = Ballistics.PredictGround(position, velocity, -9.81f, 0f);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.x, Is.EqualTo(6.14f).Within(.01f));
            Assert.That(first.y, Is.Zero);
        }

        [Test]
        public void PredictGround_ReturnsCurrentPositionWhenNoLandingRootExists()
        {
            var position = new Vector2(2f, 4f);

            Assert.That(Ballistics.PredictGround(position, Vector2.zero, -9.81f, 10f), Is.EqualTo(position));
            Assert.That(Ballistics.PredictGround(position, new Vector2(3f, 2f), 0f, 0f), Is.EqualTo(position));
        }

        [UnityTest]
        public IEnumerator Launch_DetachesAndProducesFlightSnapshot()
        {
            var hand = new GameObject("Hand").transform;
            var rig = BallTestFactory.Create();
            rig.AttachTo(hand);

            rig.Launch(Vector2.right, 6f, .25f);
            yield return new WaitForFixedUpdate();
            Assert.That(rig.Body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(rig.Snapshot.IsAttached, Is.False);
            Assert.That(rig.Snapshot.IsInFlight, Is.True);
            Assert.That(rig.Snapshot.Velocity.x, Is.GreaterThan(0f));

            Object.Destroy(hand.gameObject);
            Object.Destroy(rig.gameObject);
        }

        [Test]
        public void Bounce_ReflectsVelocityAndRaisesCollisionEvent()
        {
            var rig = BallTestFactory.Create();
            var observed = false;
            rig.Collided += collision => observed = collision != null;

            var incoming = new Vector2(2f, -4f);
            var reflected = rig.Bounce(incoming, Vector2.up);

            Assert.That(reflected, Is.EqualTo(new Vector2(2f, 4f)));
            Assert.That(observed, Is.False, "A synthetic bounce must not pretend to be a Unity collision event.");

            Object.DestroyImmediate(rig.gameObject);
        }

        static class BallTestFactory
        {
            public static BallRig Create()
            {
                var go = new GameObject("BallRigTest");
                go.AddComponent<Rigidbody2D>();
                return go.AddComponent<BallRig>();
            }
        }
    }
}
