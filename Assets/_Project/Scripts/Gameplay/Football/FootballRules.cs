using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class FootballRules
    {
        public const float KickAnimationSeconds = .18f;
        const float ShotFeedbackSeconds = .9f;
        const int DefaultMaxKicks = 5;
        readonly FootballTuning tuning;
        readonly FootballMatchOptions options;
        readonly List<FootballOutcome> outcomes = new List<FootballOutcome>(DefaultMaxKicks);
        readonly IReadOnlyList<FootballOutcome> readonlyOutcomes;
        double chargeSeconds, flightAccumulator;

        public FootballRules(FootballTuning tuning, FootballMatchOptions options = null)
        {
            tuning.EnsureValid();
            this.tuning = tuning;
            this.options = options ?? new FootballMatchOptions(DefaultMaxKicks, 3, true);
            readonlyOutcomes = outcomes.AsReadOnly();
        }
        public FootballState State { get; private set; } = FootballState.Start;
        public float AimX { get; private set; } = .55f;
        public float Power { get; private set; }
        public float KeeperX => Flight?.KeeperX ?? 0f;
        public float FlightElapsed => Flight?.Time ?? 0f;
        public float StateElapsed { get; private set; }
        public FootballShot? LastShot { get; private set; }
        public FootballOutcome? LastOutcome { get; private set; }
        public FootballFlightSimulation Flight { get; private set; }
        public int Kicks { get; private set; }
        public int Goals { get; private set; }
        public IReadOnlyList<FootballOutcome> Outcomes => readonlyOutcomes;
        public bool PreviewVisible => State == FootballState.Charging;
        public int? MaximumKicks => options.MaxKicks;
        public int RequiredGoals => options.RequiredGoals;
        public bool KeeperEnabled => options.KeeperEnabled;

        public bool Start()
        {
            if (State != FootballState.Start) return false;
            ChangeState(FootballState.Aiming);
            return true;
        }
        public bool SetAim(float direction)
        {
            if (!FootballTuning.IsFinite(direction) || Mathf.Abs(direction) > 1f) return false;
            if (State != FootballState.Aiming) return false;
            AimX = direction;
            return true;
        }
        public bool BeginCharge()
        {
            if (State != FootballState.Aiming) return false;
            Power = 0f;
            chargeSeconds = 0d;
            LastShot = null;
            LastOutcome = null;
            Flight = null;
            ChangeState(FootballState.Charging);
            return true;
        }
        public bool ReleaseShot()
        {
            if (State != FootballState.Charging) return false;
            LastShot = FootballShotSolver.Create(AimX, Power);
            Flight = new FootballFlightSimulation(LastShot.Value, tuning, options.KeeperEnabled);
            flightAccumulator = 0d;
            ChangeState(FootballState.Kicking);
            return true;
        }
        public void CancelCharge()
        {
            if (State != FootballState.Charging) return;
            Power = 0f;
            chargeSeconds = 0d;
            ChangeState(FootballState.Aiming);
        }
        public int GetPreview(Vector3[] points) => PreviewVisible
            ? FootballShotSolver.Predict(FootballShotSolver.Create(AimX, Power), tuning, points,
                options.KeeperEnabled) : 0;

        public void Tick(float deltaTime)
        {
            if (!FootballTuning.IsFinite(deltaTime) || deltaTime <= 0f) return;
            double remaining = deltaTime;
            while (remaining > 0d)
            {
                switch (State)
                {
                    case FootballState.Start:
                    case FootballState.Aiming:
                    case FootballState.MatchResult:
                        return;
                    case FootballState.Charging:
                        chargeSeconds = (chargeSeconds + remaining) % (tuning.PowerRiseSeconds * 2d);
                        Power = (float)((1d - Math.Cos(chargeSeconds * Math.PI / tuning.PowerRiseSeconds)) * .5d);
                        StateElapsed = (float)chargeSeconds;
                        return;
                    case FootballState.Kicking:
                        Consume(ref remaining, KickAnimationSeconds, FootballState.Flying);
                        break;
                    case FootballState.Flying:
                        const double realStep = (double)FootballShotSolver.StepSeconds / FootballShotSolver.PresentationSpeed;
                        double used = Math.Min(remaining, realStep - flightAccumulator);
                        remaining -= used;
                        flightAccumulator += used;
                        if (flightAccumulator + 1e-12d < realStep) return;
                        flightAccumulator = 0d;
                        Flight.Step();
                        StateElapsed = Flight.Time;
                        if (Flight.Done)
                        {
                            LastOutcome = Flight.Outcome.Value;
                            outcomes.Add(LastOutcome.Value);
                            Kicks++;
                            if (LastOutcome == FootballOutcome.Goal) Goals++;
                            if ((!options.MaxKicks.HasValue || options.StopAtRequiredGoals) &&
                                Goals >= options.RequiredGoals)
                            {
                                ChangeState(FootballState.MatchResult);
                                break;
                            }
                            ChangeState(FootballState.ShotResult);
                        }
                        break;
                    case FootballState.ShotResult:
                        Consume(ref remaining, ShotFeedbackSeconds,
                            options.MaxKicks.HasValue && Kicks >= options.MaxKicks.Value
                                ? FootballState.MatchResult : FootballState.Aiming);
                        if (State == FootballState.Aiming)
                        {
                            Power = 0f;
                            Flight = null;
                            LastShot = null;
                            LastOutcome = null;
                        }
                        break;
                    default: return;
                }
            }
        }
        public MinigameResult BuildResult()
        {
            if (State != FootballState.MatchResult || !options.MaxKicks.HasValue || Kicks != options.MaxKicks.Value)
                throw new InvalidOperationException("The score result is available only after all configured kicks.");
            bool passed = Goals >= options.RequiredGoals;
            float score = passed ? Goals * 2f : 0f;
            return new MinigameResult(passed, score, passed ? ScoreUtil.ToRank(score) : Rank.F);
        }

        public ChallengeAttemptResult BuildChallengeResult(ChallengeAttemptContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (State != FootballState.MatchResult)
                throw new InvalidOperationException("The challenge result is available only after its target is complete.");
            bool passed = Goals >= options.RequiredGoals;
            MinigameResult exam = context.ChallengeId == "soccer_exam" ? BuildResult() : null;
            return new ChallengeAttemptResult(context, passed,
                new ChallengeMetrics(completedTargets: Goals, kicks: Kicks), exam);
        }
        void Consume(ref double remaining, float duration, FootballState next)
        {
            float timeLeft = Mathf.Max(0f, duration - StateElapsed);
            float step = (float)Math.Min(remaining, timeLeft);
            StateElapsed += step;
            remaining -= step;
            if (StateElapsed >= duration) ChangeState(next);
        }
        void ChangeState(FootballState state) { State = state; StateElapsed = 0f; }
    }
}
