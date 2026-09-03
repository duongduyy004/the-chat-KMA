using System.Reflection;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class SceneTransitionOverlayTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (SceneTransitionOverlay overlay in Object.FindObjectsByType<SceneTransitionOverlay>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(overlay.gameObject);
            }
        }

        [Test]
        public void StartsHiddenThenShowAndHideToggleVisibility()
        {
            var root = new GameObject("scene-transition-overlay");
            try
            {
                var overlay = root.AddComponent<SceneTransitionOverlay>();
                overlay.InitializeForTest();

                Assert.That(overlay.IsVisible, Is.False);

                overlay.Show();
                Assert.That(overlay.IsVisible, Is.True);

                overlay.Hide();
                Assert.That(overlay.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BindSubscribesToRouterSceneLoadEventsToShowAndHide()
        {
            var overlayRoot = new GameObject("scene-transition-overlay");
            var routerRoot = new GameObject("scene-router");
            try
            {
                var overlay = overlayRoot.AddComponent<SceneTransitionOverlay>();
                overlay.InitializeForTest();
                var router = routerRoot.AddComponent<SceneRouter>();

                overlay.Bind(router);

                RaiseSceneLoadStarted(router);
                Assert.That(overlay.IsVisible, Is.True);

                RaiseSceneLoadCompleted(router);
                Assert.That(overlay.IsVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(overlayRoot);
                Object.DestroyImmediate(routerRoot);
            }
        }

        static void RaiseSceneLoadStarted(SceneRouter router) => InvokeEvent(router, "SceneLoadStarted");

        static void RaiseSceneLoadCompleted(SceneRouter router) => InvokeEvent(router, "SceneLoadCompleted");

        static void InvokeEvent(SceneRouter router, string eventName)
        {
            var field = typeof(SceneRouter).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = (System.Action)field.GetValue(router);
            handler?.Invoke();
        }
    }
}
