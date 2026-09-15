using System.Reflection;
using System.Collections;
using System.Linq;
using UnityEditor;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
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
        public void MapPresentationPlacesSevenSubjectsAndProgressInUniformEightCellGrid()
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
                Assert.That(grid.constraintCount, Is.EqualTo(4));
                Assert.That(grid.transform.childCount, Is.EqualTo(8));
                Assert.That(grid.transform.GetChild(7).name, Is.EqualTo("ProgressCard"));
                Assert.That(grid.transform.GetChild(7).GetComponent<MapNodeView>(), Is.Null);
                Assert.That(root.GetComponentsInChildren<MapNodeView>(true), Has.Length.EqualTo(7));
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

                Text title = header.Find("Heading/TitleContainer/Title").GetComponent<Text>();
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(52));
                Assert.That(title.fontStyle, Is.EqualTo(FontStyle.Bold));
                Text subtitle = header.Find("Heading/SubtitleContainer/Subtitle").GetComponent<Text>();
                Assert.That(subtitle.text, Is.EqualTo("Chọn một môn để bắt đầu"));
                Assert.That(subtitle.fontSize, Is.GreaterThanOrEqualTo(24));

                Transform livesPanel = header.Find("LivesPanel");
                Assert.That(livesPanel, Is.Not.Null);
                Assert.That(livesPanel.GetComponent<Image>(), Is.Not.Null);
                Text lives = livesPanel.Find("LivesLabelContainer/LivesLabel").GetComponent<Text>();
                Assert.That(lives.text, Is.EqualTo("LƯỢT: 5/5"));
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
        public void MapCardsCommunicateCompletedReadyAndLockedStatesWithoutColorAlone()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var session = new GameSession();
                session.StartSubject(SubjectId.Sprint);
                session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, session);

                MapNodeView completed = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
                MapNodeView ready = screen.Nodes.Single(node => node.SubjectId == SubjectId.Endurance);
                MapNodeView locked = screen.Nodes.Single(node => node.SubjectId == SubjectId.Basketball);

                Assert.That(completed.transform.Find("StatusContainer/Status").GetComponent<Text>().text,
                    Is.EqualTo("✓  HOÀN THÀNH"));
                Assert.That(completed.transform.Find("DetailContainer").gameObject.activeSelf, Is.True);
                Assert.That(completed.transform.Find("ActionHint").GetComponent<Text>().text,
                    Is.EqualTo("THI →"));
                Assert.That(ready.DetailText, Is.EqualTo("SẴN SÀNG"));
                Assert.That(ready.transform.Find("StatusContainer/Status").GetComponent<Text>().text,
                    Is.EqualTo("SẴN SÀNG"));
                Assert.That(ready.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(ready.transform.Find("ActionHint").GetComponent<Text>().text,
                    Is.EqualTo("THI →"));
                RectTransform readyStatus = ready.transform.Find("StatusContainer").GetComponent<RectTransform>();
                RectTransform readyAction = ready.transform.Find("ActionHint").GetComponent<RectTransform>();
                Assert.That(readyStatus.anchorMin.y, Is.EqualTo(0f));
                Assert.That(readyStatus.anchorMax.y, Is.EqualTo(0f));
                Assert.That(readyAction.anchorMin.y, Is.EqualTo(0f));
                Assert.That(readyAction.anchorMax.y, Is.EqualTo(0f));
                Assert.That(readyStatus.offsetMin.y, Is.EqualTo(readyAction.offsetMin.y).Within(.1f));
                Assert.That(locked.transform.Find("StatusContainer/Status").GetComponent<Text>().text,
                    Is.EqualTo("🔒  ĐANG PHÁT TRIỂN"));
                Assert.That(locked.DetailText, Is.EqualTo("ĐANG PHÁT TRIỂN"));
                Assert.That(locked.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(locked.transform.Find("ActionHint"), Is.Null);

                Assert.That(completed.GetComponent<BrutalButton>(), Is.Not.Null);
                Assert.That(ready.GetComponent<BrutalButton>(), Is.Not.Null);
                Assert.That(locked.GetComponent<BrutalButton>(), Is.Null);
                Assert.That(locked.GetComponent<Button>(), Is.Null);

                var iconSprites = screen.Nodes.Select(node =>
                {
                    Image icon = node.transform.Find("CardHeader/SportIcon/IconGlyph")
                        .GetComponent<Image>();
                    Assert.That(icon.sprite, Is.Not.Null, node.name);
                    Assert.That(icon.preserveAspect, Is.True, node.name);
                    return icon.sprite;
                }).ToArray();
                Assert.That(iconSprites.Distinct().Count(), Is.EqualTo(7));

                foreach (MapNodeView node in screen.Nodes)
                {
                    Text title = node.transform.Find("CardHeader/TitleContainer/Title").GetComponent<Text>();
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
        public void ProgressCardShowsFractionProportionalFillAndActionableRemainingCount()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var session = new GameSession();
                session.StartSubject(SubjectId.Sprint);
                session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, session);

                Transform progress = root.transform.Find(
                    "S5MapPresentation/Content/SelectionGrid/ProgressCard");
                Color progressColor = progress.GetComponent<Image>().color;
                Assert.That(progressColor.r, Is.EqualTo(13f / 255f).Within(.001f));
                Assert.That(progressColor.g, Is.EqualTo(57f / 255f).Within(.001f));
                Assert.That(progressColor.b, Is.EqualTo(92f / 255f).Within(.001f));
                Text fraction = progress.Find("ProgressFractionContainer/ProgressFraction")
                    .GetComponent<Text>();
                Assert.That(fraction.text, Is.EqualTo("1 / 7"));
                Assert.That(fraction.fontSize, Is.GreaterThanOrEqualTo(44));
                RectTransform track = progress.Find("ProgressTrack").GetComponent<RectTransform>();
                Assert.That(track.GetComponent<LayoutElement>().preferredHeight,
                    Is.GreaterThanOrEqualTo(28f));
                Assert.That(track.Find("Fill").GetComponent<RectTransform>().anchorMax.x,
                    Is.EqualTo(1f / 7f).Within(.001f));
                Text hint = progress.Find("UnlockHintContainer/UnlockHint").GetComponent<Text>();
                Assert.That(hint.text,
                    Is.EqualTo("Hoàn thành thêm 6 môn để mở thử thách tiếp theo."));
                Assert.That(hint.fontSize, Is.GreaterThanOrEqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LockedFooterUsesInformationalSurfacesInsteadOfDisabledButtons()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                Transform future = root.transform.Find("S5MapPresentation/Content/FutureRow");
                Assert.That(future.GetComponentsInChildren<Button>(true), Is.Empty);
                Assert.That(future.GetComponentsInChildren<Text>(true).Select(text => text.text),
                    Is.EquivalentTo(new[] { "SẮP RA MẮT", "Hít đất", "Nhịp điệu", "Bơi lội" }));

                Transform challenge = root.transform.Find("S5MapPresentation/Content/BossChallenge");
                Assert.That(challenge, Is.Not.Null);
                Assert.That(challenge.GetComponent<Button>(), Is.Null);
                Text message = challenge.Find("Label").GetComponent<Text>();
                Assert.That(message.text,
                    Is.EqualTo("🔒  Hoàn thành thêm các môn để mở thử thách tiếp theo."));
                Assert.That(message.fontSize, Is.GreaterThanOrEqualTo(24));
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
                foreach (Transform card in grid)
                {
                    Image surface = card.GetComponent<Image>();
                    Assert.That(surface.sprite, Is.Not.Null, card.name);
                    Assert.That(surface.type, Is.EqualTo(Image.Type.Sliced), card.name);
                    Shadow shadow = card.GetComponent<Shadow>();
                    Assert.That(shadow, Is.Not.Null, card.name);
                    Assert.That(Mathf.Abs(shadow.effectDistance.x), Is.LessThanOrEqualTo(8f), card.name);
                }

                Image challenge = root.transform.Find("S5MapPresentation/Content/BossChallenge")
                    .GetComponent<Image>();
                Assert.That(challenge.sprite, Is.Not.Null);
                Assert.That(challenge.type, Is.EqualTo(Image.Type.Sliced));
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
                Assert.That(livesLabel.GetComponentInChildren<Text>().horizontalOverflow,
                    Is.EqualTo(HorizontalWrapMode.Overflow));

                LayoutElement upcoming = root.transform.Find(
                        "S5MapPresentation/Content/FutureRow/UpcomingLabelContainer")
                    .GetComponent<LayoutElement>();
                Assert.That(upcoming.preferredHeight, Is.GreaterThanOrEqualTo(40f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LockedCardsAndChallengeRenderLockPictograms()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                MapNodeView ready = screen.Nodes.Single(node => node.SubjectId == SubjectId.Sprint);
                MapNodeView locked = screen.Nodes.Single(node => node.SubjectId == SubjectId.Basketball);
                Assert.That(ready.transform.Find("LockIcon"), Is.Null);
                Image cardLock = locked.transform.Find("LockIcon").GetComponent<Image>();
                Assert.That(cardLock.sprite, Is.Not.Null);
                Assert.That(cardLock.preserveAspect, Is.True);
                Assert.That(cardLock.raycastTarget, Is.False);

                Image challengeLock = root.transform.Find(
                        "S5MapPresentation/Content/BossChallenge/LockIcon")
                    .GetComponent<Image>();
                Assert.That(challengeLock.sprite, Is.Not.Null);
                Assert.That(challengeLock.preserveAspect, Is.True);
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
                Assert.That(sprint.Find("StatusContainer/Status").GetComponent<Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));
                Assert.That(sprint.Find("DetailContainer/Detail").GetComponent<Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));
                Assert.That(sprint.Find("ActionHint").GetComponent<Text>().fontSize,
                    Is.GreaterThanOrEqualTo(26));

                Text progressHint = root.transform.Find(
                        "S5MapPresentation/Content/SelectionGrid/ProgressCard/UnlockHintContainer/UnlockHint")
                    .GetComponent<Text>();
                Assert.That(progressHint.fontSize, Is.GreaterThanOrEqualTo(24));
                foreach (Text tag in root.transform.Find("S5MapPresentation/Content/FutureRow")
                             .GetComponentsInChildren<Text>(true))
                    Assert.That(tag.fontSize, Is.GreaterThanOrEqualTo(22), tag.name);
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
                Text status = node.transform.Find("StatusContainer/Status").GetComponent<Text>();
                Text action = node.transform.Find("ActionHint").GetComponent<Text>();

                node.SetAvailability(false, "TẠM KHÓA");

                Assert.That(button.interactable, Is.False);
                Assert.That(feedback.enabled, Is.False);
                Assert.That(status.text, Is.EqualTo("🔒  TẠM KHÓA"));
                Assert.That(node.DetailText, Is.EqualTo("TẠM KHÓA"));
                Assert.That(node.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(action.text, Is.Empty);

                node.SetAvailability(true, "TẠM KHÓA");

                Assert.That(button.interactable, Is.True);
                Assert.That(feedback.enabled, Is.True);
                Assert.That(status.text, Is.EqualTo("SẴN SÀNG"));
                Assert.That(node.DetailText, Is.EqualTo("SẴN SÀNG"));
                Assert.That(node.transform.Find("DetailContainer").gameObject.activeSelf, Is.False);
                Assert.That(action.text, Is.EqualTo("THI →"));
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
                var session = new GameSession();
                session.StartSubject(SubjectId.Sprint);
                session.SubmitResult(SubjectId.Sprint, new MinigameResult(true, 8f, Rank.A));
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

                MapNodeView ready = screen.Nodes.Single(node => node.SubjectId == SubjectId.Endurance);
                Color readyBorder = ready.GetComponent<Outline>().effectColor;
                Assert.That(readyBorder.r, Is.GreaterThan(.9f));
                Assert.That(readyBorder.g, Is.GreaterThan(.65f));
                Assert.That(readyBorder.b, Is.LessThan(.35f));
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

        [Test]
        public void ZeroProgressHidesFillInsteadOfCreatingANegativeWidthRect()
        {
            var root = new GameObject("map", typeof(RectTransform));
            try
            {
                var screen = root.AddComponent<MapScreen>();
                MapPresentationBuilder.Build(screen, new GameSession());

                GameObject fill = root.transform.Find(
                        "S5MapPresentation/Content/SelectionGrid/ProgressCard/ProgressTrack/Fill")
                    .gameObject;
                Assert.That(fill.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
