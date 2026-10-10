using System.Collections.Generic;

namespace KMA.Gameplay.UI
{
    /// A minigame that can explain itself. BuildGuide runs when the guide opens, so the pages carry
    /// the numbers of the lesson that is loaded at that moment.
    public interface IMinigameGuideSource
    {
        string GuideKey { get; }
        IReadOnlyList<TutorialStep> BuildGuide();
    }
}
