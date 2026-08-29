using System;

namespace KMA.Input
{
    public sealed class HoldInputDetector
    {
        const double DefaultMaxChargeSeconds = 1d;
        readonly double maxChargeSeconds;
        bool holding;
        double startedAt;

        public HoldInputDetector(double maxChargeSeconds = DefaultMaxChargeSeconds)
        {
            if (!IsFinite(maxChargeSeconds) || maxChargeSeconds <= 0d)
                throw new ArgumentOutOfRangeException(nameof(maxChargeSeconds));

            this.maxChargeSeconds = maxChargeSeconds;
        }

        public event Action OnHoldStart;
        public event Action<double> OnHoldEnd;

        public double ChargeRatio { get; private set; }

        public void FeedDown(double t)
        {
            if (!IsFinite(t)) return;

            holding = true;
            startedAt = t;
            ChargeRatio = 0d;
            OnHoldStart?.Invoke();
        }

        public void FeedUp(double t)
        {
            if (!holding || !IsFinite(t)) return;

            holding = false;
            double duration = Math.Max(0d, t - startedAt);
            ChargeRatio = Math.Min(1d, duration / maxChargeSeconds);
            OnHoldEnd?.Invoke(duration);
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
