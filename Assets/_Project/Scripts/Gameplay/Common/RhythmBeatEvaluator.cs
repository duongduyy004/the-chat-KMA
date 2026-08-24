namespace KMA.Gameplay
{
    public readonly struct RhythmBeatEvaluator
    {
        private const double BoundaryToleranceMs = 1e-9;
        private readonly double perfectMs;
        private readonly double goodMs;

        public RhythmBeatEvaluator(double perfectMs, double goodMs)
        {
            this.perfectMs = perfectMs;
            this.goodMs = goodMs;
        }

        public TimingJudge Judge(double inputDspTime, double beatDspTime)
        {
            double delta = System.Math.Abs(inputDspTime - beatDspTime) * 1000d;
            return delta <= perfectMs + BoundaryToleranceMs ? TimingJudge.Perfect :
                delta <= goodMs + BoundaryToleranceMs ? TimingJudge.Good : TimingJudge.Miss;
        }
    }
}
