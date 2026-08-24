using System;

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
    }
}
