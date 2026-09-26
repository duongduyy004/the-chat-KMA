using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class FootballRules
    {
        const float AimLimit = .9f;
        const float PowerNoiseComparisonTolerance = .000001f;
        const float KickAnimationSeconds = .18f;
        const float ShotFeedbackSeconds = 1f;
        const int MaxKicks = 5;
        const int RequiredGoals = 3;

        readonly FootballTuning tuning;
        readonly Func<float> signedNoise;
        readonly System.Random random;
        readonly List<FootballOutcome> outcomes = new List<FootballOutcome>(MaxKicks);
        readonly IReadOnlyList<FootballOutcome> readonlyOutcomes;
        double aimPhaseSeconds;
        double chargePhaseSeconds;

        public FootballRules(FootballTuning tuning, Func<float> signedNoise = null)
        {
            tuning.EnsureValid();
            this.tuning = tuning;
            random = new System.Random();
            this.signedNoise = signedNoise ?? NextSignedNoise;
            readonlyOutcomes = outcomes.AsReadOnly();
            aimPhaseSeconds = tuning.AimTraverseSeconds * .5d;
            State = FootballState.Start;
        }

        public FootballState State { get; private set; }
        public float AimX { get; private set; }
        public float Power { get; private set; }
        public float KeeperX { get; private set; }
        public float FlightElapsed { get; private set; }
        public float StateElapsed { get; private set; }
        public FootballShot? LastShot { get; private set; }
        public FootballOutcome? LastOutcome { get; private set; }
        public int Kicks { get; private set; }
        public int Goals { get; private set; }
        public IReadOnlyList<FootballOutcome> Outcomes => readonlyOutcomes;

        public bool Start()
        {
            if (State != FootballState.Start)
                return false;
            ChangeState(FootballState.Aiming);
            return true;
        }

        public bool LockAim()
        {
            if (State != FootballState.Aiming)
                return false;
            ChangeState(FootballState.AimLocked);
            return true;
        }

        public bool BeginCharge()
        {
            if (State != FootballState.AimLocked)
                return false;
            Power = 0f;
            chargePhaseSeconds = 0d;
            LastShot = null;
            LastOutcome = null;
            ChangeState(FootballState.Charging);
            return true;
        }

        public bool ReleaseShot()
        {
            if (State != FootballState.Charging)
                return false;

            float shotPower = Power;
            float noise = shotPower > .85f + PowerNoiseComparisonTolerance ? signedNoise() : 0f;
            LastShot = FootballShotSolver.Create(AimX, shotPower, noise);
            LastOutcome = null;
            FlightElapsed = 0f;
            KeeperX = 0f;
            ChangeState(FootballState.Kicking);
            return true;
        }

        public void CancelCharge()
        {
            if (State != FootballState.Charging)
                return;
            Power = 0f;
            chargePhaseSeconds = 0d;
            ChangeState(FootballState.AimLocked);
        }

        public void Tick(float deltaTime)
        {
            if (!FootballTuning.IsFinite(deltaTime) || deltaTime <= 0f)
                return;

            double remaining = deltaTime;
            while (remaining > 0d)
            {
                switch (State)
                {
                    case FootballState.Start:
                    case FootballState.AimLocked:
                    case FootballState.MatchResult:
                        return;

                    case FootballState.Aiming:
                        AdvanceAim(remaining);
                        return;

                    case FootballState.Charging:
                        AdvanceCharge(remaining);
                        return;

                    case FootballState.Kicking:
                        ConsumeStateTime(ref remaining, KickAnimationSeconds, FootballState.Flying);
                        break;

                    case FootballState.Flying:
                        AdvanceFlight(ref remaining);
                        break;

                    case FootballState.ShotResult:
                        AdvanceShotFeedback(ref remaining);
                        break;

                    default:
                        return;
                }
            }
        }

        public MinigameResult BuildResult()
        {
            if (State != FootballState.MatchResult || Kicks != MaxKicks)
                throw new InvalidOperationException("The result is available only after all five kicks.");

            bool passed = Goals >= RequiredGoals;
            float score = !passed ? 0f : Goals switch
            {
                3 => 6f,
                4 => 8f,
                _ => 10f
            };
            return new MinigameResult(passed, score, passed ? ScoreUtil.ToRank(score) : Rank.F);
        }

        void AdvanceAim(double deltaTime)
        {
            double traverse = tuning.AimTraverseSeconds;
            double period = traverse * 2d;
            aimPhaseSeconds = (aimPhaseSeconds + deltaTime) % period;
            double phase = aimPhaseSeconds <= traverse
                ? aimPhaseSeconds / traverse
                : 2d - (aimPhaseSeconds / traverse);
            AimX = (float)((phase * 2d - 1d) * AimLimit);
        }

        void AdvanceCharge(double deltaTime)
        {
            double rise = tuning.PowerRiseSeconds;
            chargePhaseSeconds = (chargePhaseSeconds + deltaTime) % (rise * 2d);
            Power = (float)(chargePhaseSeconds <= rise
                ? chargePhaseSeconds / rise
                : 2d - chargePhaseSeconds / rise);
            StateElapsed = (float)chargePhaseSeconds;
        }

        void AdvanceFlight(ref double remaining)
        {
            FootballShot shot = LastShot.Value;
            float timeLeft = Mathf.Max(0f, shot.FlightSeconds - FlightElapsed);
            float step = (float)Math.Min(remaining, timeLeft);
            FlightElapsed += step;
            StateElapsed = FlightElapsed;
            remaining -= step;
            KeeperX = FootballShotSolver.KeeperX(shot, tuning, FlightElapsed);

            if (FlightElapsed >= shot.FlightSeconds)
            {
                LastOutcome = FootballShotSolver.Resolve(shot, tuning);
                outcomes.Add(LastOutcome.Value);
                Kicks++;
                if (LastOutcome == FootballOutcome.Goal)
                    Goals++;
                ChangeState(FootballState.ShotResult);
            }
        }

        void AdvanceShotFeedback(ref double remaining)
        {
            float timeLeft = Mathf.Max(0f, ShotFeedbackSeconds - StateElapsed);
            float step = (float)Math.Min(remaining, timeLeft);
            StateElapsed += step;
            remaining -= step;
            if (StateElapsed < ShotFeedbackSeconds)
                return;

            if (Kicks >= MaxKicks)
            {
                ChangeState(FootballState.MatchResult);
                return;
            }

            AimX = 0f;
            aimPhaseSeconds = tuning.AimTraverseSeconds * .5d;
            KeeperX = 0f;
            LastShot = null;
            ChangeState(FootballState.Aiming);
        }

        void ConsumeStateTime(ref double remaining, float duration, FootballState nextState)
        {
            float timeLeft = Mathf.Max(0f, duration - StateElapsed);
            float step = (float)Math.Min(remaining, timeLeft);
            StateElapsed += step;
            remaining -= step;
            if (StateElapsed >= duration)
                ChangeState(nextState);
        }

        void ChangeState(FootballState state)
        {
            State = state;
            StateElapsed = 0f;
        }

        float NextSignedNoise()
        {
            return (float)(random.NextDouble() * 2d - 1d);
        }
    }
}
