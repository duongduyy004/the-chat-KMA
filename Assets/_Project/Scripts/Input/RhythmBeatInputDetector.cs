using System;

namespace KMA.Input
{
    public enum TimingJudge
    {
        Perfect,
        Good,
        Miss
    }

    public sealed class RhythmBeatInputDetector
    {
        const double PerfectWindowMs = 80d;
        const double GoodWindowMs = 160d;

        public event Action<TimingJudge, double> OnJudge;

        public void FeedTap(double inputDsp, double beatDsp)
        {
            if (!IsFinite(inputDsp) || !IsFinite(beatDsp)) return;

            double deltaMs = (inputDsp - beatDsp) * 1000d;
            double absoluteDeltaMs = Math.Abs(deltaMs);
            TimingJudge judge = absoluteDeltaMs <= PerfectWindowMs ? TimingJudge.Perfect :
                absoluteDeltaMs <= GoodWindowMs ? TimingJudge.Good : TimingJudge.Miss;
            OnJudge?.Invoke(judge, deltaMs);
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
