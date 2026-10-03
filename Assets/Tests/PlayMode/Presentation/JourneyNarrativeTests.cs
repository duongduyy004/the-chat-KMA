using System.Collections;
using System.Linq;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class JourneyNarrativeTests
    {
        GameObject root;

        [UnityTest]
        public IEnumerator SkipCanRetryAfterSaveFailureAndPersistsBeforeClosing()
        {
            root = new GameObject("JourneyNarrativeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            JourneyDialoguePresenter presenter = root.AddComponent<JourneyDialoguePresenter>();
            bool allowSave = false;
            string savedKey = null;
            presenter.Configure(JourneyDialogueLibrary.LoadDefault(), key =>
            {
                savedKey = key;
                return allowSave;
            });
            int closed = 0;
            presenter.Show("opening", () => closed++);
            yield return null;
            Button skip = root.GetComponentsInChildren<Button>(true).First(button => button.name == "BỎ QUA");

            skip.onClick.Invoke();
            Assert.That(presenter.IsShowing, Is.True);
            Assert.That(savedKey, Is.EqualTo("opening"));
            Assert.That(closed, Is.Zero);

            allowSave = true;
            skip.onClick.Invoke();
            Assert.That(presenter.IsShowing, Is.False);
            Assert.That(closed, Is.EqualTo(1));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root) Object.Destroy(root);
            yield return null;
        }
    }
}
