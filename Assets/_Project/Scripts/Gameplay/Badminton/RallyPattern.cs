using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public readonly struct RallyExchange
    {
        public RallyExchange(float timing, float windCue, Vector2 trajectory)
        {
            Timing = timing;
            WindCue = windCue;
            Trajectory = trajectory;
        }

        public float Timing { get; }
        public float WindCue { get; }
        public Vector2 Trajectory { get; }
    }

    public sealed class RallyPattern
    {
        static readonly RallyExchange[] AuthoredExchanges =
        {
            new RallyExchange(.4f, .15f, new Vector2(.65f, .45f)),
            new RallyExchange(.8f, -.2f, new Vector2(-.55f, .6f)),
            new RallyExchange(1.2f, .1f, new Vector2(.45f, .8f)),
            new RallyExchange(1.6f, -.15f, new Vector2(-.7f, .5f)),
            new RallyExchange(2f, .05f, new Vector2(.35f, .7f))
        };

        public RallyPattern(IReadOnlyList<RallyExchange> exchanges)
        {
            if (exchanges == null || exchanges.Count == 0)
                throw new ArgumentException("An authored rally requires at least one exchange.", nameof(exchanges));
            Exchanges = exchanges;
        }

        public IReadOnlyList<RallyExchange> Exchanges { get; }
        public static RallyPattern AuthoredDefault() => new RallyPattern(AuthoredExchanges);

        public Vector2 TrajectoryAt(int exchangeIndex)
        {
            if (exchangeIndex < 0 || exchangeIndex >= Exchanges.Count)
                throw new ArgumentOutOfRangeException(nameof(exchangeIndex));
            return Exchanges[exchangeIndex].Trajectory;
        }
    }
}
