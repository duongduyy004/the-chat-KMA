using System;
using KMA.Gameplay;

namespace KMA.Tests.Gameplay.Progression
{
    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
        public void Advance(TimeSpan by) => UtcNow += by;
    }
}
