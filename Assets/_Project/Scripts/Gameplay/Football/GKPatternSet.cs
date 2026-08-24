using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class GKPattern
    {
        public GKPattern(FootballPhase phase, float keeperPlacement, float coverage,
            float minimumForce, float minimumSpin, ShotKind counterShot)
        {
            Phase = phase;
            KeeperPlacement = Mathf.Clamp01(keeperPlacement);
            Coverage = Mathf.Clamp01(coverage);
            MinimumForce = Mathf.Clamp01(minimumForce);
            MinimumSpin = Mathf.Clamp01(minimumSpin);
            CounterShot = counterShot;
        }

        public FootballPhase Phase { get; }
        public float KeeperPlacement { get; }
        public float Coverage { get; }
        public float MinimumForce { get; }
        public float MinimumSpin { get; }
        public ShotKind CounterShot { get; }

        public bool Resolve(FootballShot shot)
        {
            var placementBeatsKeeper = Mathf.Abs(shot.Placement - KeeperPlacement) > Coverage;
            var authoredCounterplay = shot.Kind == CounterShot && shot.Force >= MinimumForce &&
                Mathf.Abs(shot.Spin) >= MinimumSpin;
            return placementBeatsKeeper && authoredCounterplay;
        }
    }

    public sealed class GKPatternSet
    {
        public GKPatternSet(IReadOnlyList<GKPattern> patterns)
        {
            Patterns = patterns;
        }

        public IReadOnlyList<GKPattern> Patterns { get; }

        public static GKPatternSet AuthoredDefault()
        {
            return new GKPatternSet(new[]
            {
                new GKPattern(new FootballPhase(GKReaction.Fast, TargetWidth.Normal), .5f, .25f, .65f, .1f, ShotKind.Curve),
                new GKPattern(new FootballPhase(GKReaction.Normal, TargetWidth.Narrow), .3f, .2f, .55f, .15f, ShotKind.Power),
                new GKPattern(new FootballPhase(GKReaction.Slow, TargetWidth.Normal), .7f, .3f, .45f, .1f, ShotKind.Placement),
                new GKPattern(new FootballPhase(GKReaction.Normal, TargetWidth.Wide), .5f, .15f, .75f, .25f, ShotKind.Curve),
                new GKPattern(new FootballPhase(GKReaction.Fast, TargetWidth.Normal), .2f, .2f, .8f, .2f, ShotKind.Power)
            });
        }
    }
}
