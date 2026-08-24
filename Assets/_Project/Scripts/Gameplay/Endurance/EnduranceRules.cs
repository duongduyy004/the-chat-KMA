using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum EnduranceInputMode { RhythmTap, BreathHold, ObstacleSwipe }
    public enum BeatEvent { Tap, Breath, Jump, Slide }
    public enum SwipeDirection { Up, Down }

    public sealed class EnduranceRules
    {
        const float DefaultStamina = 100f;
        const float TimeLimit = 90f;
        readonly int requiredLaps;
        readonly MinigameLifecycle lifecycle;
        int laps, combo, perfect, good, judged;
        float stamina = DefaultStamina, elapsed;
        BeatEvent currentBeat;

        public EnduranceRules(int requiredLaps = 3, MinigameLifecycle lifecycle = null)
        {
            if (requiredLaps < 1) throw new ArgumentOutOfRangeException(nameof(requiredLaps));
            this.requiredLaps = requiredLaps;
            this.lifecycle = lifecycle ?? new MinigameLifecycle(0f, 0f);
            Mode = EnduranceInputMode.RhythmTap;
            currentBeat = BeatEvent.Tap;
        }

        public static EnduranceRules Default() => new EnduranceRules();

        public static EnduranceRules AtObstacleBeat()
        {
            var value = new EnduranceRules();
            value.AdvanceToPlayForTest();
            value.Dispatch(new AuthoredBeat(BeatEvent.Jump));
            return value;
        }

        public static EnduranceRules ForTest(int laps, int requiredLaps, int combo, float stamina)
        {
            var value = new EnduranceRules(requiredLaps) {
                laps = Mathf.Max(0, laps), combo = Mathf.Max(0, combo),
                stamina = Mathf.Clamp(stamina, 0f, DefaultStamina)
            };
            return value;
        }

        public EnduranceInputMode Mode { get; private set; }
        public MinigamePhase Phase => lifecycle.Phase;
        public float Stamina => stamina;
        public int Laps => laps;
        public int RequiredLaps => requiredLaps;
        public float LapProgress => Mathf.Clamp01((float)laps / requiredLaps);
        public int Combo => combo;
        public int PerfectCount => perfect;
        public int GoodCount => good;
        public int JudgedCount => judged;
        public int MissCount { get; private set; }
        public bool ObstacleCleared { get; private set; }
        public float Elapsed => elapsed;

        public void Tick(float dt)
        {
            dt = Mathf.Max(0f, dt);
            lifecycle.Tick(dt);
            if (lifecycle.Phase == MinigamePhase.Play) elapsed += dt;
        }

        public void AdvanceToPlayForTest() { Tick(0f); Tick(0f); }
        public bool BeginResolve() => lifecycle.BeginResolve();

        public void Dispatch(AuthoredBeat authoredBeat)
        {
            if (lifecycle.Phase != MinigamePhase.Play) return;
            currentBeat = authoredBeat.Beat;
            Mode = authoredBeat.Beat == BeatEvent.Breath ? EnduranceInputMode.BreathHold :
                authoredBeat.Beat == BeatEvent.Jump || authoredBeat.Beat == BeatEvent.Slide ? EnduranceInputMode.ObstacleSwipe :
                EnduranceInputMode.RhythmTap;
            ObstacleCleared = false;
            if (authoredBeat.EndsLap) laps++;
        }

        public void Tap(double inputDsp, double beatDsp)
        {
            if (lifecycle.Phase != MinigamePhase.Play || Mode != EnduranceInputMode.RhythmTap) return;
            judged++;
            TimingJudge result = new RhythmBeatEvaluator(80, 160).Judge(inputDsp, beatDsp);
            if (result == TimingJudge.Perfect) { perfect++; combo++; }
            else if (result == TimingJudge.Good) { good++; combo++; stamina = Mathf.Max(0f, stamina - 2f); }
            else { MissCount++; combo = 0; stamina = Mathf.Max(0f, stamina - 8f); }
        }

        public void EndHold(float beatsHeld)
        {
            if (lifecycle.Phase != MinigamePhase.Play || Mode != EnduranceInputMode.BreathHold) return;
            stamina = Mathf.Min(DefaultStamina, stamina + 12f * Mathf.Clamp01(beatsHeld));
        }

        public void Swipe(SwipeDirection direction)
        {
            if (lifecycle.Phase != MinigamePhase.Play || Mode != EnduranceInputMode.ObstacleSwipe) return;
            bool expected = currentBeat == BeatEvent.Jump && direction == SwipeDirection.Up ||
                currentBeat == BeatEvent.Slide && direction == SwipeDirection.Down;
            ObstacleCleared = expected;
            if (!expected) { combo = 0; stamina = Mathf.Max(0f, stamina - 15f); }
        }

        public MinigameResult BuildResult()
        {
            bool pass = laps >= requiredLaps && stamina > 0f && elapsed <= TimeLimit;
            float accuracy = judged == 0 ? 0f : 2f * (perfect + .5f * good) / judged;
            return ScoreUtil.Build(pass, accuracy, stamina / DefaultStamina, Mathf.Clamp01(combo / 32f));
        }
    }
}
