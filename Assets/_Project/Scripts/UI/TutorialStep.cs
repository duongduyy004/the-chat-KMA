using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    [Serializable]
    public sealed class TutorialStep
    {
        [SerializeField] string title;
        [SerializeField] string instruction;
        [SerializeField] Sprite icon;
        [SerializeField] string animationKey;

        public string Title => title ?? string.Empty;
        public string Instruction => instruction ?? string.Empty;
        public Sprite Icon => icon;
        public string AnimationKey => animationKey ?? string.Empty;

        public TutorialStep(string title, string instruction, Sprite icon = null, string animationKey = null)
        {
            this.title = title;
            this.instruction = instruction;
            this.icon = icon;
            this.animationKey = animationKey;
        }
    }
}
