using System;
using System.Collections.Generic;

namespace KMA.Gameplay.UI
{
    public sealed class GuideNavigator
    {
        public const string NextLabel = "TIẾP";
        public const string StartLabel = "BẮT ĐẦU";
        public const string CloseLabel = "ĐÓNG";
        public const string BackLabel = "QUAY LẠI";
        public const string SkipLabel = "BỎ QUA";

        readonly IReadOnlyList<TutorialStep> pages;

        public GuideNavigator(IReadOnlyList<TutorialStep> pages, GuideMode mode)
        {
            if (pages == null || pages.Count == 0)
                throw new ArgumentException("A guide needs at least one page.", nameof(pages));
            this.pages = pages;
            Mode = mode;
        }

        public GuideMode Mode { get; }
        public int Index { get; private set; }
        public int Count => pages.Count;
        public TutorialStep Current => pages[Index];
        public bool CanGoBack => Index > 0;
        public bool IsLast => Index == pages.Count - 1;
        public bool ShowsSkip => Mode == GuideMode.FirstRun && !IsLast;
        public string PrimaryLabel => !IsLast ? NextLabel : Mode == GuideMode.FirstRun ? StartLabel : CloseLabel;
        public string Progress => $"{Index + 1} / {Count}";

        /// Turns the page; on the last page it returns true, meaning the guide should close.
        public bool Primary()
        {
            if (IsLast)
                return true;
            Index++;
            return false;
        }

        public void Back()
        {
            if (CanGoBack)
                Index--;
        }
    }
}
