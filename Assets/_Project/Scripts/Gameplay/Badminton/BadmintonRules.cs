using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum BadmintonShot { Lift, Drive, Smash, Overcharge }

    public sealed class BadmintonRules
    {
        const int TargetScore = 5;
        const float DefaultTimeLimit = 60f;
        readonly float timeLimit;
        readonly MinigameLifecycle lifecycle;
        readonly HashSet<BadmintonShot> usedShots = new HashSet<BadmintonShot>();
        int releases, accurateReleases, distinctShots, exchangeIndex, rally, longestRally;
        bool canAwardPlayerPoint;
        float elapsed;

        public BadmintonRules(MinigameLifecycle lifecycle) : this(DefaultTimeLimit, lifecycle) { }

        public BadmintonRules(float timeLimit = DefaultTimeLimit, MinigameLifecycle lifecycle = null)
        {
            if (timeLimit <= 0f) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            this.timeLimit = timeLimit;
            this.lifecycle = lifecycle ?? new MinigameLifecycle(0f, 0f);
            AuthoredPattern = RallyPattern.AuthoredDefault();
            if (lifecycle == null) { this.lifecycle.Tick(0f); this.lifecycle.Tick(0f); }
        }

        public int PlayerPoints { get; private set; }
        public int OpponentPoints { get; private set; }
        public int Rally => rally;
        public int LongestRally => longestRally;
        public int Releases => releases;
        public int AccurateReleases => accurateReleases;
        public float Elapsed => elapsed;
        public float LastWindCue { get; private set; }
        public float LastExchangeTiming { get; private set; }
        public MinigamePhase Phase => lifecycle.Phase;
        public RallyPattern AuthoredPattern { get; }
        public bool PrimaryObjectiveComplete => PlayerPoints >= TargetScore && PlayerPoints > OpponentPoints;

        public BadmintonShot Release(float charge, float height) => Release(charge, height, 0f);

        public BadmintonShot Release(float charge, float height, float authoredWindCue)
        {
            LastWindCue = authoredWindCue;
            if (charge > 1f) return Record(BadmintonShot.Overcharge, false);
            var shot = height >= .7f ? BadmintonShot.Smash : height >= .35f ? BadmintonShot.Drive : BadmintonShot.Lift;
            return Record(shot, true);
        }

        public bool TryExchange(float charge, float height)
        {
            if (Phase != MinigamePhase.Play) return false;
            var exchange = AuthoredPattern.Exchanges[exchangeIndex % AuthoredPattern.Exchanges.Count];
            exchangeIndex++;
            LastExchangeTiming = exchange.Timing;
            var shot = Release(charge, height, exchange.WindCue);
            if (shot == BadmintonShot.Overcharge) { rally = 0; return false; }
            rally++;
            longestRally = Mathf.Max(longestRally, rally);
            canAwardPlayerPoint = true;
            return true;
        }

        public bool TryExchange(float charge, float height, float authoredWindCue)
        {
            if (Phase != MinigamePhase.Play) return false;
            var exchange = AuthoredPattern.Exchanges[exchangeIndex % AuthoredPattern.Exchanges.Count];
            if (!Mathf.Approximately(exchange.WindCue, authoredWindCue)) return false;
            return TryExchange(charge, height);
        }

        public bool AwardPlayerPoint()
        {
            if (Phase != MinigamePhase.Play || !canAwardPlayerPoint) return false;
            canAwardPlayerPoint = false;
            PlayerPoints++;
            rally = 0;
            return true;
        }

        public void AwardOpponentPoint()
        {
            if (Phase != MinigamePhase.Play) return;
            canAwardPlayerPoint = false;
            OpponentPoints++;
            rally = 0;
        }

        public void Tick(float deltaTime)
        {
            var dt = Mathf.Max(0f, deltaTime);
            var wasPlay = Phase == MinigamePhase.Play;
            lifecycle.Tick(dt);
            if (!wasPlay || Phase != MinigamePhase.Play) return;
            elapsed += dt;
            if (elapsed >= timeLimit) BeginResolve();
        }

        public bool BeginResolve()
        {
            if (!lifecycle.BeginResolve()) return false;
            canAwardPlayerPoint = false;
            return true;
        }

        public static BadmintonRules ForTest(int playerPoints, int opponentPoints, int rally)
        {
            var value = new BadmintonRules();
            value.PlayerPoints = Mathf.Max(0, playerPoints);
            value.OpponentPoints = Mathf.Max(0, opponentPoints);
            value.rally = Mathf.Max(0, rally);
            value.longestRally = value.rally;
            return value;
        }

        public MinigameResult BuildResult()
        {
            var accuracy = 2f * accurateReleases / Mathf.Max(1, releases);
            var efficiency = Mathf.Clamp01((PlayerPoints - OpponentPoints) / (float)TargetScore);
            var mastery = Mathf.Clamp01(distinctShots / 3f);
            return ScoreUtil.Build(PrimaryObjectiveComplete && elapsed < timeLimit, accuracy, efficiency, mastery);
        }

        BadmintonShot Record(BadmintonShot shot, bool accurate)
        {
            releases++;
            if (accurate) { accurateReleases++; if (usedShots.Add(shot)) distinctShots++; }
            return shot;
        }
    }
}
