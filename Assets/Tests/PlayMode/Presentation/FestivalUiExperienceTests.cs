using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class FestivalUiExperienceTests
    {
        [UnityTest]
        public IEnumerator SplashShowsReadableProgressAndLoadingHint()
        {
            yield return SceneManager.LoadSceneAsync(
                "Assets/_Project/Scenes/Bootstrap.unity", LoadSceneMode.Single);

            Transform splash = GameObject.Find("SplashCanvas")?.transform;
            Assert.That(splash, Is.Not.Null);
            TMP_Text progress = splash.Find("ProgressPercent")?.GetComponent<TMP_Text>();
            Assert.That(progress, Is.Not.Null,
                "The loading state needs a numeric progress value, not only a fill bar.");
            Assert.That(progress.font, Is.Not.Null);
            TMP_Text hint = splash.Find("LoadingHint")?.GetComponent<TMP_Text>();
            Assert.That(hint, Is.Not.Null);
            Assert.That(hint.text, Is.EqualTo("ĐANG KHỞI ĐỘNG NGÀY HỘI THỂ THAO"));
            Assert.That(hint.font, Is.Not.Null);
            Image hintPlate = splash.Find("LoadingHintPlate")?.GetComponent<Image>();
            Assert.That(hintPlate, Is.Not.Null,
                "The loading hint needs a stable high-contrast surface over the stadium art.");
            Assert.That(hintPlate.color.a, Is.GreaterThanOrEqualTo(.7f));
        }

        [UnityTest]
        public IEnumerator MenuBuildsOneClearPrimaryActionAndCompactUtilities()
        {
            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            yield return null;

            Transform panel = GameObject.Find("FestivalMenuPanel")?.transform;
            Assert.That(panel, Is.Not.Null);
            var buttons = panel.GetComponentsInChildren<Button>();
            Assert.That(buttons.Select(button => button.name), Is.EqualTo(new[]
            {
                "CONTINUEButton", "NEW GAMEButton", "SETTINGSButton", "QUITButton"
            }));

        }

        [UnityTest]
        public IEnumerator Settings_OpenAdjustAndReturn_UsesVisibleRuntimeControls()
        {
            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuScreen>();
            FindButton("SETTINGSButton").onClick.Invoke();
            var settings = Object.FindFirstObjectByType<SettingsScreen>();
            Assert.That(settings, Is.Not.Null, "Settings must remain active when the menu is hidden.");
            Assert.That(menu.IsVisible, Is.False);
            var sliders = settings.GetComponentsInChildren<Slider>();
            Assert.That(sliders.Length, Is.EqualTo(2));
            sliders.Single(slider => slider.name == "MusicSlider").value = .25f;
            sliders.Single(slider => slider.name == "SfxSlider").value = .5f;
            var toggle = settings.GetComponentInChildren<Toggle>();
            Assert.That(toggle, Is.Not.Null);
            toggle.isOn = false;
            Assert.That(settings.CurrentSettings.musicVol, Is.EqualTo(.25f));
            Assert.That(settings.CurrentSettings.sfxVol, Is.EqualTo(.5f));
            Assert.That(settings.CurrentSettings.vibration, Is.False);
            settings.GetComponentsInChildren<Button>().Single(button => button.name == "BackButton")
                .onClick.Invoke();
            Assert.That(menu.IsVisible, Is.True);
            Assert.That(settings.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator MapUsesReadableLivesUniformCardsAndIntegratedProgress()
        {
            yield return SceneManager.LoadSceneAsync("Map", LoadSceneMode.Single);
            for (int frame = 0; frame < 4; frame++)
            {
                Canvas.ForceUpdateCanvases();
                yield return null;
            }

            Transform root = GameObject.Find("S5MapPresentation")?.transform;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Find("Content/Header/BackButton"), Is.Not.Null);
            RectTransform header = root.Find("Content/Header").GetComponent<RectTransform>();
            Assert.That(header.rect.height, Is.InRange(108f, 124f));

            Transform hearts = root.Find("Content/Header/LivesPanel/HeartBar");
            Assert.That(hearts, Is.Not.Null);
            foreach (LayoutElement heart in hearts.GetComponentsInChildren<LayoutElement>(true)
                         .Where(element => element.name.StartsWith("Heart") && element.name != "HeartBar"))
                Assert.That(heart.preferredWidth, Is.InRange(32f, 40f));

            Transform grid = root.Find("Content/SelectionGrid");
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.GetComponent<GridLayoutGroup>().cellSize.y,
                Is.GreaterThanOrEqualTo(220f));
            Assert.That(grid.childCount, Is.EqualTo(3));
            Assert.That(grid.GetChild(2).name, Is.EqualTo("FootballNode"));
            Assert.That(grid.Find("SprintNode/ActionHint").GetComponent<Text>().text,
                Is.EqualTo("THI →"));
        }

        [UnityTest]
        public IEnumerator SprintUsesAutomaticVietnameseStartBanner()
        {
            yield return SceneManager.LoadSceneAsync("MG_Sprint", LoadSceneMode.Single);
            yield return null;

            Transform chrome = GameObject.Find("SprintBroadcastChrome")?.transform;
            Assert.That(chrome, Is.Not.Null, "Automatic Sprint broadcast chrome install failed.");
            TMP_Text modeLabel = chrome.Find("ModeLabel").GetComponent<TMP_Text>();
            Assert.That(modeLabel.text, Is.EqualTo("CHẠY NƯỚC RÚT · 100M"));
            Assert.That(modeLabel.font, Is.Not.Null);

            SprintStartPresentation start = Object.FindFirstObjectByType<SprintStartPresentation>();
            Assert.That(start, Is.Not.Null);
            Assert.That(start.InstructionText, Is.EqualTo(SprintStartPresentation.InstructionCopy));
        }

        static Button FindButton(string name) => Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(button => button.name == name);
    }
}
