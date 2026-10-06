using System.Collections;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    /// Deactivation runs OnDisable/OnEnable only in Play Mode, so the re-activation path lives here.
    public sealed class KitPressFeedbackPlayModeTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("FeedbackRoot", typeof(RectTransform));

        [TearDown]
        public void TearDown() => Object.Destroy(root);

        [UnityTest]
        public IEnumerator DisabledLookSurvivesDeactivateAndReactivate()
        {
            ButtonHandle handle = UiKit.Button(root.transform, "Shoot", "GIỮ ĐỂ SÚT", ButtonVariant.Primary);
            Color restLabel = handle.Label.color;
            handle.Button.interactable = false;
            yield return null;
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.DisabledSurface));

            handle.Button.gameObject.SetActive(false);
            handle.Button.gameObject.SetActive(true);
            // Checked before any Update: a re-activated disabled button must never flash its enabled colours.
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.DisabledSurface));
            Assert.That(handle.Label.color, Is.EqualTo(MinigameUiTheme.DisabledText));
            yield return null;
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.DisabledSurface));
            Assert.That(handle.Label.color, Is.EqualTo(MinigameUiTheme.DisabledText));

            handle.Button.interactable = true;
            yield return null;
            Assert.That(handle.Face.color, Is.EqualTo(MinigameUiTheme.Accent));
            Assert.That(handle.Label.color, Is.EqualTo(restLabel));
        }
    }
}
