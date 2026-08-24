using System;
using UnityEngine;

namespace KMA.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BallRig : MonoBehaviour
    {
        [SerializeField] Rigidbody2D body;
        [SerializeField] FlightProfile profile;

        Transform attachment;
        float currentCurvature;
        bool isInFlight;

        public Rigidbody2D Body => body;
        public FlightProfile Profile => profile;
        public BallFlightSnapshot Snapshot => new BallFlightSnapshot(
            body.position,
            body.velocity,
            attachment != null,
            isInFlight,
            currentCurvature);

        public event Action<Collision2D> Collided;

        void Awake()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();
        }

        public void AttachTo(Transform target)
        {
            attachment = target;
            currentCurvature = 0f;
            isInFlight = false;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            if (attachment)
                body.position = attachment.position;
        }

        public void Launch(Vector2 direction, float force, float curvature)
        {
            attachment = null;
            isInFlight = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = ActiveProfile.GravityScale;
            body.drag = ActiveProfile.LinearDrag;
            body.velocity = direction.normalized * force;
            currentCurvature = curvature;
        }

        public bool IsNearApex(float threshold) => Mathf.Abs(body.velocity.y) < threshold;

        public Vector2 PredictLandingPoint() => Ballistics.PredictGround(
            body.position,
            body.velocity,
            Physics2D.gravity.y * body.gravityScale,
            ActiveProfile.GroundY);

        public Vector2 Bounce(Vector2 incomingVelocity, Vector2 surfaceNormal)
        {
            return Vector2.Reflect(incomingVelocity, surfaceNormal.normalized) * ActiveProfile.BounceDamping;
        }

        void FixedUpdate()
        {
            if (attachment)
            {
                body.position = attachment.position;
                body.velocity = Vector2.zero;
                return;
            }

            if (!isInFlight || body.velocity.sqrMagnitude <= Mathf.Epsilon || Mathf.Approximately(currentCurvature, 0f))
                return;

            body.AddForce(Vector2.Perpendicular(body.velocity.normalized) * currentCurvature);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.contactCount > 0 && isInFlight)
            {
                var contact = collision.GetContact(0);
                body.velocity = Bounce(body.velocity, contact.normal);
            }

            Collided?.Invoke(collision);
        }

        FlightProfile ActiveProfile => profile ? profile : DefaultProfile.Instance;

        static class DefaultProfile
        {
            static FlightProfile instance;

            public static FlightProfile Instance
            {
                get
                {
                    if (!instance)
                    {
                        instance = ScriptableObject.CreateInstance<FlightProfile>();
                        instance.hideFlags = HideFlags.HideAndDontSave;
                    }

                    return instance;
                }
            }
        }
    }

    public static class Ballistics
    {
        public static Vector2 PredictGround(Vector2 position, Vector2 velocity, float gravity, float groundY)
        {
            if (Mathf.Approximately(gravity, 0f))
                return position;

            float c = position.y - groundY;
            float discriminant = velocity.y * velocity.y - 2f * gravity * c;
            if (discriminant < 0f)
                return position;

            float root = Mathf.Sqrt(discriminant);
            float t1 = (-velocity.y + root) / gravity;
            float t2 = (-velocity.y - root) / gravity;
            float time = Mathf.Max(t1, t2);
            if (time < 0f)
                return position;

            return new Vector2(position.x + velocity.x * time, groundY);
        }
    }
}
