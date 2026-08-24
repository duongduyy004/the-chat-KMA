using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum BallContext
    {
        Low,
        Rising,
        ApexNearNet
    }

    public enum VolleyAction
    {
        Invalid,
        Dig,
        Set,
        Spike
    }

    public sealed class VolleyballRules
    {
        const float DefaultTimeLimit = 60f;
        const float AccurateTouchThreshold = .75f;

        readonly int targetScore;
        readonly float timeLimit;
        readonly MinigameLifecycle lifecycle;
        readonly VolleyReturnPattern returnPattern;
        int playerScore;
        int opponentScore;
        int totalTouches;
        int accurateTouches;
        int combo;
        int longestCombo;
        float elapsed;

        public VolleyballRules(int targetScore = 5, float timeLimit = DefaultTimeLimit, MinigameLifecycle lifecycle = null)
        {
            if (targetScore < 1)
                throw new ArgumentOutOfRangeException(nameof(targetScore));
            if (timeLimit <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeLimit));

            this.targetScore = targetScore;
            this.timeLimit = timeLimit;
            this.lifecycle = lifecycle ?? new MinigameLifecycle(2f, 3f);
            returnPattern = VolleyReturnPattern.AuthoredDefault();
        }

        public int PlayerScore => playerScore;
        public int OpponentScore => opponentScore;
        public int TotalTouches => totalTouches;
        public int AccurateTouches => accurateTouches;
        public int LongestCombo => longestCombo;
        public float Elapsed => elapsed;
        public MinigamePhase Phase => lifecycle.Phase;

        public static VolleyAction ResolveGesture(BallContext context, Vector2 swipe)
        {
            switch (context)
            {
                case BallContext.Low:
                    return VolleyAction.Dig;
                case BallContext.Rising:
                    return VolleyAction.Set;
                case BallContext.ApexNearNet:
                    return swipe.x > 0f && swipe.y < 0f ? VolleyAction.Spike : VolleyAction.Invalid;
                default:
                    return VolleyAction.Invalid;
            }
        }

        public VolleyAction ResolveTouch(BallContext context, Vector2 swipe, bool inReachZone, float timingAccuracy)
        {
            if (!inReachZone || timingAccuracy < AccurateTouchThreshold)
                return VolleyAction.Invalid;

            return ResolveGesture(context, swipe);
        }

        public void RecordTouch(float timingAccuracy)
        {
            float accuracy = Mathf.Clamp01(timingAccuracy);
            totalTouches++;
            if (accuracy >= AccurateTouchThreshold)
                accurateTouches++;
        }

        public void AwardRallyPoint()
        {
            playerScore++;
            combo++;
            longestCombo = Mathf.Max(longestCombo, combo);
        }

        public void AwardOpponentPoint()
        {
            opponentScore++;
            combo = 0;
        }

        public void Tick(float deltaTime)
        {
            bool wasPlay = lifecycle.Phase == MinigamePhase.Play;
            lifecycle.Tick(Mathf.Max(0f, deltaTime));
            if (!wasPlay || lifecycle.Phase != MinigamePhase.Play)
                return;

            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed >= timeLimit)
                lifecycle.BeginResolve();
        }

        public bool TryLaunchSelected(BallRig ball)
        {
            return lifecycle.Phase == MinigamePhase.Play && returnPattern.TryLaunch(ball);
        }

        public bool TryResolveAndLaunch(BallRig ball, BallContext context, Vector2 swipe, bool inReachZone, float timingAccuracy)
        {
            if (lifecycle.Phase != MinigamePhase.Play)
                return false;

            VolleyAction action = ResolveTouch(context, swipe, inReachZone, timingAccuracy);
            if (action == VolleyAction.Invalid)
                return false;

            returnPattern.SelectTrajectory(action);
            RecordTouch(timingAccuracy);
            return returnPattern.TryLaunch(ball);
        }

        public bool BeginResolve() => lifecycle.BeginResolve();

        public void SetForTest(int playerScore, int opponentScore, int combo)
        {
            this.playerScore = Mathf.Max(0, playerScore);
            this.opponentScore = Mathf.Max(0, opponentScore);
            this.combo = Mathf.Max(0, combo);
            longestCombo = this.combo;
        }

        public MinigameResult BuildResult()
        {
            bool pass = playerScore >= targetScore && playerScore > opponentScore && elapsed < timeLimit;
            float accuracy = totalTouches == 0 ? 0f : 2f * accurateTouches / totalTouches;
            float efficiency = 1f - opponentScore / (float)targetScore;
            float mastery = Mathf.Clamp01(longestCombo / 10f);
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }
    }
}
