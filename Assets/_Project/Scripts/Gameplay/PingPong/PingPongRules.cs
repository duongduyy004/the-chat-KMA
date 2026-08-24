using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class PingPongRules
    {
        const float InitialBallSpeed = 10f;
        const int TargetScore = 5;
        const float ReturnSpeedMultiplier = 1.08f;
        const float DefaultTimeLimit = 60f;
        const float AccurateTimingThreshold = .75f;

        readonly float initialSpeed;
        readonly float maxSpeed;
        readonly float timeLimit;
        readonly MinigameLifecycle lifecycle;
        readonly ReturnPattern returnPattern;
        int rally;
        int longestRally;
        int returns;
        bool canAwardPlayerPoint;
        float totalAccuracy;
        float elapsed;

        public PingPongRules(float initialSpeed = InitialBallSpeed, float maxSpeed = 25f, MinigameLifecycle lifecycle = null,
            float timeLimit = DefaultTimeLimit)
        {
            if (initialSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(initialSpeed));
            if (maxSpeed < initialSpeed) throw new ArgumentOutOfRangeException(nameof(maxSpeed));
            if (timeLimit <= 0f) throw new ArgumentOutOfRangeException(nameof(timeLimit));

            this.initialSpeed = initialSpeed;
            this.maxSpeed = maxSpeed;
            BallSpeed = initialSpeed;
            this.timeLimit = timeLimit;
            this.lifecycle = lifecycle ?? new MinigameLifecycle(0f, 0f);
            returnPattern = ReturnPattern.AuthoredDefault();

            if (lifecycle == null)
            {
                this.lifecycle.Tick(0f);
                this.lifecycle.Tick(0f);
            }
        }

        public int PlayerScore { get; private set; }
        public int PlayerPoints => PlayerScore;
        public int OpponentScore { get; private set; }
        public int OpponentPoints => OpponentScore;
        public int Rally => rally;
        public int RallyLength => rally;
        public int LongestRally => longestRally;
        public int Returns => returns;
        public float BallSpeed { get; private set; }
        public float Elapsed => elapsed;
        public MinigamePhase Phase => lifecycle.Phase;
        public ReturnPattern AuthoredPattern => returnPattern;
        public bool PrimaryObjectiveComplete => PlayerScore >= TargetScore && PlayerScore > OpponentScore;

        public void SuccessfulReturn(float accuracy)
        {
            rally++;
            longestRally = Mathf.Max(longestRally, rally);
            totalAccuracy += Mathf.Clamp01(accuracy);
            returns++;
            BallSpeed = Mathf.Min(maxSpeed, BallSpeed * ReturnSpeedMultiplier);
        }

        public bool TryReturn(BallRig ball, float timingAccuracy, Vector2 placement)
        {
            if (Phase != MinigamePhase.Play || timingAccuracy < AccurateTimingThreshold || !returnPattern.IsPlacementValid(placement))
                return false;

            var exchangeIndex = FindExchange(placement);
            if (!returnPattern.TryLaunch(ball, exchangeIndex, BallSpeed))
                return false;

            SuccessfulReturn(timingAccuracy);
            canAwardPlayerPoint = true;
            return true;
        }

        public bool AwardPlayerPoint()
        {
            if (!canAwardPlayerPoint)
                return false;

            canAwardPlayerPoint = false;
            PlayerScore++;
            rally = 0;
            return true;
        }

        public void AwardOpponentPoint()
        {
            canAwardPlayerPoint = false;
            OpponentScore++;
            rally = 0;
        }

        public void Tick(float deltaTime)
        {
            var dt = Mathf.Max(0f, deltaTime);
            var wasPlay = Phase == MinigamePhase.Play;
            lifecycle.Tick(dt);
            if (!wasPlay || Phase != MinigamePhase.Play)
                return;

            elapsed += dt;
            if (elapsed >= timeLimit)
                lifecycle.BeginResolve();
        }

        public bool BeginResolve() => lifecycle.BeginResolve();

        public static PingPongRules ForTest(int playerPoints, int opponentPoints, int rally)
        {
            var value = new PingPongRules(10f, 25f);
            value.PlayerScore = Mathf.Max(0, playerPoints);
            value.OpponentScore = Mathf.Max(0, opponentPoints);
            value.rally = Mathf.Max(0, rally);
            value.longestRally = value.rally;
            return value;
        }

        public MinigameResult BuildResult()
        {
            var accuracy = 2f * totalAccuracy / Mathf.Max(1, returns);
            var efficiency = Mathf.Clamp01((PlayerScore - OpponentScore) / (float)TargetScore);
            var mastery = Mathf.Clamp01(longestRally / 20f);
            return ScoreUtil.Build(PrimaryObjectiveComplete && elapsed < timeLimit, accuracy, efficiency, mastery);
        }

        int FindExchange(Vector2 placement)
        {
            var exchanges = returnPattern.Exchanges;
            var selected = 0;
            var nearest = float.PositiveInfinity;
            for (var i = 0; i < exchanges.Count; i++)
            {
                var distance = Vector2.Distance(exchanges[i], placement);
                if (distance < nearest)
                {
                    selected = i;
                    nearest = distance;
                }
            }

            return selected;
        }
    }
}
