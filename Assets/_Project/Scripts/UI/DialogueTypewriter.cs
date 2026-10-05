using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Counts how many characters of a dialogue line are visible as time passes.
    public sealed class DialogueTypewriter
    {
        readonly float charactersPerSecond;
        float elapsed;
        int total;

        public DialogueTypewriter(float charactersPerSecond = 45f)
        {
            if (charactersPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(charactersPerSecond));
            this.charactersPerSecond = charactersPerSecond;
        }

        public int VisibleCharacters { get; private set; }
        public bool IsDone => VisibleCharacters >= total;

        public void Begin(int totalCharacters)
        {
            total = Mathf.Max(0, totalCharacters);
            elapsed = 0f;
            VisibleCharacters = 0;
        }

        public void Tick(float deltaTime)
        {
            if (IsDone || deltaTime <= 0f) return;
            elapsed += deltaTime;
            VisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(elapsed * charactersPerSecond));
        }

        public void Complete() => VisibleCharacters = total;
    }
}
