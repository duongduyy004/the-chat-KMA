using UnityEngine;

namespace KMA.Gameplay
{
    public enum EnduranceInputMode
    {
        RhythmTap,
        BreathHold,
        ObstacleSwipe
    }

    public enum BeatEvent
    {
        Tap,
        Breath,
        Jump,
        Slide
    }

    public enum SwipeDirection
    {
        Up,
        Down
    }

    public sealed class EnduranceRules
    {
        const float DefaultStamina = 100f;
        const float TimeLimit = 90f;

        readonly int requiredLaps;
        int laps;
        int combo;
        int perfect;
        int good;
        int judged;
        float stamina = DefaultStamina;
        float elapsed;
        BeatEvent currentBeat;

        public EnduranceRules(int requiredLaps = 3)
        {
            if (requiredLaps < 1)
                throw new System.ArgumentOutOfRangeException(nameof(requiredLaps));

            this.requiredLaps = requiredLaps;
            Mode = EnduranceInputMode.RhythmTap;
            currentBeat = BeatEvent.Tap;
        }

        public static EnduranceRules Default() => new EnduranceRules();

        public static EnduranceRules AtObstacleBeat()
        {
            var value = new EnduranceRules();
            value.EnterBeat(BeatEvent.Jump);
            return value;
        }

        public static EnduranceRules ForTest(int laps, int requiredLaps, int combo, float stamina)
        {
            var value = new EnduranceRules(requiredLaps)
            {
                laps = Mathf.Max(0, laps),
                combo = Mathf.Max(0, combo),
                stamina = Mathf.Clamp(stamina, 0f, DefaultStamina)
            };
            return value;
        }

        public EnduranceInputMode Mode { get; private set; }
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

        public void EnterBeat(BeatEvent beat)
        {
            currentBeat = beat;
            Mode = beat == BeatEvent.Breath ? EnduranceInputMode.BreathHold :
                beat == BeatEvent.Jump || beat == BeatEvent.Slide ? EnduranceInputMode.ObstacleSwipe :
                EnduranceInputMode.RhythmTap;
            ObstacleCleared = false;
        }

        public void Tap(double inputDsp, double beatDsp)
        {
            if (Mode != EnduranceInputMode.RhythmTap)
                return;

            judged++;
            TimingJudge result = new RhythmBeatEvaluator(80, 160).Judge(inputDsp, beatDsp);
            if (result == TimingJudge.Perfect)
            {
                perfect++;
                combo++;
            }
            else if (result == TimingJudge.Good)
            {
                good++;
                combo++;
                stamina = Mathf.Max(0f, stamina - 2f);
            }
            else
            {
                MissCount++;
                combo = 0;
                stamina = Mathf.Max(0f, stamina - 8f);
            }
        }

        public void EndHold(float beatsHeld)
        {
            if (Mode != EnduranceInputMode.BreathHold)
                return;

            stamina = Mathf.Min(DefaultStamina, stamina + 12f * Mathf.Clamp01(beatsHeld));
        }

        public void Swipe(SwipeDirection direction)
        {
            if (Mode != EnduranceInputMode.ObstacleSwipe)
                return;

            bool expected = currentBeat == BeatEvent.Jump && direction == SwipeDirection.Up ||
                currentBeat == BeatEvent.Slide && direction == SwipeDirection.Down;
            ObstacleCleared = expected;
            if (!expected)
            {
                combo = 0;
                stamina = Mathf.Max(0f, stamina - 15f);
            }
        }

        public void CompleteLap() => laps++;

        public void Tick(float dt)
        {
            elapsed += Mathf.Max(0f, dt);
        }

        public MinigameResult BuildResult()
        {
            bool pass = laps >= requiredLaps && stamina > 0f && elapsed <= TimeLimit;
            float accuracy = judged == 0 ? 0f : 2f * (perfect + .5f * good) / judged;
            return ScoreUtil.Build(pass, accuracy, stamina / DefaultStamina, Mathf.Clamp01(combo / 32f));
        }
    }
}
