using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    // Includes buttons created lazily by pause/result/confirmation panels.
    static class UiAudioFeedback
    {
        static readonly HashSet<Button> Bound = new HashSet<Button>();
        static readonly Selectable[] Selectables = new Selectable[512];
        static int lastFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Bound.Clear();
            lastFrame = -1;
            Canvas.willRenderCanvases -= BindVisibleButtons;
            Canvas.willRenderCanvases += BindVisibleButtons;
        }

        static void BindVisibleButtons()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            Bound.RemoveWhere(button => !button);
            int count = Selectable.AllSelectablesNoAlloc(Selectables);
            for (int i = 0; i < count; i++)
            {
                if (!(Selectables[i] is Button button) || !Bound.Add(button)) continue;
                // Button validates input before invoking onClick. Its action may have already
                // hidden the panel by the time this listener runs (Resume/Back/Confirm).
                button.onClick.AddListener(() => GameAudio.Play(GameSound.Click));
            }
        }
    }
}
