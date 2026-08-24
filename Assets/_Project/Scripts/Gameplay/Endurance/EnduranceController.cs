using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("KMA.Gameplay.Running.PlayMode.Tests")]

namespace KMA.Gameplay
{
    public sealed class EnduranceCueSchedule
    {
        readonly int obstacleBeat;
        readonly int warningBeats;

        public EnduranceCueSchedule(int obstacleBeat, int warningBeats)
        {
            if (obstacleBeat < 0) throw new ArgumentOutOfRangeException(nameof(obstacleBeat));
            if (warningBeats < 0) throw new ArgumentOutOfRangeException(nameof(warningBeats));

            this.obstacleBeat = obstacleBeat;
            this.warningBeats = warningBeats;
            Mode = EnduranceInputMode.RhythmTap;
        }

        public bool ObstacleCueVisible { get; private set; }
        public EnduranceInputMode Mode { get; private set; }
        public int WarningBeat => obstacleBeat - warningBeats;
        public int WarningLeadBeats => warningBeats;

        public void AdvanceToBeat(int beat)
        {
            ObstacleCueVisible = beat >= WarningBeat;
            if (beat >= obstacleBeat)
                Mode = EnduranceInputMode.ObstacleSwipe;
        }
    }

    public sealed class EnduranceController : MinigameBase
    {
        [SerializeField] double rhythmOffsetMs;
        [SerializeField] double beatsPerMinute = 120d;
        [SerializeField] GameObject obstacleCue;
        [SerializeField] AudioSource metronome;

        LapPattern pattern;
        EnduranceCueSchedule cueSchedule;
        double songStartDspTime;
        int nextBeatIndex;
        bool dspClockStarted;

        public EnduranceRules Rules { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public MinigameResult LastResult { get; private set; }
        public double RhythmOffsetMs { get => rhythmOffsetMs; set => rhythmOffsetMs = value; }
        public bool ObstacleCueVisible => cueSchedule != null && cueSchedule.ObstacleCueVisible;
        public double BeatIntervalSeconds => 60d / Math.Max(1d, beatsPerMinute);

        protected override void Awake()
        {
            base.Awake();
            Rules = new EnduranceRules(3, Lifecycle);
            pattern = LapPattern.Default;
        }

        internal void ConfigureForTest(int requiredLaps)
        {
            ConfigureLifecycleForTest(0f, 0f, requiredLaps);
        }

        internal void ConfigureLifecycleForTest(float tutorialSeconds, float countdownSeconds, int requiredLaps)
        {
            Lifecycle = new MinigameLifecycle(tutorialSeconds, countdownSeconds);
            Rules = new EnduranceRules(requiredLaps, Lifecycle);
            LastResult = null;
            ResetDspSchedule();
        }

        internal void Simulate(float dt)
        {
            Lifecycle.Tick(Mathf.Max(0f, dt));
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(Mathf.Max(0f, dt));
        }

        internal void AdvanceToPlayForTest()
        {
            Rules.AdvanceToPlayForTest();
        }

        internal void ConfigurePatternForTest(LapPattern authoredPattern)
        {
            pattern = authoredPattern ?? throw new ArgumentNullException(nameof(authoredPattern));
            ResetDspSchedule();
        }

        internal void AdvanceToBeatForTest(int beat)
        {
            if (beat < 0) throw new ArgumentOutOfRangeException(nameof(beat));
            DispatchBeat(beat);
        }

        public double CalibratedInputTime(double rawDspTime) => rawDspTime + rhythmOffsetMs / 1000d;

        public void Dispatch(AuthoredBeat beat)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Dispatch(beat);
        }

        public void Tap(double inputDsp, double beatDsp)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Tap(CalibratedInputTime(inputDsp), beatDsp);
        }

        public void EndHold(float beatsHeld)
        {
            if (Phase == MinigamePhase.Play)
                Rules.EndHold(beatsHeld);
        }

        public void Swipe(SwipeDirection direction)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Swipe(direction);
        }

        public void Resolve()
        {
            if (Phase != MinigamePhase.Play)
                return;

            LastResult = Rules.BuildResult();
            Finish(LastResult);
        }

        protected override void TickPlay(float dt)
        {
            Rules.TickPlay(dt);
            AdvanceDspSchedule();
            if (Rules.Laps >= Rules.RequiredLaps)
                Resolve();
        }

        void AdvanceDspSchedule()
        {
            if (!dspClockStarted)
            {
                songStartDspTime = AudioSettings.dspTime;
                nextBeatIndex = 0;
                dspClockStarted = true;
            }

            double elapsed = AudioSettings.dspTime - songStartDspTime;
            int beat = Mathf.FloorToInt((float)(elapsed / BeatIntervalSeconds));
            while (nextBeatIndex <= beat)
                DispatchBeat(nextBeatIndex++);
        }

        void DispatchBeat(int index)
        {
            if (pattern == null || pattern.Events.Length == 0)
                return;

            int authoredIndex = index % pattern.Events.Length;
            int obstacleBeat = FindNextObstacleBeat(authoredIndex, index);
            cueSchedule = obstacleBeat < 0 ? null : new EnduranceCueSchedule(obstacleBeat, 2);
            if (cueSchedule != null)
                cueSchedule.AdvanceToBeat(index);
            SetObstacleCueVisible(ObstacleCueVisible);

            if (Phase == MinigamePhase.Play)
                Rules.Dispatch(pattern.EventAt(authoredIndex));
        }

        int FindNextObstacleBeat(int authoredIndex, int absoluteBeat)
        {
            for (int offset = 0; offset < pattern.Events.Length; offset++)
            {
                int index = (authoredIndex + offset) % pattern.Events.Length;
                if (pattern.HasWarningAt(index))
                    return absoluteBeat + offset;
            }

            return -1;
        }

        void SetObstacleCueVisible(bool visible)
        {
            if (obstacleCue != null)
                obstacleCue.SetActive(visible);
        }

        void ResetDspSchedule()
        {
            dspClockStarted = false;
            nextBeatIndex = 0;
            cueSchedule = null;
            SetObstacleCueVisible(false);
        }
    }
}
