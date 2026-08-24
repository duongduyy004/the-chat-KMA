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

        [UnityTest]
        public IEnumerator PhysicsCollision_EmitsAndReflectsWithDamping()
        {
            var profile = FlightProfile.Create(1f, 0f, 0f, .5f);
            var rig = BallTestFactory.Create(profile, true);
            rig.transform.position = new Vector2(0f, 2f);
            var floor = new GameObject("BallGround");
            var floorCollider = floor.AddComponent<BoxCollider2D>();
            floorCollider.size = new Vector2(10f, .2f);
            floor.transform.position = new Vector2(0f, -.1f);
            var collided = false;
            rig.Collided += collision => collided = collision != null;

            rig.Launch(Vector2.down, 5f, 0f);
            for (var step = 0; step < 30 && !collided; step++)
                yield return new WaitForFixedUpdate();

            Assert.That(collided, Is.True);
            Assert.That(rig.Body.velocity.y, Is.GreaterThan(0f));
            Assert.That(rig.Body.velocity.y, Is.LessThan(5f));

            Object.Destroy(floor);
            Object.Destroy(rig.gameObject);
            Object.Destroy(profile);
        }

        [UnityTest]
        public IEnumerator Launch_UsesConfiguredGravityDragCurvatureAcrossFixedSteps()
        {
            var profile = FlightProfile.Create(1f, .5f, -100f, 1f);
            var rig = BallTestFactory.Create(profile, false);
            rig.Body.position = new Vector2(0f, 10f);
            rig.Launch(Vector2.right, 4f, .75f);
            var expectedVelocity = new Vector2(4f, 0f);
            var expectedPosition = rig.Body.position;
            var deltaTime = Time.fixedDeltaTime;

            for (var step = 0; step < 3; step++)
            {
                expectedVelocity = Ballistics.AdvanceVelocity(expectedVelocity, Physics2D.gravity, .75f, .5f, deltaTime);
                expectedPosition += expectedVelocity * deltaTime;
                yield return new WaitForFixedUpdate();
                Assert.That(rig.Body.velocity.x, Is.EqualTo(expectedVelocity.x).Within(.001f));
                Assert.That(rig.Body.velocity.y, Is.EqualTo(expectedVelocity.y).Within(.001f));
                Assert.That(rig.Body.position.x, Is.EqualTo(expectedPosition.x).Within(.001f));
                Assert.That(rig.Body.position.y, Is.EqualTo(expectedPosition.y).Within(.001f));
            }

            var predicted = rig.PredictLandingPoint();
            var direct = Ballistics.PredictGround(rig.Body.position, rig.Body.velocity, Physics2D.gravity, -100f, .5f, .75f, deltaTime);
            Assert.That(predicted.x, Is.EqualTo(direct.x).Within(.001f));
            Assert.That(predicted.y, Is.EqualTo(direct.y).Within(.001f));

            Object.Destroy(rig.gameObject);
            Object.Destroy(profile);
        }

        static class BallTestFactory
        {
            public static BallRig Create(FlightProfile profile = null, bool addCollider = false)
            {
                var go = new GameObject("BallRigTest");
                var body = go.AddComponent<Rigidbody2D>();
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                if (addCollider)
                    go.AddComponent<CircleCollider2D>();
                var rig = go.AddComponent<BallRig>();
                if (profile)
                    rig.SetProfile(profile);
                return rig;
            }
        }
    }
}
