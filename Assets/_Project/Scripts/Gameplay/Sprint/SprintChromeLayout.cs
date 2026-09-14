using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    /// Keeps Sprint chrome positioned from the live safe-area rect.
    /// Build() registers each element with the layout function that produces its rect;
    /// this component re-applies them whenever the safe area's size changes, so the HUD is
    /// correct regardless of when the Canvas first performs layout, and after rotation.
    public sealed class SprintChromeLayout : MonoBehaviour
    {
        readonly List<(RectTransform target, Func<Rect, Rect> layout)> elements = new();
        readonly List<Action<Rect>> actions = new();
        RectTransform safeArea;
        Vector2 appliedSize = new Vector2(-1f, -1f);

        public int ElementCount => elements.Count;
        public int ActionCount => actions.Count;
        public bool HasAppliedLayout => appliedSize.x > 0f && appliedSize.y > 0f;

        public void Bind(RectTransform safeAreaRect)
        {
            safeArea = safeAreaRect;
            appliedSize = new Vector2(-1f, -1f);
        }

        public void Register(RectTransform target, Func<Rect, Rect> layout)
        {
            if (target != null && layout != null)
                elements.Add((target, layout));
        }

        /// Registers arbitrary safe-area-derived work (e.g. recomputing a corner radius or a
        /// sizeDelta) to re-run whenever the safe area's size changes, alongside the rect pass.
        public void Register(Action<Rect> apply)
        {
            if (apply != null)
                actions.Add(apply);
        }

        void LateUpdate() => ApplyIfChanged();

        public void ApplyIfChanged()
        {
            if (safeArea == null)
                return;

            Rect rect = safeArea.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var size = new Vector2(rect.width, rect.height);
            if (size == appliedSize)
                return;

            appliedSize = size;
            var safe = new Rect(0f, 0f, rect.width, rect.height);
            for (int i = 0; i < elements.Count; i++)
            {
                var (target, layout) = elements[i];
                if (target != null)
                    SprintFestivalPresentation.ApplyRectPublic(target, safe, layout(safe));
            }

            for (int i = 0; i < actions.Count; i++)
                actions[i]?.Invoke(safe);
        }
    }
}
