using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Input
{
    public enum SwipeDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public readonly struct SwipeResult
    {
        public SwipeResult(SwipeDirection direction, double length, double duration, double curvature)
        {
            Direction = direction;
            Length = length;
            Duration = duration;
            Curvature = curvature;
        }

        public SwipeDirection Direction { get; }
        public double Length { get; }
        public double Duration { get; }
        public double Curvature { get; }
    }

    public sealed class SwipeInputDetector
    {
        readonly List<Sample> samples = new List<Sample>();

        public event Action<SwipeResult> OnSwipe;

        public void FeedSample(Vector2 position, double t)
        {
            if (!IsFinite(t) || !IsFinite(position.x) || !IsFinite(position.y)) return;
            if (samples.Count > 0 && t < samples[samples.Count - 1].Time) return;

            samples.Add(new Sample(position, t));
        }

        public void FeedEnd()
        {
            if (samples.Count < 2)
            {
                samples.Clear();
                return;
            }

            Sample first = samples[0];
            Sample last = samples[samples.Count - 1];
            Vector2 delta = last.Position - first.Position;
            double length = delta.magnitude;
            double duration = Math.Max(0d, last.Time - first.Time);
            var result = new SwipeResult(GetDirection(delta), length, duration, GetCurvature(first.Position, last.Position, length));
            samples.Clear();
            OnSwipe?.Invoke(result);
        }

        SwipeDirection GetDirection(Vector2 delta)
        {
            if (Math.Abs(delta.x) >= Math.Abs(delta.y))
                return delta.x >= 0f ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y >= 0f ? SwipeDirection.Up : SwipeDirection.Down;
        }

        double GetCurvature(Vector2 start, Vector2 end, double length)
        {
            if (length == 0d) return 0d;

            Vector2 line = end - start;
            double greatestDeviation = 0d;
            for (var index = 1; index < samples.Count - 1; index++)
            {
                Vector2 offset = samples[index].Position - start;
                double deviation = Math.Abs(line.x * offset.y - line.y * offset.x) / length;
                greatestDeviation = Math.Max(greatestDeviation, deviation);
            }

            return greatestDeviation / length;
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        readonly struct Sample
        {
            public Sample(Vector2 position, double time)
            {
                Position = position;
                Time = time;
            }

            public Vector2 Position { get; }
            public double Time { get; }
        }
    }
}
