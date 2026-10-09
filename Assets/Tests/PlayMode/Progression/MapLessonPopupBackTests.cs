using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Progression
{
    // Android's Back button reaches the Input System as Escape.
    public sealed class MapLessonPopupBackTests : InputTestFixture
    {
        GameObject root;

        public override void TearDown()
        {
            Object.DestroyImmediate(root);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator BackClosesTheLessonPopup()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            root = new GameObject("map", typeof(RectTransform));
            var screen = root.AddComponent<MapScreen>();
            MapPresentationBuilder.Build(screen, new GameSession());
            screen.SelectSubject(SubjectId.Sprint);
            yield return null;
            Assert.That(screen.LessonList.IsOpen, Is.True);

            Press(keyboard.escapeKey);
            yield return null;
            Release(keyboard.escapeKey);

            Assert.That(screen.LessonList.IsOpen, Is.False);
            Assert.That(screen.LessonList.transform.parent.Find("LessonScrim").gameObject.activeSelf, Is.False);
        }
    }
}
