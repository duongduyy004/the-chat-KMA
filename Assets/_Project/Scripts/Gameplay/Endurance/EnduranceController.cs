using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class EnduranceController : MinigameBase
    {
        public EnduranceRules Rules { get; private set; }
        public MinigamePhase Phase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;
        public MinigameResult LastResult { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Rules = new EnduranceRules(3, Lifecycle);
        }

        public void ConfigureForTest(int requiredLaps)
        {
            Lifecycle = new MinigameLifecycle(0f, 0f);
            Rules = new EnduranceRules(requiredLaps, Lifecycle);
            LastResult = null;
        }

        public void AdvanceToPlayForTest()
        {
            Rules.AdvanceToPlayForTest();
        }

        public void Dispatch(AuthoredBeat beat)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Dispatch(beat);
        }

        public void Tap(double inputDsp, double beatDsp)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Tap(inputDsp, beatDsp);
        }

        public void EndHold(float beatsHeld)
        {
            if (Phase == MinigamePhase.Play)
                Rules.EndHold(beatsHeld);
        }

        public void Swipe(SwipeDirection direction)
        {
            if (Phase == MinigamePhase.Play)
                Rules.Swipe(direction);
        }

        public void Resolve()
        {
            if (Phase != MinigamePhase.Play)
                return;

            LastResult = Rules.BuildResult();
            Finish(LastResult);
        }

        protected override void TickPlay(float dt)
        {
            Rules.Tick(dt);
            if (Rules.Laps >= Rules.RequiredLaps)
                Resolve();
        }
    }
}
