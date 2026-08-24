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

        internal void ConfigureForTest(int requiredLaps)
        {
            ConfigureLifecycleForTest(0f, 0f, requiredLaps);
        }

        internal void ConfigureLifecycleForTest(float tutorialSeconds, float countdownSeconds, int requiredLaps)
        {
            Lifecycle = new MinigameLifecycle(tutorialSeconds, countdownSeconds);
            Rules = new EnduranceRules(requiredLaps, Lifecycle);
            LastResult = null;
        }

        internal void Simulate(float dt)
        {
            Lifecycle.Tick(Mathf.Max(0f, dt));
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(Mathf.Max(0f, dt));
        }

        internal void AdvanceToPlayForTest()
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
            Rules.TickPlay(dt);
            if (Rules.Laps >= Rules.RequiredLaps)
                Resolve();
        }
    }
}
