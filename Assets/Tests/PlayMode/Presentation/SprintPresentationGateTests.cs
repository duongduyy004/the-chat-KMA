using System.Collections;
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class SprintPresentationGateTests
    {
        const string SceneName = "MG_Sprint";
        [UnityTest]
        public IEnumerator SprintSceneHasCompletePresentationContractAndUsesAutomaticStart()
        {
            yield return LoadSprint();
            yield return new WaitForSeconds(.5f);

            var scene = SceneManager.GetActiveScene();
            var controllers = SceneObjects<SprintController>(scene);
            var sprintHuds = SceneObjects<SprintHud>(scene);
            var windCues = SceneObjects<SprintWindCue>(scene);
            var overlays = SceneObjects<TutorialOverlay>(scene);
            var starts = SceneObjects<SprintStartPresentation>(scene);
            var pauses = SceneObjects<PausePanel>(scene);
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(sprintHuds.Length, Is.EqualTo(1));
            Assert.That(windCues.Length, Is.EqualTo(1));
            Assert.That(overlays.Length, Is.EqualTo(1));
            Assert.That(starts.Length, Is.EqualTo(1));
            Assert.That(pauses.Length, Is.EqualTo(1));

            var sprintHud = sprintHuds[0];
            var windCue = windCues[0];
            Assert.That(sprintHud.HasBoundVisuals, Is.True, "SprintHud must bind all authored HUD labels/fills.");
            Assert.That(windCue.HasBoundVisuals, Is.True, "SprintWindCue must bind a separate host, Image, and TMP state label.");

            var start = starts[0];
            Assert.That(overlays[0].ShouldShow, Is.False,
                "Sprint must not open the shared multi-page tutorial.");
            Assert.That(start.TutorialVisible, Is.True);
            Assert.That(start.TutorialText,
                Is.EqualTo("← TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI →"));

            Transform tutorialBanner = GameObject.Find("SprintBroadcastChrome")?.transform
                .Find("StartPresentation/TutorialBanner");
            TMP_Text tutorialMessage = tutorialBanner?.Find("TutorialLabel")?.GetComponent<TMP_Text>();
            Assert.That(tutorialBanner, Is.Not.Null);
            Assert.That(tutorialBanner.gameObject.activeSelf, Is.True,
                "The active Sprint tutorial phase must render its dedicated banner.");
            Assert.That(tutorialMessage, Is.Not.Null);
            Assert.That(tutorialMessage.text, Is.EqualTo("TRÁI     BẤM LUÂN PHIÊN ĐỂ CHẠY     PHẢI"));
            Canvas.ForceUpdateCanvases();
            RectTransform tutorialRect = tutorialBanner.GetComponent<RectTransform>();
            RectTransform chromeRect = tutorialRect.parent.GetComponent<RectTransform>();
            RectTransform safeAreaRect = chromeRect.parent.GetComponent<RectTransform>();
            Assert.That(safeAreaRect.rect.width, Is.GreaterThan(100f));
            Assert.That(chromeRect.rect.width, Is.GreaterThan(100f));
            Assert.That(tutorialRect.rect.width, Is.GreaterThan(100f));
            Assert.That(tutorialRect.rect.height, Is.GreaterThan(50f));
            Rect tutorialBounds = ScreenRect(tutorialRect);
            Assert.That(tutorialBounds.width, Is.GreaterThan(Screen.width * .5f));
            Assert.That(tutorialBounds.center.y, Is.EqualTo(Screen.height * .5f).Within(Screen.height * .2f),
                "The tutorial banner must occupy the visual center, not overlap the top scene content.");
            RectTransform leftArrow = tutorialBanner?.Find("LeftArrow")?.GetComponent<RectTransform>();
            RectTransform rightArrow = tutorialBanner?.Find("RightArrow")?.GetComponent<RectTransform>();
            RectTransform leftTipTop = tutorialBanner?.Find("LeftArrow/TipTop")
                ?.GetComponent<RectTransform>();
            RectTransform leftTipBottom = tutorialBanner?.Find("LeftArrow/TipBottom")
                ?.GetComponent<RectTransform>();
            RectTransform rightTipTop = tutorialBanner?.Find("RightArrow/TipTop")
                ?.GetComponent<RectTransform>();
            RectTransform rightTipBottom = tutorialBanner?.Find("RightArrow/TipBottom")
                ?.GetComponent<RectTransform>();
            Assert.That(leftTipTop, Is.Not.Null);
            Assert.That(leftTipBottom, Is.Not.Null);
            Assert.That(rightTipTop, Is.Not.Null);
            Assert.That(rightTipBottom, Is.Not.Null);
            Assert.That(leftArrow.anchorMax.x, Is.LessThan(.5f));
            Assert.That(rightArrow.anchorMin.x, Is.GreaterThan(.5f));
            Assert.That(leftTipTop.anchorMin.x, Is.LessThan(.5f));
            Assert.That(leftTipBottom.anchorMin.x, Is.LessThan(.5f));
            Assert.That(rightTipTop.anchorMin.x, Is.GreaterThan(.5f));
            Assert.That(rightTipBottom.anchorMin.x, Is.GreaterThan(.5f));
            Assert.That(leftTipTop.anchoredPosition.y, Is.GreaterThan(leftTipBottom.anchoredPosition.y),
                "Left-arrow tips must form a chevron, not an X.");
            Assert.That(rightTipTop.anchoredPosition.y, Is.GreaterThan(rightTipBottom.anchoredPosition.y),
                "Right-arrow tips must form a chevron, not an X.");
            Assert.That(SignedZAngle(leftTipTop), Is.EqualTo(45f).Within(.01f));
            Assert.That(SignedZAngle(leftTipBottom), Is.EqualTo(-45f).Within(.01f));
            Assert.That(SignedZAngle(rightTipTop), Is.EqualTo(-45f).Within(.01f));
            Assert.That(SignedZAngle(rightTipBottom), Is.EqualTo(45f).Within(.01f));

            var pause = pauses[0];
            var player = GameObject.Find("Player");
            Assert.That(player, Is.Not.Null);
            var playerVisual = player.GetComponentInChildren<SpriteRenderer>();
            Assert.That(playerVisual, Is.Not.Null, "Player needs a visible child SpriteRenderer.");
            Assert.That(playerVisual.sprite, Is.Not.Null);
            var safeArea = pause.GetComponentInParent<SafeAreaFitter>();
            Assert.That(safeArea, Is.Not.Null, "Pause must be inside the safe-area hierarchy.");
            var pauseRect = pause.GetComponent<RectTransform>();
            Assert.That(pauseRect.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchorMax, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.pivot, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(pauseRect.anchoredPosition.x, Is.LessThan(0f));
            Assert.That(pauseRect.anchoredPosition.y, Is.LessThan(0f));
            pause.Open();
            Assert.That(FindNamed<Button>(scene, "ResumeButton"), Is.Not.Null);
            Assert.That(FindNamed<Button>(scene, "RestartButton"), Is.Not.Null);
            Assert.That(FindNamed<Button>(scene, "ExitButton"), Is.Not.Null);
            pause.Resume();

            var canvas = pause.GetComponentInParent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));

            var left = FindNamed<ScreenTapArea>(scene, "LeftTap");
            var right = FindNamed<ScreenTapArea>(scene, "RightTap");
            Assert.That(left, Is.Not.Null);
            Assert.That(right, Is.Not.Null);
            var leftRect = left.GetComponent<RectTransform>();
            var rightRect = right.GetComponent<RectTransform>();
            Assert.That(leftRect.anchorMin.x, Is.EqualTo(.01f));
            Assert.That(leftRect.anchorMax.x, Is.LessThan(rightRect.anchorMin.x));
            Assert.That(rightRect.anchorMax.x, Is.EqualTo(.99f));
            Assert.That(1920f * (leftRect.anchorMax.x - leftRect.anchorMin.x) + leftRect.sizeDelta.x, Is.GreaterThanOrEqualTo(140f));
            Assert.That(1920f * (rightRect.anchorMax.x - rightRect.anchorMin.x) + rightRect.sizeDelta.x, Is.GreaterThanOrEqualTo(140f));
        }

        [UnityTest]
        public IEnumerator SprintUsesOnlyStartPresentationBeforeResolve()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var start = SceneObjects<SprintStartPresentation>(scene)[0];
            var phase = SceneObjects<PhaseOverlay>(scene)[0];
            Transform sharedTutorial = FindChildRecursive(phase.transform, "TutorialRoot");
            Transform sharedCountdown = FindChildRecursive(phase.transform, "CountdownRoot");
            Transform sharedPlay = FindChildRecursive(phase.transform, "PlayRoot");
            TMP_Text sharedPhaseLabel = FindNamed<TMP_Text>(scene, "PhaseLabel");

            Assert.That(sharedTutorial, Is.Not.Null);
            Assert.That(sharedCountdown, Is.Not.Null);
            Assert.That(sharedPlay, Is.Not.Null);
            Assert.That(sharedPhaseLabel, Is.Not.Null);
            Assert.That(sharedTutorial.gameObject.activeSelf, Is.False);
            Assert.That(sharedCountdown.gameObject.activeSelf, Is.False);
            Assert.That(sharedPlay.gameObject.activeSelf, Is.False);
            Assert.That(sharedPhaseLabel.text, Is.Empty);

            start.TickForTest(1.5f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Countdown));
            Assert.That(sharedTutorial.gameObject.activeSelf, Is.False);
            Assert.That(sharedCountdown.gameObject.activeSelf, Is.False,
                "Sprint countdown must not duplicate the dedicated start presenter.");
            Assert.That(sharedPlay.gameObject.activeSelf, Is.False);
            Assert.That(sharedPhaseLabel.text, Is.Empty);

            controller.Simulate(1f);
            controller.Simulate(1f);
            controller.Simulate(1f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            Assert.That(sharedTutorial.gameObject.activeSelf, Is.False);
            Assert.That(sharedCountdown.gameObject.activeSelf, Is.False);
            Assert.That(sharedPlay.gameObject.activeSelf, Is.False,
                "Sprint must not show the shared static PLAY surface.");
            Assert.That(sharedPhaseLabel.text, Is.Empty);
        }

        [UnityTest]
        public IEnumerator SprintPresentationRendersDedicatedHudMetricsAndWindStateTransitions()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var sprintHud = SceneObjects<SprintHud>(scene)[0];
            var windCue = SceneObjects<SprintWindCue>(scene)[0];
            Transform chrome = GameObject.Find("SprintBroadcastChrome")?.transform;
            Assert.That(chrome, Is.Not.Null);
            Assert.That(GameObject.Find("SprintFestivalChrome"), Is.Null);

            TMP_Text distance = chrome.Find("Scoreboard/Distance")?.GetComponent<TMP_Text>();
            TMP_Text rank = chrome.Find("Scoreboard/RankBadge/RankLabel")?.GetComponent<TMP_Text>();
            TMP_Text cadence = chrome.Find("Scoreboard/Combo")?.GetComponent<TMP_Text>();
            Image distanceFill = chrome.Find("ProgressRail/RailFill")?.GetComponent<Image>();

            Assert.That(sprintHud.HasBoundVisuals, Is.True);
            Assert.That(distance, Is.Not.Null);
            Assert.That(rank, Is.Not.Null);
            Assert.That(cadence, Is.Not.Null);
            Assert.That(distanceFill, Is.Not.Null);
            Assert.That(distanceFill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(chrome.GetComponentInParent<SafeAreaFitter>(), Is.Not.Null);
            Assert.That(distance.text, Is.EqualTo("0 / 100 m"));
            Assert.That(rank.text, Is.EqualTo("1st"));
            Assert.That(cadence.text, Is.EqualTo("COMBO ×0"));

            controller.ConfigureForTest(.8f);
            controller.AdvanceToDistance(42f);
            sprintHud.Refresh();

            Assert.That(distance.text, Is.EqualTo("42 / 100 m"));
            Assert.That(rank.text, Is.EqualTo("1st"));
            Assert.That(cadence.text, Is.EqualTo("COMBO ×0"));
            Assert.That(distanceFill.fillAmount, Is.EqualTo(.42f).Within(.001f));

            var cueImage = FindNamed<Image>(scene, "WindCueHost");
            Assert.That(cueImage, Is.Not.Null);
            var cueHost = cueImage.gameObject;
            var cueState = cueHost.GetComponentInChildren<TMP_Text>(true);
            Assert.That(cueState, Is.Not.Null);
            Assert.That(cueHost.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(cueHost.GetComponentInParent<SafeAreaFitter>(), Is.Not.Null);
            Assert.That(windCue.gameObject.activeSelf, Is.True);

            controller.AdvanceToDistance(30f);
            controller.Simulate(0f);
            windCue.Refresh();
            Assert.That(cueHost.activeSelf, Is.True);
            Assert.That(cueState.text, Is.EqualTo("GIÓ ĐANG ĐẾN"));
            Assert.That(cueImage.color, Is.EqualTo(Color.white));

            controller.Simulate(.8f);
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("CHẠM ĐỂ CẢN GIÓ"));
            Assert.That(cueImage.color, Is.EqualTo(new Color(1f, .8f, 0f, 1f)));

            controller.OnLeftTap();
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("CẢN GIÓ THÀNH CÔNG"));
            Assert.That(cueImage.color, Is.EqualTo(Color.green));

            controller.ConfigureForTest(.8f);
            controller.AdvanceToDistance(30f);
            controller.Simulate(2.1f);
            windCue.Refresh();
            Assert.That(cueState.text, Is.EqualTo("LỠ NHỊP GIÓ"));
            Assert.That(cueImage.color, Is.EqualTo(Color.red));

            yield return null;
        }

        [UnityTest]
        public IEnumerator SprintSceneKeepsRunnersInFourEqualTrackBandsAndMarksOnlyThePlayer()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            Transform[] runnerRoots =
            {
                FindNamed<Transform>(scene, "Runner_01"),
                FindNamed<Transform>(scene, "Player"),
                FindNamed<Transform>(scene, "Runner_03"),
                FindNamed<Transform>(scene, "Runner_04")
            };
            var trackRegion = new Rect(-9.6f, -2.8f, 19.2f, 5.6f);
            for (int lane = 0; lane < runnerRoots.Length; lane++)
            {
                Assert.That(runnerRoots[lane], Is.Not.Null);
                SpriteRenderer runnerVisual = runnerRoots[lane].GetComponentInChildren<SpriteRenderer>(true);
                Assert.That(runnerVisual, Is.Not.Null);
                float expectedY = trackRegion.yMin + trackRegion.height *
                    (1f - SprintUiLayout.LaneCenter01(lane, runnerRoots.Length));
                Assert.That(runnerVisual.transform.position.y, Is.EqualTo(expectedY).Within(.02f));
            }

            Transform chromeMarker = GameObject.Find("SprintBroadcastChrome")?.transform.Find("PlayerMarker");
            Assert.That(chromeMarker, Is.Null);

            Transform playerPresentation = runnerRoots[1].GetComponentInChildren<RunnerVisualPresenter>(true)?.transform;
            Assert.That(playerPresentation, Is.Not.Null);
            Transform marker = playerPresentation.Find("PlayerMarker");
            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.IsChildOf(runnerRoots[1]), Is.True);
            TextMesh markerLabel = marker.GetComponent<TextMesh>();
            Assert.That(markerLabel, Is.Not.Null);
            Assert.That(markerLabel.text, Is.EqualTo("PLAYER"));

            MonoBehaviour playerIdentity = FindIdentityOutline(playerPresentation);
            Assert.That(playerIdentity, Is.Not.Null);
            Assert.That(ReadProperty<Color>(playerIdentity, "OutlineColor"), Is.EqualTo(Color.cyan));
            Assert.That(ReadProperty<SpriteRenderer>(playerIdentity, "Source"), Is.SameAs(
                runnerRoots[1].GetComponentInChildren<SpriteRenderer>(true)));
            for (int lane = 0; lane < runnerRoots.Length; lane++)
            {
                if (lane == 1) continue;
                Assert.That(FindIdentityOutline(runnerRoots[lane]), Is.Null);
                Assert.That(FindChildRecursive(runnerRoots[lane], "PlayerMarker"), Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator SprintControls_AreRealButtonsMatchingTheirHitAreas()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            foreach (string tapName in new[] { "LeftTap", "RightTap" })
            {
                var tap = GameObject.Find(tapName).GetComponent<RectTransform>();
                var visual = tap.Find("Visual") as RectTransform;
                Assert.That(visual, Is.Not.Null, $"{tapName} must own a Visual child.");

                Assert.That(visual.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(visual.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(visual.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(visual.offsetMax, Is.EqualTo(Vector2.zero));

                Assert.That(tap.GetComponent<Image>().raycastTarget, Is.True, $"{tapName} must receive taps.");
                Assert.That(visual.Find("Border").GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(visual.Find("Background").GetComponent<Image>().sprite, Is.Not.Null);
            }

            Assert.That(GameObject.Find("LeftTap").transform.Find("Visual/Label").GetComponent<TMP_Text>().text,
                Is.EqualTo("TRÁI"));
            Assert.That(GameObject.Find("RightTap").transform.Find("Visual/Label").GetComponent<TMP_Text>().text,
                Is.EqualTo("PHẢI"));
            Assert.That(GameObject.Find("LeftTap").transform.Find("Visual/Arrow").GetComponent<TMP_Text>().text,
                Is.EqualTo("←"));
            Assert.That(GameObject.Find("RightTap").transform.Find("Visual/Arrow").GetComponent<TMP_Text>().text,
                Is.EqualTo("→"));
        }

        [UnityTest]
        public IEnumerator SprintControls_HighlightTheExpectedSideWithoutTouchingGameplay()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            var controller = SceneObjects<SprintController>(scene)[0];
            var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
            controller.Simulate(1f);
            controller.Simulate(1f);
            controller.Simulate(1f);
            presenter.RefreshForTest();

            Assert.That(presenter.HighlightedSide, Is.EqualTo(controller.ExpectedSide));

            int comboBefore = controller.CadenceCombo;
            presenter.RefreshForTest();
            Assert.That(controller.CadenceCombo, Is.EqualTo(comboBefore),
                "The presenter must never advance gameplay state.");
        }

        [UnityTest]
        public IEnumerator SprintControls_ShrinkOnPressAndRecover()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            var controller = SceneObjects<SprintController>(scene)[0];
            var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
            float distanceBefore = controller.Snapshot.Distance;

            presenter.PressForTest(KMA.Gameplay.Side.Left);
            Assert.That(presenter.LeftScale, Is.EqualTo(.94f).Within(.001f));
            Assert.That(presenter.RightScale, Is.EqualTo(1f).Within(.001f));

            presenter.TickForTest(.091f);
            Assert.That(presenter.LeftScale, Is.EqualTo(1f).Within(.001f));
            Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBefore).Within(.0001f));
        }

        [UnityTest]
        public IEnumerator SprintTapAreasStayInTheInputCanvasOutsideTheChrome()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
            Transform inputCanvas = GameObject.Find("Input")?.transform;
            Assert.That(inputCanvas, Is.Not.Null);

            foreach (string tapName in new[] { "LeftTap", "RightTap" })
            {
                Transform tap = GameObject.Find(tapName).transform;
                Assert.That(tap.IsChildOf(inputCanvas), Is.True,
                    $"{tapName} must stay in the input canvas.");
                Assert.That(tap.IsChildOf(chrome), Is.False,
                    $"{tapName} must not be reparented under the HUD chrome.");
            }
        }

        [UnityTest]
        public IEnumerator SprintControlsColourReflectsExpectedPressedAndFinishedStates()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var start = SceneObjects<SprintStartPresentation>(scene)[0];
            var presenter = SceneObjects<SprintControlPresenter>(scene)[0];

            Image LeftBackground() =>
                GameObject.Find("LeftTap").transform.Find("Visual/Background").GetComponent<Image>();
            Image RightBackground() =>
                GameObject.Find("RightTap").transform.Find("Visual/Background").GetComponent<Image>();
            Image LeftBorder() =>
                GameObject.Find("LeftTap").transform.Find("Visual/Border").GetComponent<Image>();
            Image RightBorder() =>
                GameObject.Find("RightTap").transform.Find("Visual/Border").GetComponent<Image>();

            start.TickForTest(1.5f);
            controller.Simulate(1f);
            controller.Simulate(1f);
            controller.Simulate(1f);
            Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Play));
            presenter.RefreshForTest();

            bool leftExpected = presenter.HighlightedSide == KMA.Gameplay.Side.Left;
            Image expectedBorder = leftExpected ? LeftBorder() : RightBorder();
            Image otherBorder = leftExpected ? RightBorder() : LeftBorder();
            Assert.That(expectedBorder.color, Is.Not.EqualTo(otherBorder.color),
                "the next side to press must look different from the other one");
            Assert.That(expectedBorder.color.a, Is.GreaterThan(otherBorder.color.a),
                "the expected side must be the more prominent of the two");

            Color idleLeftBackground = LeftBackground().color;
            Color idleRightBackground = RightBackground().color;
            presenter.PressForTest(KMA.Gameplay.Side.Left);
            Assert.That(LeftBackground().color, Is.Not.EqualTo(idleLeftBackground),
                "pressing must visibly change the button, not only its scale");
            Assert.That(RightBackground().color, Is.EqualTo(idleRightBackground),
                "pressing one side must not restyle the other");

            presenter.TickForTest(.1f);
            presenter.RefreshForTest();
            Assert.That(LeftBackground().color, Is.EqualTo(idleLeftBackground).Within(.01f),
                "the press tint must decay back to idle");
        }

        [UnityTest]
        public IEnumerator SprintControlPressFeedbackShrinksThenRestoresWithoutChangingGameplay()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
            controller.ConfigureForTest(.8f);
            float distanceBefore = controller.Snapshot.Distance;
            int comboBefore = controller.CadenceCombo;

            presenter.PressForTest(KMA.Gameplay.Side.Left);
            Assert.That(presenter.LeftScale, Is.EqualTo(.94f).Within(.001f));
            presenter.TickForTest(.091f);
            Assert.That(presenter.LeftScale, Is.EqualTo(1f).Within(.001f));
            Assert.That(controller.Snapshot.Distance, Is.EqualTo(distanceBefore));
            Assert.That(controller.CadenceCombo, Is.EqualTo(comboBefore));
        }

        [UnityTest]
        public IEnumerator SprintFinishLineRevealsExactlyAtSeventyMetersWithoutBlockingInput()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var finish = SceneObjects<SprintFinishLinePresenter>(scene)[0];
            Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
            Transform finishLine = chrome.Find("FinishLine");
            Assert.That(finishLine, Is.Not.Null);

            RectTransform finishRect = finishLine.GetComponent<RectTransform>();
            Assert.That(finishRect.anchorMin.y, Is.EqualTo(0f));
            Assert.That(finishRect.anchorMax.y, Is.EqualTo(1f));

            Image[] squares = finishLine.GetComponentsInChildren<Image>(true);
            Assert.That(squares.Length, Is.GreaterThan(1));
            for (int i = 0; i < squares.Length; i++)
                Assert.That(squares[i].raycastTarget, Is.False);

            controller.ConfigureForTest(.8f);
            controller.AdvanceToDistance(69.9f);
            finish.RefreshForTest();
            Assert.That(finish.IsVisible, Is.False);
            Assert.That(finishLine.gameObject.activeSelf, Is.False);

            controller.AdvanceToDistance(70f);
            finish.RefreshForTest();
            Assert.That(finish.IsVisible, Is.True);
            Assert.That(finishLine.gameObject.activeSelf, Is.True);

            controller.AdvanceToDistance(100f);
            finish.RefreshForTest();
            Assert.That(finish.IsVisible, Is.True);
            Assert.That(finishLine.gameObject.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator SprintScene_ShowsNoLegacyHudChrome()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            Assert.That(GameObject.Find("SprintMetrics"), Is.Null,
                "The legacy SprintMetrics group must be deleted from MG_Sprint.unity.");
            Assert.That(GameObject.Find("SprintFestivalChrome"), Is.Null);

            foreach (string name in new[] { "Timer", "Phase", "Score", "Status", "Progress", "Stamina", "HeartBar" })
            {
                GameObject shared = GameObject.Find(name);
                Assert.That(shared, Is.Null, $"Shared HUD element {name} must not be active in the Sprint scene.");
            }
        }

        [UnityTest]
        public IEnumerator SprintResultPresentationStylesOutcomesAndKeepsSingleContinueDuringAnimation()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var panel = SceneObjects<ResultPanel>(scene)[0];
            var presentation = SceneObjects<SprintResultPresentation>(scene)[0];
            Transform content = panel.transform.Find("Content");
            TMP_Text title = content.Find("StatusLabel").GetComponent<TMP_Text>();
            TMP_Text score = content.Find("ScoreLabel").GetComponent<TMP_Text>();
            TMP_Text rank = content.Find("RankLabel").GetComponent<TMP_Text>();
            Button action = content.Find("ActionButton").GetComponent<Button>();

            var routes = new List<string>();
            panel.ActionRequested += routes.Add;

            panel.Show(new MinigameResult(false, 0f, Rank.F), "Punishment");
            presentation.ShowForTest(panel.CurrentResult);

            Assert.That(title.text, Is.EqualTo("THẤT BẠI"));
            AssertColor32(title.color, new Color32(255, 89, 94, 255));
            Assert.That(score.text, Is.EqualTo("0"));
            Assert.That(rank.text, Is.EqualTo("F"));
            Assert.That(action.interactable, Is.True);

            panel.Continue();
            panel.Continue();
            Assert.That(routes.Count, Is.EqualTo(1));
            Assert.That(routes[0], Is.EqualTo("Punishment"));

            panel.Show(new MinigameResult(true, 8.4f, Rank.A), "Map");
            presentation.ShowForTest(panel.CurrentResult);

            Assert.That(title.text, Is.EqualTo("HOÀN THÀNH!"));
            AssertColor32(title.color, new Color32(94, 222, 140, 255));
            Assert.That(score.text, Is.EqualTo("8"));
            Assert.That(rank.text, Is.EqualTo("A"));
            Assert.That(action.interactable, Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SprintScene_BuildsTheApprovedScoreboardAndRail()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
            Assert.That(chrome.GetComponentInParent<KMA.Gameplay.UI.SafeAreaFitter>(), Is.Not.Null);

            Assert.That(chrome.Find("Scoreboard/Distance").GetComponent<TMP_Text>().text, Is.EqualTo("0 / 100 m"));
            Assert.That(chrome.Find("Scoreboard/RankBadge/RankLabel").GetComponent<TMP_Text>().text, Is.EqualTo("1st"));
            Assert.That(chrome.Find("Scoreboard/Combo").GetComponent<TMP_Text>().text, Is.EqualTo("COMBO ×0"));
            Assert.That(chrome.Find("ModeLabel").GetComponent<TMP_Text>().text, Is.EqualTo("CHẠY NƯỚC RÚT · 100M"));

            Image railFill = chrome.Find("ProgressRail/RailFill").GetComponent<Image>();
            Assert.That(railFill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(railFill.fillAmount, Is.EqualTo(0f).Within(.001f));
            Assert.That(railFill.color, Is.EqualTo(SprintUiTheme.Accent));

            Image pip = chrome.Find("ProgressRail/PlayerPip").GetComponent<Image>();
            Assert.That(pip.color, Is.EqualTo(SprintUiTheme.Player));

            Assert.That(chrome.Find("Scoreboard").GetComponent<Image>().sprite, Is.Not.Null,
                "The scoreboard must use a generated rounded sprite, not the default square.");
            Assert.That(chrome.Find("Scoreboard").GetComponent<Outline>(), Is.Null,
                "Outline is replaced by Shadow.");
            Assert.That(chrome.Find("Scoreboard").GetComponent<Shadow>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SprintHud_ReflectsDistanceRankAndCombo()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            var controller = SceneObjects<SprintController>(scene)[0];
            var hud = SceneObjects<SprintHud>(scene)[0];
            Assert.That(hud.HasBoundVisuals, Is.True);

            controller.AdvanceToDistance(42f);
            hud.Refresh();

            Transform chrome = GameObject.Find("SprintBroadcastChrome").transform;
            Assert.That(chrome.Find("Scoreboard/Distance").GetComponent<TMP_Text>().text, Is.EqualTo("42 / 100 m"));
            Assert.That(chrome.Find("ProgressRail/RailFill").GetComponent<Image>().fillAmount,
                Is.EqualTo(.42f).Within(.001f));
            // Anchors collapse to 0 under batchmode's zero-sized safe rect (see CONTROLLER RULING #1),
            // so progress is asserted through the dedicated field instead of the anchor position.
            Assert.That(hud.PipProgress, Is.EqualTo(.42f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator SprintChrome_RegistersEveryElementForRelayout()
        {
            yield return LoadSprint();
            var scene = SceneManager.GetActiveScene();

            var layout = SceneObjects<SprintChromeLayout>(scene)[0];
            Assert.That(layout, Is.Not.Null, "chrome must own a relayout component");
            Assert.That(layout.ElementCount, Is.GreaterThanOrEqualTo(4),
                "rail, scoreboard, mode chip and pause must all be registered");
        }

        static IEnumerator LoadSprint()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
        }

        static T[] SceneObjects<T>(Scene scene) where T : Component
        {
            var all = Object.FindObjectsOfType<T>(true);
            var count = 0;
            for (var i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == scene) count++;
            var result = new T[count];
            var index = 0;
            for (var i = 0; i < all.Length; i++)
                if (all[i].gameObject.scene == scene) result[index++] = all[i];
            return result;
        }

        static T FindNamed<T>(Scene scene, string name) where T : Component
        {
            var objects = SceneObjects<T>(scene);
            for (var i = 0; i < objects.Length; i++)
                if (objects[i].name == name) return objects[i];
            return null;
        }

        static Transform FindChildRecursive(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;
                Transform nested = FindChildRecursive(child, name);
                if (nested != null) return nested;
            }
            return null;
        }

        static MonoBehaviour FindIdentityOutline(Transform root)
        {
            var components = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
                if (components[i] != null && components[i].GetType().Name == "SprintPlayerIdentityOutline")
                    return components[i];
            return null;
        }

        static T ReadProperty<T>(MonoBehaviour component, string propertyName) =>
            (T)component.GetType().GetProperty(propertyName).GetValue(component);

        static Rect ScreenRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Camera camera = rect.GetComponentInParent<Canvas>().worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static void AssertColor32(Color color, Color32 expected)
        {
            Color32 actual = color;
            Assert.That(actual.r, Is.EqualTo(expected.r));
            Assert.That(actual.g, Is.EqualTo(expected.g));
            Assert.That(actual.b, Is.EqualTo(expected.b));
            Assert.That(actual.a, Is.EqualTo(expected.a));
        }

        static float SignedZAngle(RectTransform transform) =>
            Mathf.DeltaAngle(0f, transform.localEulerAngles.z);
    }
}
