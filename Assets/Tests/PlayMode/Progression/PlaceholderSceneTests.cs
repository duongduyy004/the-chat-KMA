using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class PlaceholderSceneTests
    {
        [Test]
        public void Placeholder_DebugControlsResolveOnlyOnce()
        {
            var controller = new GameObject("Placeholder").AddComponent<PlaceholderMinigameController>();
            try
            {
                var completions = 0;
                controller.Completed += _ => completions++;
                controller.ConfigureForTest(SubjectId.Volleyball);
                controller.DebugPass();
                controller.DebugFail();
                Assert.That(completions, Is.EqualTo(1));
                Assert.That(controller.PresentationPhase, Is.EqualTo(MinigamePhase.Resolve));
            }
            finally { Object.DestroyImmediate(controller.gameObject); }
        }
    }
}
