using System;
using KMA.Gameplay.UI;
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

    public sealed class EnduranceController : MinigameBase, IPauseAware
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
        bool dspClockPaused;
        double pausedElapsedSeconds;

        public EnduranceRules Rules { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public MinigameResult LastResult { get; private set; }
        public double RhythmOffsetMs { get => rhythmOffsetMs; set => rhythmOffsetMs = value; }
        public bool ObstacleCueVisible => cueSchedule != null && cueSchedule.ObstacleCueVisible;
        public double BeatIntervalSeconds => 60d / Math.Max(1d, beatsPerMinute);
        public AudioSource MetronomeAudioSource => metronome;
        public bool DspClockScheduled => dspClockStarted;
        public double MetronomeStartDspTime => songStartDspTime;
        internal int InputTapCount { get; private set; }
        internal int InputHoldCount { get; private set; }
        internal int InputSwipeCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Rules = new EnduranceRules(3, Lifecycle);
            pattern = LapPattern.Default;
            if (metronome == null)
                metronome = GetComponent<AudioSource>();
            if (metronome == null)
                metronome = gameObject.AddComponent<AudioSource>();
            EnsureMetronomeClip();
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

        public double CurrentBeatDspTime
        {
            get
            {
                if (!dspClockStarted)
                    return AudioSettings.dspTime;

                double elapsed = dspClockPaused
                    ? pausedElapsedSeconds
                    : Math.Max(0d, AudioSettings.dspTime - songStartDspTime);
                int beat = Mathf.FloorToInt((float)(elapsed / BeatIntervalSeconds));
                return songStartDspTime + beat * BeatIntervalSeconds;
            }
        }

        public void TapAtCurrentBeat() => Tap(AudioSettings.dspTime, CurrentBeatDspTime);

        public void Dispatch(AuthoredBeat beat)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Dispatch(beat);
        }

        public void Tap(double inputDsp, double beatDsp)
        {
            if (Phase != MinigamePhase.Play)
                return;

            InputTapCount++;
            Rules.Tap(CalibratedInputTime(inputDsp), beatDsp);
        }

        // Used by the shared detector path, whose router has already applied
        // RhythmOffsetMs before publishing the judged delta.
        internal void TapFromCalibratedDelta(double deltaMs)
        {
            if (Phase != MinigamePhase.Play)
                return;

            InputTapCount++;
            double beatDsp = CurrentBeatDspTime;
            Rules.Tap(beatDsp + deltaMs / 1000d, beatDsp);
        }

        public void EndHold(float beatsHeld)
        {
            if (Phase != MinigamePhase.Play)
                return;

            InputHoldCount++;
            Rules.EndHold(beatsHeld);
        }

        public void Swipe(SwipeDirection direction)
        {
            if (Phase != MinigamePhase.Play)
                return;

            InputSwipeCount++;
            Rules.Swipe(direction);
        }

        public void Resolve()
        {
            if (Phase != MinigamePhase.Play)
                return;

            LastResult = Rules.BuildResult();
            Finish(LastResult);
        }

        protected override MinigameHudState BuildHudState()
        {
            var rules = Rules;
            return new MinigameHudState(
                phase: Phase.ToString(),
                timeRemaining: Mathf.Max(0f, rules == null ? 0f : 90f - rules.Elapsed),
                progress01: rules == null ? 0f : rules.LapProgress,
                stamina01: Mathf.Clamp01((rules == null ? 0f : rules.Stamina) / 100f),
                score: rules == null ? 0f : rules.BuildResult().Score,
                statusText: rules == null ? string.Empty : CurrentStatusText(rules));
        }
        string CurrentStatusText(EnduranceRules rules) =>
            ObstacleCueVisible ? "OBSTACLE — SWIPE NOW" :
            rules.Mode == EnduranceInputMode.BreathHold ? "HOLD TO BREATHE" :
            rules.Mode == EnduranceInputMode.ObstacleSwipe ? "SWIPE TO CLEAR" :
            "TAP TO THE BEAT";
        protected override void TickPlay(float dt)
        {
            Rules.TickPlay(dt);
            AdvanceDspSchedule();
            if (Rules.Laps >= Rules.RequiredLaps)
                Resolve();
        }

        void AdvanceDspSchedule()
        {
            if (dspClockPaused)
                return;
            if (!dspClockStarted)
            {
                EnsureMetronomeAudioSource();
                EnsureMetronomeClip();
                songStartDspTime = AudioSettings.dspTime + 0.05d;
                metronome.Stop();
                metronome.loop = true;
                metronome.PlayScheduled(songStartDspTime);
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
            if (metronome != null)
                metronome.Stop();
            dspClockStarted = false;
            dspClockPaused = false;
            pausedElapsedSeconds = 0d;
            nextBeatIndex = 0;
            cueSchedule = null;
            SetObstacleCueVisible(false);
        }

        public void SetPaused(bool paused)
        {
            if (!dspClockStarted)
                return;
            if (paused)
            {
                if (!dspClockPaused)
                {
                    pausedElapsedSeconds = Math.Max(0d, AudioSettings.dspTime - songStartDspTime);
                    dspClockPaused = true;
                    if (metronome != null)
                        metronome.Pause();
                }
                return;
            }

            if (dspClockPaused)
            {
                songStartDspTime = AudioSettings.dspTime - pausedElapsedSeconds;
                dspClockPaused = false;
                if (metronome != null)
                    metronome.UnPause();
            }
        }

        void EnsureMetronomeClip()
        {
            if (metronome == null || metronome.clip != null)
                return;

            const int sampleRate = 48000;
            const int sampleCount = sampleRate / 2;
            var clip = AudioClip.Create("EnduranceMetronome", sampleCount, 1, sampleRate, false);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float envelope = Mathf.Clamp01(1f - i / (sampleRate * 0.08f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / sampleRate) * envelope * 0.18f;
            }
            clip.SetData(samples, 0);
            metronome.clip = clip;
        }

        void EnsureMetronomeAudioSource()
        {
            if (metronome == null)
                metronome = GetComponent<AudioSource>();
            if (metronome == null)
                metronome = gameObject.AddComponent<AudioSource>();
        }
    }
}
