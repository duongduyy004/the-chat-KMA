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
        Vector2 simulationGravity;
        Vector2 lastIntegratedVelocity;
        bool isInFlight;

        public Rigidbody2D Body => EnsureBody();
        public FlightProfile Profile => profile;

        public void SetProfile(FlightProfile value) => profile = value;
        public BallFlightSnapshot Snapshot => new BallFlightSnapshot(
            Body.position,
            Body.velocity,
            attachment != null,
            isInFlight,
            currentCurvature);

        public event Action<Collision2D> Collided;

        void Awake()
        {
            EnsureBody();
        }

        Rigidbody2D EnsureBody()
        {
            if (!body)
                body = GetComponent<Rigidbody2D>();
            return body;
        }

        public void AttachTo(Transform target)
        {
            EnsureBody();
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
            EnsureBody();
            attachment = null;
            isInFlight = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            simulationGravity = Physics2D.gravity * ActiveProfile.GravityScale;
            body.gravityScale = 0f;
            body.drag = 0f;
            body.velocity = direction.normalized * force;
            currentCurvature = curvature;
        }

        public bool IsNearApex(float threshold) => Mathf.Abs(Body.velocity.y) < threshold;

        public Vector2 PredictLandingPoint() => Ballistics.PredictGround(
            Body.position,
            Body.velocity,
            simulationGravity,
            ActiveProfile.GroundY,
            ActiveProfile.LinearDrag,
            currentCurvature,
            Time.fixedDeltaTime);

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

            if (!isInFlight)
                return;

            lastIntegratedVelocity = Ballistics.AdvanceVelocity(
                body.velocity, simulationGravity, currentCurvature, ActiveProfile.LinearDrag, Time.fixedDeltaTime);
            body.velocity = lastIntegratedVelocity;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.contactCount > 0 && isInFlight)
            {
                var contact = collision.GetContact(0);
                body.velocity = Bounce(lastIntegratedVelocity, contact.normal);
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
        public static Vector2 AdvanceVelocity(Vector2 velocity, Vector2 gravity, float curvature, float linearDrag, float deltaTime)
        {
            if (deltaTime <= 0f)
                return velocity;

            if (velocity.sqrMagnitude > Mathf.Epsilon && !Mathf.Approximately(curvature, 0f))
                velocity += Vector2.Perpendicular(velocity.normalized) * curvature * deltaTime;

            velocity += gravity * deltaTime;
            float dragFactor = 1f / (1f + Mathf.Max(0f, linearDrag) * deltaTime);
            return velocity * dragFactor;
        }

        public static Vector2 PredictGround(Vector2 position, Vector2 velocity, Vector2 gravity, float groundY, float linearDrag, float curvature, float deltaTime, int maxSteps = 10000)
        {
            if (deltaTime <= 0f || maxSteps <= 0)
                return position;

            var currentPosition = position;
            var currentVelocity = velocity;
            for (var step = 0; step < maxSteps; step++)
            {
                var nextVelocity = AdvanceVelocity(currentVelocity, gravity, curvature, linearDrag, deltaTime);
                var nextPosition = currentPosition + nextVelocity * deltaTime;
                if (nextPosition.y <= groundY && nextVelocity.y <= 0f)
                {
                    float denominator = currentPosition.y - nextPosition.y;
                    float fraction = Mathf.Approximately(denominator, 0f) ? 1f : Mathf.Clamp01((currentPosition.y - groundY) / denominator);
                    return new Vector2(Mathf.Lerp(currentPosition.x, nextPosition.x, fraction), groundY);
                }

                currentPosition = nextPosition;
                currentVelocity = nextVelocity;
            }

            return position;
        }

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
