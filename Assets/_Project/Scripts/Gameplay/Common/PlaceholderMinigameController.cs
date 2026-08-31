using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class PlaceholderMinigameController : MinigameBase
    {
        object subject;
        bool configured;

        public object Subject => subject;
        public bool IsConfigured => configured;

        public void ConfigureForTest(object value)
        {
            subject = value;
            configured = true;
            Lifecycle.Tick(999f);
            Lifecycle.Tick(999f);
        }

        public void DebugPass() => Finish(new MinigameResult(true, 6f, Rank.C));
        public void DebugFail() => Finish(new MinigameResult(false, 0f, Rank.F));
        protected override void TickPlay(float dt) { }
    }
}
