using UnityEngine;

namespace KMA.Gameplay
{
    public abstract class MinigameBase : MonoBehaviour
    {
        public event System.Action<MinigameResult> Completed;
        protected MinigameLifecycle Lifecycle { get; set; }
        public MinigamePhase PresentationPhase => Lifecycle == null ? MinigamePhase.Tutorial : Lifecycle.Phase;

        protected virtual void Awake() => Lifecycle = new MinigameLifecycle(2f, 3f);

        protected virtual void Update()
        {
            Lifecycle.Tick(Time.deltaTime);
            if (Lifecycle.Phase == MinigamePhase.Play)
                TickPlay(Time.deltaTime);
        }

        protected abstract void TickPlay(float dt);

        protected void Finish(MinigameResult result)
        {
            if (Lifecycle.BeginResolve())
                Completed?.Invoke(result);
        }
    }
}
