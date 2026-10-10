using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Presentation
{
    public sealed class TutorialSeenStoreTests
    {
        [Test]
        public void DirectSceneUsesTheMemoryFallbackAndNoPlayerPrefsKey()
        {
            const string playerPrefsKey = "KMA.tutorialSeen.Sprint";
            PlayerPrefs.DeleteKey(playerPrefsKey);
            try
            {
                var memory = new MemoryTutorialSeenStore();
                var store = new SaveDataTutorialSeenStore(memory);
                store.MarkSeen("Sprint");
                store.MarkSeen(SaveDataTutorialSeenStore.FrogJumpKey);

                Assert.That(store.HasSeen("Sprint"), Is.True);
                Assert.That(memory.HasSeen(SaveDataTutorialSeenStore.FrogJumpKey), Is.True);
                Assert.That(PlayerPrefs.HasKey(playerPrefsKey), Is.False,
                    "Direct-scene completion must not create the retired PlayerPrefs tutorial key.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
            }
        }
    }
}
