using System;

namespace KMA.Input
{
    public enum Side
    {
        Left,
        Right
    }

    public sealed class AlternateTapInputDetector
    {
        public event Action<Side> OnValidTap;
        public event Action OnWrongSide;
        public event Action<Side> OnWrongSideTap;

        public Side ExpectedSide { get; private set; } = Side.Left;

        public void FeedTap(Side side, double t)
        {
            if (!IsFinite(t)) return;

            if (side != ExpectedSide)
            {
                OnWrongSide?.Invoke();
                OnWrongSideTap?.Invoke(side);
                return;
            }

            OnValidTap?.Invoke(side);
            ExpectedSide = side == Side.Left ? Side.Right : Side.Left;
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
