using System;

namespace KMA.Gameplay
{
    public readonly struct AuthoredBeat
    {
        public AuthoredBeat(BeatEvent beat, bool endsLap = false)
        {
            Beat = beat;
            EndsLap = endsLap;
        }

        public BeatEvent Beat { get; }
        public bool EndsLap { get; }
    }

    public sealed class LapPattern
    {
        readonly AuthoredBeat[] events;

        public LapPattern(AuthoredBeat[] events)
        {
            if (events == null || events.Length == 0)
                throw new ArgumentException("An authored lap pattern must contain at least one event.", nameof(events));

            this.events = (AuthoredBeat[])events.Clone();
        }

        public static LapPattern Default => new LapPattern(new[]
        {
            new AuthoredBeat(BeatEvent.Tap),
            new AuthoredBeat(BeatEvent.Breath),
            new AuthoredBeat(BeatEvent.Jump),
            new AuthoredBeat(BeatEvent.Slide, endsLap: true)
        });

        public AuthoredBeat[] Events => (AuthoredBeat[])events.Clone();
    }
}
