using System;
using System.Collections.Generic;

namespace KMA.Input
{
    public sealed class TapMashInputDetector
    {
        const double TapWindowSeconds = 1d;
        readonly Queue<double> tapTimes = new Queue<double>();

        public event Action OnTap;

        public int TapsPerSecond { get; private set; }

        public void FeedTap(double t)
        {
            if (!IsFinite(t)) return;

            tapTimes.Enqueue(t);
            while (tapTimes.Count > 0 && t - tapTimes.Peek() > TapWindowSeconds)
                tapTimes.Dequeue();

            TapsPerSecond = tapTimes.Count;
            OnTap?.Invoke();
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
