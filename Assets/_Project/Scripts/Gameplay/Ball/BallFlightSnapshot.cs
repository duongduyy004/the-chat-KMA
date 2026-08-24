using UnityEngine;

namespace KMA.Gameplay
{
    public readonly struct BallFlightSnapshot
    {
        public BallFlightSnapshot(Vector2 position, Vector2 velocity, bool isAttached, bool isInFlight, float curvature)
        {
            Position = position;
            Velocity = velocity;
            IsAttached = isAttached;
            IsInFlight = isInFlight;
            Curvature = curvature;
        }

        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public bool IsAttached { get; }
        public bool IsInFlight { get; }
        public float Curvature { get; }
    }
}
