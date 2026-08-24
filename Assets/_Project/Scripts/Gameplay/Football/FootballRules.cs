using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum ShotKind { Placement, Power, Curve }
    public enum GKReaction { Slow, Normal, Fast }
    public enum TargetWidth { Wide, Normal, Narrow }
    public enum DifficultyModifier { Neutral, SlowKeeper, FastKeeper, WideTarget, NarrowTarget }

    public readonly struct FootballShot : IEquatable<FootballShot>
    {
        public FootballShot(float placement, float force, float spin, ShotKind kind)
        {
            Placement = Mathf.Clamp01(placement);
            Force = Mathf.Clamp01(force);
            Spin = Mathf.Clamp(spin, -1f, 1f);
            Kind = kind;
        }

        public float Placement { get; }
        public float Force { get; }
        public float Spin { get; }
        public ShotKind Kind { get; }

        public bool Equals(FootballShot other) => Placement == other.Placement && Force == other.Force &&
            Spin == other.Spin && Kind == other.Kind;

        public override bool Equals(object obj) => obj is FootballShot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Placement, Force, Spin, Kind);
    }

    public sealed class FootballPhase
    {
        public FootballPhase(GKReaction reaction, TargetWidth targetWidth)
        {
            var reactionModifier = reaction != GKReaction.Normal;
            var targetModifier = targetWidth != TargetWidth.Normal;
            if (reactionModifier == targetModifier)
                throw new ArgumentException("FootballPhase requires exactly one difficulty modifier.");

            Reaction = reaction;
            TargetWidth = targetWidth;
            ActiveModifier = reactionModifier ?
                (reaction == GKReaction.Fast ? DifficultyModifier.FastKeeper : DifficultyModifier.SlowKeeper) :
                (targetWidth == TargetWidth.Narrow ? DifficultyModifier.NarrowTarget : DifficultyModifier.WideTarget);
        }

        public GKReaction Reaction { get; }
        public TargetWidth TargetWidth { get; }
        public DifficultyModifier ActiveModifier { get; }
        public int ActiveModifierCount => ActiveModifier == DifficultyModifier.Neutral ? 0 : 1;
    }

    public sealed class FootballRules
    {
        const int MaxKicks = 5;
        const int RequiredGoals = 3;
        const float DefaultTimeLimit = 30f;

        readonly MinigameLifecycle lifecycle;
        readonly GKPatternSet patternSet;
        int kicks;
        int goals;
        float accuracyTotal;
        float elapsed;

        public FootballRules(GKPatternSet patternSet = null, MinigameLifecycle lifecycle = null)
        {
            this.patternSet = patternSet ?? GKPatternSet.AuthoredDefault();
            if (this.patternSet.Patterns.Count != MaxKicks)
                throw new ArgumentException("Football requires exactly five authored goalkeeper patterns.", nameof(patternSet));

            this.lifecycle = lifecycle ?? new MinigameLifecycle(0f, 0f);
            if (lifecycle == null) { this.lifecycle.Tick(0f); this.lifecycle.Tick(0f); }
        }

        public int Kicks => kicks;
        public int Goals => goals;
        public float Elapsed => elapsed;
        public MinigamePhase Phase => lifecycle.Phase;
        public GKPatternSet PatternSet => patternSet;
        public GKPattern LastKeeperPattern { get; private set; }
        public FootballShot LastShot { get; private set; }
        public bool PrimaryObjectiveComplete => kicks == MaxKicks && goals >= RequiredGoals;

        public bool ResolveAuthoredShot(FootballShot shot)
        {
            if (Phase != MinigamePhase.Play || kicks >= MaxKicks)
                return false;

            return ResolveAuthoredShot(shot, patternSet.Patterns[kicks]);
        }

        public bool ResolveAuthoredShot(FootballShot shot, GKPattern selectedPattern)
        {
            if (Phase != MinigamePhase.Play || kicks >= MaxKicks ||
                !ReferenceEquals(selectedPattern, patternSet.Patterns[kicks]))
                return false;

            LastKeeperPattern = selectedPattern;
            var goal = selectedPattern.Resolve(shot);
            kicks++;
            if (goal) goals++;
            accuracyTotal += Mathf.Clamp01(shot.Placement);
            LastShot = shot;
            if (kicks == MaxKicks) lifecycle.BeginResolve();
            return goal;
        }

        public void Tick(float deltaTime)
        {
            var dt = Mathf.Max(0f, deltaTime);
            var wasPlay = Phase == MinigamePhase.Play;
            lifecycle.Tick(dt);
            if (wasPlay && Phase == MinigamePhase.Play)
            {
                elapsed += dt;
                if (elapsed >= DefaultTimeLimit) lifecycle.BeginResolve();
            }
        }

        public bool BeginResolve() => lifecycle.BeginResolve();

        public static FootballRules ForTest(int kicks, int goals)
        {
            var value = new FootballRules();
            value.kicks = Mathf.Clamp(kicks, 0, MaxKicks);
            value.goals = Mathf.Clamp(goals, 0, value.kicks);
            return value;
        }

        public MinigameResult BuildResult()
        {
            return ScoreUtil.Build(kicks == MaxKicks && goals >= RequiredGoals,
                2f * accuracyTotal / MaxKicks,
                Mathf.Clamp01((goals - RequiredGoals) / 2f),
                Mathf.Clamp01(kicks / 3f));
        }
    }
}
