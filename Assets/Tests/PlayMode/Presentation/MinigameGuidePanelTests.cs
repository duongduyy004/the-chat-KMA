using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameGuidePanelTests
    {
        static readonly TutorialStep[] Pages =
        {
            new TutorialStep("MỤC TIÊU", "Chạy 150 m."), new TutorialStep("LUẬT", "Đừng bấm sai.")
        };

        GameObject host;
        MinigameGuidePanel panel;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            host = new GameObject("GuideHostRoot");
            panel = MinigameGuidePanel.Create(host.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            Time.timeScale = 1f;
        }

        [Test]
        public void PanelStartsHiddenAndDrawsAboveThePauseMenu()
        {
            Canvas canvas = panel.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(950));
            Assert.That(panel.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight, Is.EqualTo(1f).Within(.001f), "the guide scales like the gameplay canvases");
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.transform.Find("Scrim").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void FirstRunFreezesPagesAndUnfreezesOnBatDau()
        {
            GuideMode? closed = null;
            panel.Closed += mode => closed = mode;
            panel.Open(Pages, GuideMode.FirstRun);

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(panel.CurrentPage.Title, Is.EqualTo("MỤC TIÊU"));
            Assert.That(panel.ProgressText, Is.EqualTo("1 / 2"));
            Assert.That(panel.SkipVisible, Is.True);
            Assert.That(panel.BackInteractable, Is.False);

            panel.transform.Find("Scrim/Card/PrimaryButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.PageIndex, Is.EqualTo(1));
            Assert.That(panel.BackInteractable, Is.True);
            Assert.That(panel.SkipVisible, Is.False);
            Assert.That(panel.PrimaryText, Does.Contain("B"), "the label is the VietText-fixed BẮT ĐẦU");

            panel.PressPrimary();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(closed, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void SkipClosesAFirstRunGuideFromAnyPage()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            panel.PressSkip();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ReviewDoesNotTouchTheTimeScaleAndHasNoSkip()
        {
            panel.Open(Pages, GuideMode.Review);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(panel.SkipVisible, Is.False);
            panel.PressSkip();
            Assert.That(panel.IsOpen, Is.True, "review closes only through ĐÓNG");
            panel.PressPrimary();
            panel.PressPrimary();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void OpeningTwiceKeepsTheFirstGuide()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            panel.PressPrimary();
            panel.Open(Pages, GuideMode.Review);
            Assert.That(panel.Mode, Is.EqualTo(GuideMode.FirstRun));
            Assert.That(panel.PageIndex, Is.EqualTo(1));
        }

        [Test]
        public void EmptyPagesDoNotOpen()
        {
            panel.Open(new TutorialStep[0], GuideMode.FirstRun);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void DestroyingAFirstRunPanelUnfreezes()
        {
            panel.Open(Pages, GuideMode.FirstRun);
            Object.DestroyImmediate(host);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
