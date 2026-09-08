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
        public bool IsHolding => holding;

        // Live elapsed ratio while a hold is in progress, computed with the exact same formula
        // FeedUp will use to commit ChargeRatio at release - so a caller that displays this every
        // frame during the hold (using the same clock it will eventually pass to FeedUp, i.e.
        // Time.realtimeSinceStartupAsDouble in production) shows the player the same number that
        // ends up submitted, instead of a second, independently-drifting clock. Returns the last
        // committed ChargeRatio when no hold is in progress.
        public double CurrentRatio(double now)
        {
            if (!holding || !IsFinite(now)) return ChargeRatio;
            double duration = Math.Max(0d, now - startedAt);
            return Math.Min(1d, duration / maxChargeSeconds);
        }

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
