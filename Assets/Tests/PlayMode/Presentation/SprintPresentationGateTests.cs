using System.Collections;
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
        const string TutorialKey = "KMA.tutorialSeen.Sprint";

        [UnityTest]
        public IEnumerator SprintSceneHasCompletePresentationContractAndKeepsTutorialStateOutOfPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(TutorialKey);
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controllers = SceneObjects<SprintController>(scene);
            var sprintHuds = SceneObjects<SprintHud>(scene);
            var windCues = SceneObjects<SprintWindCue>(scene);
            var overlays = SceneObjects<TutorialOverlay>(scene);
            var pauses = SceneObjects<PausePanel>(scene);
            Assert.That(controllers.Length, Is.EqualTo(1));
            Assert.That(sprintHuds.Length, Is.EqualTo(1));
            Assert.That(windCues.Length, Is.EqualTo(1));
            Assert.That(overlays.Length, Is.EqualTo(1));
            Assert.That(pauses.Length, Is.EqualTo(1));

            var sprintHud = sprintHuds[0];
            var windCue = windCues[0];
            Assert.That(sprintHud.HasBoundVisuals, Is.True, "SprintHud must bind all authored HUD labels/fills.");
            Assert.That(windCue.HasBoundVisuals, Is.True, "SprintWindCue must bind a separate host, Image, and TMP state label.");

            var overlay = overlays[0];
            Assert.That(overlay.ShouldShow, Is.True);
            Assert.That(overlay.CurrentStep.Instruction,
                Is.EqualTo("Chạm luân phiên hai bên để tăng tốc"));
            overlay.Next();
            Assert.That(overlay.CurrentStep.Instruction,
                Is.EqualTo("Chạm đúng phía chỉ báo trước khi gió ập đến"));
            overlay.Skip();
            Assert.That(PlayerPrefs.HasKey(TutorialKey), Is.False);
            Assert.That(overlay.ShouldShow, Is.False);

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

            PlayerPrefs.DeleteKey(TutorialKey);
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
            TMP_Text rank = chrome.Find("Scoreboard/Rank")?.GetComponent<TMP_Text>();
            TMP_Text cadence = chrome.Find("Scoreboard/Combo")?.GetComponent<TMP_Text>();
            Image distanceFill = chrome.Find("Scoreboard/ProgressTrack/ProgressFill")?.GetComponent<Image>();

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
        public IEnumerator SprintControlsUseSmallTranslucentVisualsInsideDisjointAuthoritativeTapAreas()
        {
            yield return LoadSprint();

            var scene = SceneManager.GetActiveScene();
            var controller = SceneObjects<SprintController>(scene)[0];
            var presenter = SceneObjects<SprintControlPresenter>(scene)[0];
            var leftTap = FindNamed<ScreenTapArea>(scene, "LeftTap");
            var rightTap = FindNamed<ScreenTapArea>(scene, "RightTap");
            Image leftVisual = FindNamed<Image>(scene, "LeftControl");
            Image rightVisual = FindNamed<Image>(scene, "RightControl");

            Assert.That(presenter, Is.Not.Null);
            Assert.That(leftVisual, Is.Not.Null);
            Assert.That(rightVisual, Is.Not.Null);
            Assert.That(leftVisual.color.a, Is.EqualTo(.50f).Within(.02f));
            Assert.That(rightVisual.color.a, Is.EqualTo(.50f).Within(.02f));
            Assert.That(leftVisual.rectTransform.rect.width, Is.EqualTo(rightVisual.rectTransform.rect.width).Within(.01f));
            Assert.That(leftVisual.rectTransform.rect.height, Is.EqualTo(rightVisual.rectTransform.rect.height).Within(.01f));

            Rect leftHit = ScreenRect(leftTap.GetComponent<RectTransform>());
            Rect rightHit = ScreenRect(rightTap.GetComponent<RectTransform>());
            Rect leftVisualBounds = ScreenRect(leftVisual.rectTransform);
            Rect rightVisualBounds = ScreenRect(rightVisual.rectTransform);
            Assert.That(leftHit.Overlaps(rightHit), Is.False);
            Assert.That(leftVisualBounds.xMin, Is.GreaterThanOrEqualTo(leftHit.xMin - .01f));
            Assert.That(leftVisualBounds.yMin, Is.GreaterThanOrEqualTo(leftHit.yMin - .01f));
            Assert.That(leftVisualBounds.xMax, Is.LessThanOrEqualTo(leftHit.xMax + .01f));
            Assert.That(leftVisualBounds.yMax, Is.LessThanOrEqualTo(leftHit.yMax + .01f));
            Assert.That(rightVisualBounds.xMin, Is.GreaterThanOrEqualTo(rightHit.xMin - .01f));
            Assert.That(rightVisualBounds.yMin, Is.GreaterThanOrEqualTo(rightHit.yMin - .01f));
            Assert.That(rightVisualBounds.xMax, Is.LessThanOrEqualTo(rightHit.xMax + .01f));
            Assert.That(rightVisualBounds.yMax, Is.LessThanOrEqualTo(rightHit.yMax + .01f));

            controller.ConfigureForTest(.8f);
            presenter.RefreshForTest();
            Assert.That(presenter.HighlightedSide, Is.EqualTo(KMA.Gameplay.Side.Left));
            controller.OnLeftTap();
            presenter.RefreshForTest();
            Assert.That(presenter.HighlightedSide, Is.EqualTo(KMA.Gameplay.Side.Right));
            Assert.That(controller.CadenceCombo, Is.EqualTo(1));
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
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
