using UnityEngine;

namespace KMA.Gameplay
{
    /// <summary>Fixed-step metres/seconds simulation; rendering never decides the outcome.</summary>
    public sealed class FootballFlightSimulation
    {
        readonly FootballShot shot;
        readonly FootballTuning tuning;
        readonly bool keeperEnabled;
        bool keeperChecked;
        float eventTime;

        public FootballFlightSimulation(FootballShot shot, FootballTuning tuning, bool keeperEnabled = true)
        {
            tuning.EnsureValid();
            this.shot = shot;
            this.tuning = tuning;
            this.keeperEnabled = keeperEnabled;
            Position = new Vector3(0f, FootballShotSolver.BallRadius, 0f);
            Velocity = shot.Velocity;
        }
        public Vector3 Position { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float Time { get; private set; }
        public float KeeperX { get; private set; }
        public float KeeperAngle => Mathf.Clamp(KeeperX / 2.5f, -1f, 1f) * 45f;
        public int Bounces { get; private set; }
        public FootballOutcome? Outcome { get; private set; }
        public bool Done { get; private set; }
        public float OutcomeElapsed => Outcome.HasValue ? Time - eventTime : 0f;

        public void Step()
        {
            if (Done) return;
            const float dt = FootballShotSolver.StepSeconds;
            const float radius = FootballShotSolver.BallRadius;
            Time += dt;
            if (keeperEnabled && !Outcome.HasValue && Time > tuning.KeeperReactionSeconds)
                KeeperX = Mathf.MoveTowards(KeeperX, Mathf.Clamp(shot.AimX * 4.3f, -2.5f, 2.5f), tuning.KeeperSpeed * dt);
            Vector3 previous = Position, p = Position, v = Velocity;
            bool rolling = p.y <= radius + .000001f && v.y == 0f;
            p.x += v.x * dt;
            p.z += v.z * dt;
            if (!rolling)
            {
                p.y += v.y * dt - FootballShotSolver.Gravity * dt * dt * .5f;
                v.y -= FootballShotSolver.Gravity * dt;
            }
            if (p.y < radius)
            {
                p.y = radius;
                Bounces++;
                v.y = Mathf.Abs(v.y) * .42f;
                v.x *= .76f;
                v.z *= .76f;
                if (v.y < .65f) v.y = 0f;
            }
            if (rolling)
            {
                float speed = new Vector2(v.x, v.z).magnitude;
                float factor = Mathf.Max(0f, 1f - 2.8f * dt / Mathf.Max(speed, .000001f));
                v.x *= factor;
                v.z *= factor;
            }
            if (keeperEnabled && !keeperChecked && previous.z < 10.8f && p.z >= 10.8f)
            {
                keeperChecked = true;
                Vector3 contact = Vector3.Lerp(previous, p, (10.8f - previous.z) / (p.z - previous.z));
                if (TouchesKeeper(contact))
                {
                    p = contact;
                    v.z = -Mathf.Abs(v.z) * .32f;
                    v.x *= .4f;
                    v.y = 1.5f;
                    Resolve(FootballOutcome.Saved);
                }
            }
            if (!Outcome.HasValue)
            {
                float post = Mathf.Abs(p.x) - FootballShotSolver.GoalHalfWidth;
                float dz = p.z - FootballShotSolver.GoalDistance;
                bool hitPost = p.y <= FootballShotSolver.GoalHeight + radius && new Vector2(post, dz).magnitude < radius + .055f;
                bool hitBar = Mathf.Abs(p.x) <= FootballShotSolver.GoalHalfWidth + radius &&
                    new Vector2(p.y - FootballShotSolver.GoalHeight, dz).magnitude < radius + .055f;
                if (hitPost || hitBar)
                {
                    v.z = -Mathf.Abs(v.z) * .5f;
                    v.x *= .6f;
                    v.y = hitBar ? -Mathf.Abs(v.y) * .6f : v.y * .6f;
                    Resolve(hitPost ? FootballOutcome.Post : FootballOutcome.Crossbar);
                }
            }
            float goalPlane = FootballShotSolver.GoalDistance + radius;
            if (!Outcome.HasValue && previous.z < goalPlane && p.z >= goalPlane)
            {
                Vector3 crossing = Vector3.Lerp(previous, p, (goalPlane - previous.z) / (p.z - previous.z));
                Resolve(Mathf.Abs(crossing.x) + radius > FootballShotSolver.GoalHalfWidth ? FootballOutcome.Wide :
                    crossing.y + radius > FootballShotSolver.GoalHeight ? FootballOutcome.High : FootballOutcome.Goal);
            }
            if (Outcome == FootballOutcome.Goal && p.z > 11.65f)
            {
                p.z = 11.65f;
                v.z = -Mathf.Abs(v.z) * .12f;
                v.x *= .3f;
                v.y *= .3f;
            }
            if (!Outcome.HasValue && ((rolling && new Vector2(v.x, v.z).magnitude < .12f) || Time > 8f))
                Resolve(FootballOutcome.Short);
            Position = p;
            Velocity = v;
            if (Outcome.HasValue && Time - eventTime > 1.1f) Done = true;
        }

        void Resolve(FootballOutcome outcome)
        {
            if (Outcome.HasValue) return;
            Outcome = outcome;
            eventTime = Time;
        }

        // Capsule silhouettes match the authored keeper sprite in preview pixel coordinates.
        bool TouchesKeeper(Vector3 contact)
        {
            Vector3 screen = FootballShotSolver.Project(contact);
            float angle = -KeeperAngle * Mathf.Deg2Rad;
            float dx = screen.x - (600f + KeeperX * 300f / FootballShotSolver.GoalHalfWidth);
            float dy = screen.y - 281f;
            var point = new Vector2(dx * Mathf.Cos(angle) - dy * Mathf.Sin(angle), dx * Mathf.Sin(angle) + dy * Mathf.Cos(angle));
            float radius = 17f * screen.z;
            foreach (var capsule in KeeperCapsules)
                if (DistanceToSegment(point, capsule.a, capsule.b) <= radius + capsule.radius) return true;
            return false;
        }
        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, delta) / Mathf.Max(delta.sqrMagnitude, .000001f));
            return Vector2.Distance(p, a + delta * t);
        }
        readonly struct Capsule
        {
            public readonly Vector2 a, b;
            public readonly float radius;
            public Capsule(float ax, float ay, float bx, float by, float r)
            { a = new Vector2(ax, ay); b = new Vector2(bx, by); radius = r; }
        }
        static readonly Capsule[] KeeperCapsules = {
            new Capsule(0,-75,0,-75,16), new Capsule(0,-47,0,-13,18),
            new Capsule(-18,-50,-36,-32,7), new Capsule(-36,-32,-51,-47,7),
            new Capsule(18,-50,36,-32,7), new Capsule(36,-32,51,-47,7),
            new Capsule(-53,-48,-58,-57,6), new Capsule(53,-48,58,-57,6),
            new Capsule(-12,-5,-24,13,8), new Capsule(12,-5,25,13,8)
        };
    }
}
