using System;
using UnityEngine;

namespace KMA.Gameplay
{
    public enum VolleyPhase
    {
        Dig,
        Set,
        Spike
    }

    public enum VolleyTrajectory
    {
        AuthoredDig,
        AuthoredSet,
        AuthoredSpike
    }

    public sealed class VolleyReturnPattern
    {
        static readonly VolleyPhase[] AuthoredPhases = { VolleyPhase.Dig, VolleyPhase.Set, VolleyPhase.Spike };

        public float CueLeadSeconds { get; }
        public VolleyPhase[] Phases => (VolleyPhase[])AuthoredPhases.Clone();
        public bool HasSelectedTrajectory { get; private set; }
        public VolleyPhase SelectedPhase { get; private set; }

        public bool CanLaunch => HasSelectedTrajectory;

        public bool TryLaunch(BallRig ball)
        {
            if (!HasSelectedTrajectory || !ball)
                return false;

            ball.Launch(LaunchDirection, LaunchForce, LaunchCurvature);
            return true;
        }

        public UnityEngine.Vector2 PredictLandingPoint(BallRig ball)
        {
            if (!ball)
                throw new ArgumentNullException(nameof(ball));

            return ball.PredictLandingPoint();
        }

        public static VolleyReturnPattern AuthoredDefault() => new VolleyReturnPattern(.6f);

        public VolleyReturnPattern(float cueLeadSeconds)
        {
            if (cueLeadSeconds < .5f)
                throw new ArgumentOutOfRangeException(nameof(cueLeadSeconds));

            CueLeadSeconds = cueLeadSeconds;
        }

        public VolleyTrajectory SelectTrajectory(VolleyAction action)
        {
            SelectedPhase = action switch
            {
                VolleyAction.Dig => VolleyPhase.Dig,
                VolleyAction.Set => VolleyPhase.Set,
                VolleyAction.Spike => VolleyPhase.Spike,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Only authored volley actions have trajectories.")
            };

            HasSelectedTrajectory = true;
            return SelectedPhase switch
            {
                VolleyPhase.Dig => VolleyTrajectory.AuthoredDig,
                VolleyPhase.Set => VolleyTrajectory.AuthoredSet,
                _ => VolleyTrajectory.AuthoredSpike
            };
        }

        Vector2 LaunchDirection => SelectedPhase switch
        {
            VolleyPhase.Dig => new Vector2(0f, 1f),
            VolleyPhase.Set => new Vector2(1f, 1.5f),
            _ => new Vector2(1f, .75f)
        };

        float LaunchForce => SelectedPhase == VolleyPhase.Spike ? 8f : 5f;
        float LaunchCurvature => SelectedPhase == VolleyPhase.Spike ? .15f : 0f;
    }
}
