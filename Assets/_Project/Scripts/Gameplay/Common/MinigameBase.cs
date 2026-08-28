using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay
{
    public abstract class MinigameBase : MonoBehaviour, IMinigameHudStateSource
    {
        public event System.Action<MinigameResult> Completed;
        [SerializeField] float tutorialSeconds = 2f;
        [SerializeField] float countdownSeconds = 3f;
        protected MinigameLifecycle Lifecycle { get; set; }
        public MinigamePhase PresentationPhase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;

        protected virtual void Awake() => Lifecycle = new MinigameLifecycle(tutorialSeconds, countdownSeconds);

        protected virtual void Update()
        {
            Lifecycle.Tick(Time.deltaTime);
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(Time.deltaTime);
        }

        public MinigameHudState ReadHudState() => BuildHudState();
        protected virtual MinigameHudState BuildHudState() => MinigameHudState.Empty;
        protected abstract void TickPlay(float dt);

        protected void Finish(MinigameResult result)
        {
            if (Lifecycle.BeginResolve())
                Completed?.Invoke(result);
        }
    }
}
