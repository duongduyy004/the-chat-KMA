using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum BasketballState { Holding, Passing, AlleyOopFlight, Resolved }
    public enum FinishJudge { Ignored, Early, Perfect, Late }

    public sealed class BasketballRules
    {
        readonly int targetBaskets;
        readonly float timeLimit;
        readonly MinigameLifecycle lifecycle;
        int baskets, attempts, perfects, combo, bestCombo;
        float elapsed;

        public BasketballRules(int targetBaskets = 5, float timeLimit = 30f, MinigameLifecycle lifecycle = null)
        {
            if (targetBaskets < 1) throw new ArgumentOutOfRangeException(nameof(targetBaskets));
            if (timeLimit <= 0f) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            this.targetBaskets = targetBaskets;
            this.timeLimit = timeLimit;
            this.lifecycle = lifecycle ?? new MinigameLifecycle(0f, 0f);
            State = BasketballState.Holding;
            if (lifecycle == null) { this.lifecycle.Tick(0f); this.lifecycle.Tick(0f); }
        }

        public BasketballState State { get; private set; }
        public MinigamePhase Phase => lifecycle.Phase;
        public int Baskets => baskets;
        public int Attempts => attempts;
        public int Combo => combo;
        public int BestCombo => bestCombo;
        public float Elapsed => elapsed;
        public float ApexProgress { get; private set; }
        public bool PrimaryObjectiveComplete => baskets >= targetBaskets;
        public AlleyOopPattern AuthoredPattern { get; private set; }

        public static BasketballRules InFlight(float apexMin, float apexMax, float velocityThreshold)
        {
            var value = new BasketballRules();
            value.AuthoredPattern = new AlleyOopPattern(Vector2.right, 8f, 0f, apexMin, apexMax, velocityThreshold);
            value.State = BasketballState.AlleyOopFlight;
            return value;
        }

        public void Hold(float deltaTime)
        {
            if (State != BasketballState.Holding || Phase != MinigamePhase.Play) return;
            // Holding never selects a toss and never advances the authored apex.
        }

        public bool TryPass(BallRig ball, Vector2 passVector)
        {
            if (!ball || Phase != MinigamePhase.Play || State != BasketballState.Holding || passVector.sqrMagnitude <= Mathf.Epsilon) return false;
            ball.AttachTo(ball.transform);
            AuthoredPattern = AlleyOopPattern.AuthoredDefault(passVector);
            State = BasketballState.Passing;
            return true;
        }

        public bool TryLaunchAlleyOop(BallRig ball)
        {
            if (!ball || AuthoredPattern == null || Phase != MinigamePhase.Play || State != BasketballState.Passing) return false;
            if (!AuthoredPattern.TryLaunch(ball)) return false;
            State = BasketballState.AlleyOopFlight;
            ApexProgress = 0f;
            return true;
        }

        public bool TryLaunchAlleyOop(BallRig ball, AlleyOopPattern candidate)
        {
            if (!ReferenceEquals(candidate, AuthoredPattern)) return false;
            return TryLaunchAlleyOop(ball);
        }

        public FinishJudge TapFinish(float ballY, float velocityY)
        {
            if (State != BasketballState.AlleyOopFlight || Phase != MinigamePhase.Play) return FinishJudge.Ignored;
            FinishJudge judge = ballY < AuthoredPattern.ApexMin || velocityY > AuthoredPattern.VelocityThreshold ? FinishJudge.Early :
                ballY > AuthoredPattern.ApexMax || velocityY < -AuthoredPattern.VelocityThreshold ? FinishJudge.Late : FinishJudge.Perfect;
            attempts++;
            if (judge == FinishJudge.Perfect)
            {
                baskets++; combo++; bestCombo = Mathf.Max(bestCombo, combo); perfects++; ApexProgress = 1f;
            }
            else combo = 0;
            if (PrimaryObjectiveComplete) { State = BasketballState.Resolved; lifecycle.BeginResolve(); }
            else State = BasketballState.Holding;
            return judge;
        }

        public void Tick(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            bool wasPlay = lifecycle.Phase == MinigamePhase.Play;
            lifecycle.Tick(dt);
            if (wasPlay && lifecycle.Phase == MinigamePhase.Play)
            {
                elapsed += dt;
                if (elapsed >= timeLimit && State != BasketballState.Resolved)
                {
                    State = BasketballState.Resolved;
                    lifecycle.BeginResolve();
                }
            }
        }

        public bool BeginResolve() => lifecycle.BeginResolve();

        public MinigameResult BuildResult()
        {
            bool pass = PrimaryObjectiveComplete && elapsed < timeLimit;
            float accuracy = 2f * perfects / Mathf.Max(1, attempts);
            float efficiency = Mathf.Clamp01(targetBaskets / (float)Mathf.Max(1, attempts));
            float mastery = Mathf.Clamp01(bestCombo / 5f);
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }
    }
}
