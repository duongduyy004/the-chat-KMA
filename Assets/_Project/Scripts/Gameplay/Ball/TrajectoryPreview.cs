using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        const string ConfigurationError =
            "TrajectoryPreview requires a BallRig source, a LineRenderer, sampleCount >= 2, and sampleStep > 0.";

        [SerializeField] BallRig source;
        [SerializeField] LineRenderer line;
        [SerializeField, Min(2)] int sampleCount = 16;
        [SerializeField, Min(.001f)] float sampleStep = .04f;
        [SerializeField, Min(0f)] float minimumForce = .01f;

        Vector3[] points;
        bool visibleRequested;
        bool configurationErrorLogged;
        float lastForce;

        public BallRig Source => source;
        public LineRenderer Line => line;

        void Awake()
        {
            Prepare();
        }

        void OnDisable()
        {
            HideLine();
        }

        public void Configure(BallRig sourceRig, LineRenderer lineRenderer, int samples, float step)
        {
            source = sourceRig;
            line = lineRenderer;
            sampleCount = samples;
            sampleStep = step;
            Prepare();
        }

        public void SetVisible(bool visible)
        {
            visibleRequested = visible;
            ApplyVisibility();
        }

        public Vector2 Refresh(Vector2 direction, float force, float curvature)
        {
            if (!IsConfigurationValid())
            {
                DisableForInvalidConfiguration();
                return source ? source.Body.position : Vector2.zero;
            }

            BallFlightSnapshot snapshot = source.Snapshot;
            lastForce = force;
            Vector2 landing = source.PredictLandingPoint(direction, force, curvature);
            Vector2 velocity = direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized * Mathf.Max(0f, force)
                : Vector2.zero;
            Vector2 gravity = Physics2D.gravity * (source.Profile ? source.Profile.GravityScale : 1f);
            float groundY = source.Profile ? source.Profile.GroundY : 0f;
            float linearDrag = source.Profile ? source.Profile.LinearDrag : 0f;
            Vector2 position = snapshot.Position;

            points[0] = position;
            bool landed = false;
            for (var index = 1; index < points.Length - 1; index++)
            {
                if (!landed)
                {
                    velocity = Ballistics.AdvanceVelocity(velocity, gravity, curvature, linearDrag, sampleStep);
                    Vector2 nextPosition = position + velocity * sampleStep;
                    landed = nextPosition.y <= groundY && velocity.y <= 0f;
                    position = landed ? landing : nextPosition;
                }

                points[index] = position;
            }

            points[points.Length - 1] = landing;
            line.positionCount = points.Length;
            line.SetPositions(points);
            ApplyVisibility();
            return landing;
        }

        public static Vector2 SampleLanding(
            Vector2 position,
            Vector2 velocity,
            Vector2 gravity,
            float groundY,
            float linearDrag,
            float curvature,
            float deltaTime)
        {
            return Ballistics.PredictGround(
                position,
                velocity,
                gravity,
                groundY,
                linearDrag,
                curvature,
                deltaTime);
        }

        void Prepare()
        {
            if (!IsConfigurationValid())
            {
                DisableForInvalidConfiguration();
                return;
            }

            if (points == null || points.Length != sampleCount)
                points = new Vector3[sampleCount];

            enabled = true;
            line.useWorldSpace = true;
            HideLine();
        }

        bool IsConfigurationValid()
        {
            return source && line && sampleCount >= 2 && sampleStep > 0f;
        }

        void DisableForInvalidConfiguration()
        {
            HideLine();
            enabled = false;
            if (configurationErrorLogged)
                return;

            configurationErrorLogged = true;
            Debug.LogError(ConfigurationError, this);
        }

        void ApplyVisibility()
        {
            if (!line)
                return;

            bool isVisible = enabled
                && visibleRequested
                && source
                && source.Snapshot.IsAttached
                && lastForce > minimumForce
                && points != null;
            line.enabled = isVisible;
            line.positionCount = isVisible ? points.Length : 0;
            if (isVisible)
                line.SetPositions(points);
        }

        void HideLine()
        {
            if (!line)
                return;

            line.enabled = false;
            line.positionCount = 0;
        }
    }
}
