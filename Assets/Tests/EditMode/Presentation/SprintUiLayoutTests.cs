using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SprintUiLayoutTests
    {
        static IEnumerable<TestCaseData> LandscapeSafeAreas()
        {
            yield return new TestCaseData(new Rect(0f, 0f, 1920f, 1080f)).SetName("16x9");
            yield return new TestCaseData(new Rect(0f, 0f, 1728f, 1080f)).SetName("16x10");
            yield return new TestCaseData(new Rect(0f, 0f, 2160f, 1080f)).SetName("18x9");
            yield return new TestCaseData(new Rect(0f, 0f, 2340f, 1080f)).SetName("19.5x9");
            yield return new TestCaseData(new Rect(0f, 0f, 2400f, 1080f)).SetName("20x9");
            yield return new TestCaseData(new Rect(120f, 0f, 2232f, 1080f)).SetName("20x9-notch");
        }

        [TestCase(0, 0.125f)]
        [TestCase(1, 0.375f)]
        [TestCase(2, 0.625f)]
        [TestCase(3, 0.875f)]
        public void LaneCenter01_CentersFourEqualLanes(int lane, float expected) =>
            Assert.That(SprintUiLayout.LaneCenter01(lane, 4), Is.EqualTo(expected).Within(.0001f));

        [TestCase(69.9f, false)]
        [TestCase(70f, true)]
        [TestCase(100f, true)]
        public void FinishVisible_UsesApprovedThreshold(float distance, bool expected) =>
            Assert.That(SprintUiLayout.FinishVisible(distance), Is.EqualTo(expected));

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void NoTwoElementsOverlapDuringTheStartState(Rect safe)
        {
            SprintUiLayout.NamedRect[] rects = SprintUiLayout.StartStateRects(safe);
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i + 1; j < rects.Length; j++)
                {
                    Assert.That(rects[i].Rect.Overlaps(rects[j].Rect), Is.False,
                        $"{rects[i].Name} overlaps {rects[j].Name} at {safe.width}x{safe.height}");
                }
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void NoTwoElementsOverlapDuringTheRace(Rect safe)
        {
            SprintUiLayout.NamedRect[] rects = SprintUiLayout.RaceRects(safe);
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i + 1; j < rects.Length; j++)
                {
                    Assert.That(rects[i].Rect.Overlaps(rects[j].Rect), Is.False,
                        $"{rects[i].Name} overlaps {rects[j].Name} at {safe.width}x{safe.height}");
                }
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void EveryElementStaysInsideTheSafeAreaWithEdgePadding(Rect safe)
        {
            float padX = safe.width * .02f;
            float padY = safe.height * .01f;
            foreach (SprintUiLayout.NamedRect named in SprintUiLayout.StartStateRects(safe))
            {
                Assert.That(named.Rect.xMin, Is.GreaterThanOrEqualTo(safe.xMin + padX - .01f), $"{named.Name} left");
                Assert.That(named.Rect.xMax, Is.LessThanOrEqualTo(safe.xMax - padX + .01f), $"{named.Name} right");
                Assert.That(named.Rect.yMin, Is.GreaterThanOrEqualTo(safe.yMin + padY - .01f), $"{named.Name} bottom");
                Assert.That(named.Rect.yMax, Is.LessThanOrEqualTo(safe.yMax - padY + .01f), $"{named.Name} top");
            }
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void ControlsAreSymmetricEqualSizedAndNeverCrossTheMidpoint(Rect safe)
        {
            Rect left = SprintUiLayout.ControlRect(safe, true);
            Rect right = SprintUiLayout.ControlRect(safe, false);
            Assert.That(left.width, Is.EqualTo(right.width).Within(.01f));
            Assert.That(left.height, Is.EqualTo(right.height).Within(.01f));
            Assert.That(left.yMin, Is.EqualTo(right.yMin).Within(.01f));
            Assert.That(left.xMin - safe.xMin, Is.EqualTo(safe.xMax - right.xMax).Within(.01f));
            Assert.That(left.xMax, Is.LessThan(safe.center.x));
            Assert.That(right.xMin, Is.GreaterThan(safe.center.x));
        }

        [TestCaseSource(nameof(LandscapeSafeAreas))]
        public void ElementSizesDependOnHeightOnly(Rect safe)
        {
            var reference = new Rect(0f, 0f, 1920f, safe.height);
            Assert.That(SprintUiLayout.ControlRect(safe, true).size,
                Is.EqualTo(SprintUiLayout.ControlRect(reference, true).size));
            Assert.That(SprintUiLayout.ScoreboardRect(safe).size,
                Is.EqualTo(SprintUiLayout.ScoreboardRect(reference).size));
            Assert.That(SprintUiLayout.PauseRect(safe).size,
                Is.EqualTo(SprintUiLayout.PauseRect(reference).size));
        }

        [Test]
        public void PauseIsSquareAndScoreboardSitsAboveTheTopLane()
        {
            var safe = new Rect(0f, 0f, 1920f, 1080f);
            Rect pause = SprintUiLayout.PauseRect(safe);
            Assert.That(pause.width, Is.EqualTo(pause.height).Within(.01f));
            Assert.That(SprintUiLayout.ScoreboardRect(safe).yMin,
                Is.GreaterThan(safe.yMin + safe.height * .76f), "scoreboard must clear the top lane");
        }

        [Test]
        public void RaceRectsAreTheStartStateRectsWithoutCountdownAndInstruction()
        {
            var safe = new Rect(0f, 0f, 1920f, 1080f);
            var raceNames = new List<string>();
            foreach (SprintUiLayout.NamedRect named in SprintUiLayout.RaceRects(safe))
                raceNames.Add(named.Name);
            Assert.That(raceNames, Does.Not.Contain("Countdown"));
            Assert.That(raceNames, Does.Not.Contain("Instruction"));
            Assert.That(raceNames, Has.Count.EqualTo(6));
            Assert.That(SprintUiLayout.StartStateRects(safe), Has.Length.EqualTo(8));
        }
    }
}
