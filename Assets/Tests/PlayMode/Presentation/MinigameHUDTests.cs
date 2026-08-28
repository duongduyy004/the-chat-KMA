using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class MinigameHUDTests
    {
        [Test]
        public void RefreshFrom_HandlesEmptyStateWithoutOptionalUiFields()
        {
            var gameObject = new GameObject("minigame-hud");
            try
            {
                var hud = gameObject.AddComponent<MinigameHUD>();

                Assert.DoesNotThrow(() => hud.RefreshFrom(MinigameHudState.Empty));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
