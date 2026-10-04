using System.Reflection;
using System.Collections;
using System.Linq;
using UnityEditor;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class UIComponentTests
    {
        [Test]
        public void SafeAreaFitterMapsLandscapeInsetsToBothHorizontalEdges()
        {
            var root = new GameObject("safe-area-fitter", typeof(RectTransform));
            try
            {
                var rectTransform = root.GetComponent<RectTransform>();
                // Offsets only read as insets on a stretched rect; on the default coincident
                // anchors they are sizeDelta, and the fitter deliberately leaves those alone.
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;

                var fitter = root.AddComponent<SafeAreaFitter>();
                var offsets = fitter.CalculateOffsets(
                    new Rect(100f, 0f, 1720f, 1080f),
                    new Vector2(1920f, 1080f));

                Assert.That(offsets.left, Is.EqualTo(100f).Within(.01f));
                Assert.That(offsets.right, Is.EqualTo(100f).Within(.01f));

                fitter.Apply(new Rect(100f, 0f, 1720f, 1080f), new Vector2Int(1920, 1080));
                Assert.That(rectTransform.offsetMin.x, Is.EqualTo(100f).Within(.01f));
                Assert.That(rectTransform.offsetMax.x, Is.EqualTo(-100f).Within(.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BrutalButtonReturnsToRestAfterPointerUp()
        {
            var root = new GameObject("brutal-button", typeof(RectTransform));
            try
            {
                var shadowObject = new GameObject("shadow", typeof(RectTransform));
                shadowObject.transform.SetParent(root.transform, false);
                var shadow = shadowObject.GetComponent<RectTransform>();
                shadow.anchoredPosition = new Vector2(6f, -6f);
                var button = root.AddComponent<BrutalButton>();
                SetPrivateField(button, "shadow", shadow);

                button.SetPressedForTest(true);
                Assert.That(button.CurrentVisualOffset, Is.EqualTo(new Vector2(4f, -4f)));
                Assert.That(shadow.anchoredPosition, Is.EqualTo(Vector2.zero));

                button.SetPressedForTest(false);
                Assert.That(button.CurrentVisualOffset, Is.EqualTo(Vector2.zero));
                Assert.That(shadow.anchoredPosition, Is.EqualTo(new Vector2(6f, -6f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SharedFontAssetsExposeSourceAndAtlasTextures()
        {
            foreach (var assetName in new[] { "Baloo2-ExtraBold", "Nunito-Bold" })
            {
                var path = $"Assets/_Project/Fonts/{assetName}.asset";
                var font = AssetDatabase.LoadMainAssetAtPath(path);
                Assert.That(font, Is.Not.Null, path);

                var source = font.GetType().GetProperty("sourceFontFile")?.GetValue(font);
                Assert.That(source, Is.Not.Null, $"{assetName} source font");

                var atlases = font.GetType().GetProperty("atlasTextures")?.GetValue(font) as IEnumerable;
                Assert.That(atlases, Is.Not.Null, $"{assetName} atlas textures");
                var atlasCount = 0;
                foreach (var atlas in atlases)
                {
                    atlasCount++;
                    Assert.That(atlas, Is.Not.Null, $"{assetName} atlas {atlasCount}");
                }
                Assert.That(atlasCount, Is.GreaterThan(0), $"{assetName} atlas count");
            }
        }

        [Test]
        public void HeartBarClampsToFiveSlotsAndRendersFilledAndEmptyStates()
        {
            var root = new GameObject("heart-bar");
            try
            {
                var heartBar = root.AddComponent<HeartBar>();
                var slots = new Image[5];
                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = new GameObject($"heart-{index}", typeof(RectTransform));
                    slot.transform.SetParent(root.transform, false);
                    slots[index] = slot.AddComponent<Image>();
                }
                SetPrivateField(heartBar, "slots", slots);

                heartBar.SetHearts(3);

                Assert.That(heartBar.CurrentHearts, Is.EqualTo(3));
                Assert.That(slots[0].color, Is.EqualTo(heartBar.FilledColor));
                Assert.That(slots[2].color, Is.EqualTo(heartBar.FilledColor));
                Assert.That(slots[3].color, Is.EqualTo(heartBar.EmptyColor));

                heartBar.SetHearts(99);
                Assert.That(heartBar.CurrentHearts, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapPresentationPlacesThreeOrderedSubjectsOnJourneyPath()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 1080f);
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                var grid = root.transform.Find("S5MapPresentation/Content/SelectionGrid")
                    .GetComponent<GridLayoutGroup>();
                Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(grid.constraintCount, Is.EqualTo(3));
                Assert.That(grid.GetComponentsInChildren<MapNodeView>(true), Has.Length.EqualTo(3));
                Assert.That(root.GetComponentsInChildren<MapNodeView>(true), Has.Length.EqualTo(3));
                Assert.That(root.GetComponentsInChildren<MapNodeView>(true).Select(node => node.SubjectId),
                    Is.EqualTo(new[] { SubjectId.Sprint, SubjectId.Volleyball, SubjectId.Football }));
                Assert.That(grid.cellSize.y, Is.GreaterThanOrEqualTo(220f));
                Assert.That(root.transform.Find("S5MapPresentation/Content/ProgressSection"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapHeaderMakesNavigationTitleAndLivesReadableAtLandscapeScale()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform header = root.transform.Find("S5MapPresentation/Content/Header");
                Assert.That(header.GetComponent<Image>(), Is.Not.Null);
                Assert.That(header.Find("Divider"), Is.Not.Null);

                LayoutElement back = header.Find("BackButton").GetComponent<LayoutElement>();
                Assert.That(back.preferredWidth, Is.GreaterThanOrEqualTo(80f));
                Assert.That(back.preferredHeight, Is.GreaterThanOrEqualTo(80f));

                TMP_Text title = header.Find("Heading/TitleContainer/Title").GetComponent<TMP_Text>();
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(52));
                Assert.That(title.font, Is.SameAs(VietTypography.Library.bold));
                Assert.That(title.extraPadding, Is.True);
                TMP_Text subtitle = header.Find("Heading/SubtitleContainer/Subtitle").GetComponent<TMP_Text>();
                Assert.That(subtitle.text, Is.EqualTo("Chọn một môn để bắt đầu"));
                Assert.That(subtitle.fontSize, Is.GreaterThanOrEqualTo(24));

                Transform livesPanel = header.Find("LivesPanel");
                Assert.That(livesPanel, Is.Not.Null);
                Assert.That(livesPanel.GetComponent<Image>(), Is.Not.Null);
                TMP_Text lives = livesPanel.Find("LivesLabelContainer/LivesLabel").GetComponent<TMP_Text>();
                Assert.That(lives.text, Is.EqualTo("Lượt thi: 5/5"));
                Assert.That(lives.fontSize, Is.GreaterThanOrEqualTo(24));
                foreach (Image heart in livesPanel.GetComponentInChildren<HeartBar>(true)
                             .GetComponentsInChildren<Image>(true))
                {
                    Assert.That(heart.sprite, Is.Not.Null, heart.name);
                    Assert.That(heart.preserveAspect, Is.True, heart.name);
                    Assert.That(heart.GetComponent<LayoutElement>().preferredWidth,
                        Is.GreaterThanOrEqualTo(32f), heart.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void JourneyLessonLabelsMeetContrastOnTheirButtonSurface()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform lesson = root.transform.Find("S5MapPresentation/Content/JourneyLessons/Lesson1");
                Color foreground = lesson.GetComponentInChildren<TMP_Text>(true).color;
                Color background = lesson.GetComponent<Image>().color;
                Assert.That(ContrastRatio(foreground, background), Is.GreaterThanOrEqualTo(4.5f),
                    "Unlocked lesson copy must remain readable on its bright card surface.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static float ContrastRatio(Color foreground, Color background)
        {
            float first = RelativeLuminance(foreground);
            float second = RelativeLuminance(background);
            return (Mathf.Max(first, second) + .05f) / (Mathf.Min(first, second) + .05f);
        }

        static float RelativeLuminance(Color color)
        {
            static float Linear(float value) => value <= .04045f
                ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
            return .2126f * Linear(color.r) + .7152f * Linear(color.g) + .0722f * Linear(color.b);
        }

        [Test]
        public void MapCardsCommunicateCompletedAndLockedStatesWithoutColorAlone()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var session = CompleteSprintJourney();
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, session);

                MapNodeView completed = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
                MapNodeView football = screen.Nodes.Single(node => node.SubjectId == SubjectId.Football);

                Assert.That(completed.transform.Find("StatusContainer/Status").GetComponent<TMP_Text>().text,
                    Is.EqualTo("HOÀN THÀNH"));
                Assert.That(completed.transform.Find("DetailContainer").gameObject.activeSelf, Is.True);
                Assert.That(completed.transform.Find("ActionHint").GetComponent<TMP_Text>().text,
                    Is.EqualTo("THI"));
                Assert.That(football.transform.Find("StatusContainer/Status").GetComponent<TMP_Text>().text,
                    Is.EqualTo("CHƯA MỞ KHÓA"));
                Assert.That(football.DetailText, Is.EqualTo("CHƯA MỞ KHÓA"));
                Assert.That(football.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(football.transform.Find("ActionHint").GetComponent<TMP_Text>().text,
                    Is.Empty);

                Assert.That(completed.GetComponent<BrutalButton>(), Is.Not.Null);
                Assert.That(football.GetComponent<BrutalButton>(), Is.Not.Null);
                Assert.That(football.GetComponent<Button>(), Is.Not.Null);
                Assert.That(football.GetComponent<Button>().interactable, Is.False);

                var iconSprites = screen.Nodes.Select(node =>
                {
                    Image icon = node.transform.Find("CardHeader/SportIcon/IconGlyph")
                        .GetComponent<Image>();
                    Assert.That(icon.sprite, Is.Not.Null, node.name);
                    Assert.That(icon.preserveAspect, Is.True, node.name);
                    return icon.sprite;
                }).ToArray();
                Assert.That(iconSprites.Distinct().Count(), Is.EqualTo(3));

                foreach (MapNodeView node in screen.Nodes)
                {
                    TMP_Text title = node.transform.Find("CardHeader/TitleContainer/Title").GetComponent<TMP_Text>();
                    Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(30), node.name);
                    Assert.That(node.GetComponent<VerticalLayoutGroup>().padding.left,
                        Is.GreaterThanOrEqualTo(20), node.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FutureSubjectsAreLockedUntilPreviousExamPasses()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Assert.That(screen.Nodes[0].IsInteractable, Is.True);
                foreach (MapNodeView future in screen.Nodes.Where(node => node.SubjectId != SubjectId.Sprint))
                {
                    Assert.That(future.IsInteractable, Is.False, future.DisplayName);
                    Assert.That(future.transform.Find("StatusContainer/Status").GetComponent<TMP_Text>().text,
                        Is.EqualTo("CHƯA MỞ KHÓA"));
                }

            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapCardsUseRoundedSurfacesAndControlledDepth()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform grid = root.transform.Find("S5MapPresentation/Content/SelectionGrid");
                foreach (var node in grid.GetComponentsInChildren<MapNodeView>(true))
                {
                    Transform card = node.transform;
                    Image surface = card.GetComponent<Image>();
                    Assert.That(surface.sprite, Is.Not.Null, card.name);
                    Assert.That(surface.type, Is.EqualTo(Image.Type.Sliced), card.name);
                    Shadow shadow = card.GetComponent<Shadow>();
                    Assert.That(shadow, Is.Not.Null, card.name);
                    Assert.That(Mathf.Abs(shadow.effectDistance.x), Is.LessThanOrEqualTo(8f), card.name);
                }

            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapLayoutPreservesCardTitleLivesAndUpcomingLabelSpace()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform sprint = root.transform.Find(
                    "S5MapPresentation/Content/SelectionGrid/SprintNode");
                HorizontalLayoutGroup cardHeader = sprint.Find("CardHeader")
                    .GetComponent<HorizontalLayoutGroup>();
                Assert.That(cardHeader.childForceExpandWidth, Is.False);
                Assert.That(cardHeader.childForceExpandHeight, Is.False);
                LayoutElement cardTitle = sprint.Find("CardHeader/TitleContainer")
                    .GetComponent<LayoutElement>();
                Assert.That(cardTitle.preferredHeight, Is.GreaterThanOrEqualTo(56f));
                Assert.That(cardTitle.flexibleWidth, Is.GreaterThan(0f));

                LayoutElement livesLabel = root.transform.Find(
                        "S5MapPresentation/Content/Header/LivesPanel/LivesLabelContainer")
                    .GetComponent<LayoutElement>();
                Assert.That(livesLabel.preferredHeight, Is.GreaterThanOrEqualTo(36f));
                Assert.That(livesLabel.preferredWidth, Is.GreaterThanOrEqualTo(150f));
                Assert.That(livesLabel.GetComponentInChildren<TMP_Text>().enableWordWrapping, Is.False);

                Transform lessons = root.transform.Find("S5MapPresentation/Content/JourneyLessons");
                Assert.That(lessons, Is.Not.Null);
                Assert.That(lessons.Find("Lesson1").GetComponent<LayoutElement>().preferredHeight,
                    Is.GreaterThanOrEqualTo(30f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AvailableSportsCardsRenderWithoutLockPictograms()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Assert.That(screen.Nodes[0].IsInteractable, Is.True, screen.Nodes[0].DisplayName);
                Assert.That(screen.Nodes[1].IsInteractable, Is.False, screen.Nodes[1].DisplayName);
                Assert.That(screen.Nodes[2].IsInteractable, Is.False, screen.Nodes[2].DisplayName);
                foreach (MapNodeView node in screen.Nodes)
                    Assert.That(node.transform.Find("LockIcon"), Is.Null, node.DisplayName);

            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapSecondaryTextRemainsReadableOnCompactLandscapeScreens()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform sprint = root.transform.Find(
                    "S5MapPresentation/Content/SelectionGrid/SprintNode");
                Assert.That(sprint.Find("StatusContainer/Status").GetComponent<TMP_Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));
                Assert.That(sprint.Find("DetailContainer/Detail").GetComponent<TMP_Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));
                Assert.That(sprint.Find("ActionHint").GetComponent<TMP_Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));

                foreach (TMP_Text line in root.transform.Find("S5MapPresentation/Content/JourneyLessons")
                             .GetComponentsInChildren<TMP_Text>(true))
                    Assert.That(line.fontSize, Is.GreaterThanOrEqualTo(16), line.name);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapNodeAvailabilityTransitionsKeepInteractionAndVisualStateSynchronized()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());
                MapNodeView node = screen.Nodes.Single(candidate =>
                    candidate.SubjectId == SubjectId.Sprint);
                Button button = node.GetComponent<Button>();
                BrutalButton feedback = node.GetComponent<BrutalButton>();
                TMP_Text status = node.transform.Find("StatusContainer/Status").GetComponent<TMP_Text>();
                TMP_Text action = node.transform.Find("ActionHint").GetComponent<TMP_Text>();

                node.SetAvailability(false, "TẠM KHÓA");

                Assert.That(button.interactable, Is.False);
                Assert.That(feedback.enabled, Is.False);
                Assert.That(status.text, Is.EqualTo("TẠM KHÓA"));
                Assert.That(node.DetailText, Is.EqualTo("TẠM KHÓA"));
                Assert.That(node.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(action.text, Is.Empty);

                node.SetAvailability(true, "TẠM KHÓA");

                Assert.That(button.interactable, Is.True);
                Assert.That(feedback.enabled, Is.True);
                Assert.That(status.text, Is.EqualTo("SẴN SÀNG"));
                Assert.That(node.DetailText, Is.EqualTo("SẴN SÀNG"));
                Assert.That(node.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(action.text, Is.EqualTo("THI"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapPaletteUsesBlueForSubjectsGreenForCompletionAndGoldForReadyState()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var session = CompleteSprintJourney();
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, session);

                MapNodeView sprint = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
                Color sprintAccent = sprint.transform.Find("CardHeader/SportIcon")
                    .GetComponent<Image>().color;
                Assert.That(sprintAccent.b, Is.GreaterThan(sprintAccent.r),
                    "Subject accents should not use warning red.");

                Color completed = sprint.GetComponent<Outline>().effectColor;
                Assert.That(completed.g, Is.GreaterThan(completed.r));
                Assert.That(completed.g, Is.GreaterThan(completed.b));

            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MapRebuildRecreatesDestroyedProceduralSportSprites()
        {
            var firstRoot = new GameObject("first-map", typeof(RectTransform));
            var secondRoot = new GameObject("second-map", typeof(RectTransform));
            try
            {
                var firstScreen = firstRoot.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(firstScreen, new GameSession());
                Image firstIcon = firstScreen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint)
                    .transform.Find("CardHeader/SportIcon/IconGlyph").GetComponent<Image>();
                Sprite destroyedSprite = firstIcon.sprite;
                Texture2D destroyedTexture = destroyedSprite.texture;
                Object.DestroyImmediate(firstRoot);
                Object.DestroyImmediate(destroyedSprite);
                Object.DestroyImmediate(destroyedTexture);

                var secondScreen = secondRoot.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(secondScreen, new GameSession());
                Sprite rebuiltSprite = secondScreen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint)
                    .transform.Find("CardHeader/SportIcon/IconGlyph").GetComponent<Image>().sprite;

                Assert.That(rebuiltSprite, Is.Not.Null);
                Assert.That(rebuiltSprite.name, Is.EqualTo("SportIcon_Sprint"));
            }
            finally
            {
                if (firstRoot != null)
                    Object.DestroyImmediate(firstRoot);
                Object.DestroyImmediate(secondRoot);
            }
        }

        static GameSession CompleteSprintJourney()
        {
            var session = new GameSession();
            Complete(session, "sprint_learn");
            Complete(session, "sprint_practice");
            Complete(session, "sprint_exam");
            return session;
        }

        static void Complete(GameSession session, string id)
        {
            ChallengeDefinition definition = session.Journey.Catalog.Get(id);
            Assert.That(session.TryStartChallenge(id, ChallengeAttemptMode.Journey, definition.Difficulty,
                out ChallengeAttemptContext context), Is.True);
            session.SubmitChallengeResult(new ChallengeAttemptResult(context, true,
                new ChallengeMetrics(completedTargets: definition.TargetCount),
                definition.Kind == ChallengeKind.Exam ? new MinigameResult(true, 8f, Rank.A) : null));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
