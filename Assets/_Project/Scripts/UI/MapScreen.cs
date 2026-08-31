using System;
using KMA.Gameplay;

namespace KMA.Gameplay.UI
{
    public sealed class MapScreen : ScreenBase
    {
        public event Action<SubjectId> SubjectRequested;
        public event Action BossRequested;
        public bool BossUnlocked { get; private set; }

        public void SetBossUnlocked(bool unlocked) => BossUnlocked = unlocked;

        public void SelectSubject(SubjectId subject) => SubjectRequested?.Invoke(subject);
        public void SelectBoss()
        {
            if (BossUnlocked)
                BossRequested?.Invoke();
        }
    }
}
