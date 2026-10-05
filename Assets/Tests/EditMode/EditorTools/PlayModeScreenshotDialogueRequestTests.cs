using KMA.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace KMA.Tests.EditorTools
{
    public sealed class PlayModeScreenshotDialogueRequestTests
    {
        const string ActiveKey = "KMA_PMS_Active";
        const string NodeKey = "KMA_PMS_OpenDialogue";
        const string TapsKey = "KMA_PMS_DialogueTaps";
        bool savedActive;
        string savedNode;
        int savedTaps;

        [SetUp]
        public void SaveState()
        {
            savedActive = SessionState.GetBool(ActiveKey, false);
            savedNode = SessionState.GetString(NodeKey, "");
            savedTaps = SessionState.GetInt(TapsKey, 0);
        }

        [TearDown]
        public void RestoreState()
        {
            SessionState.SetBool(ActiveKey, savedActive);
            SessionState.SetString(NodeKey, savedNode);
            SessionState.SetInt(TapsKey, savedTaps);
        }

        [Test]
        public void StaleRequestIsIgnoredAndClearedWhenNoCaptureIsActive()
        {
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetString(NodeKey, "opening");
            SessionState.SetInt(TapsKey, 3);

            Assert.That(PlayModeScreenshot.TryConsumeDialogueRequest(out _, out _), Is.False);

            SessionState.SetBool(ActiveKey, true);
            Assert.That(PlayModeScreenshot.TryConsumeDialogueRequest(out _, out _), Is.False,
                "A request left over from an earlier capture must not revive on a later Play.");
        }

        [Test]
        public void ActiveRequestIsReturnedOnceThenConsumed()
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetString(NodeKey, "course_complete");
            SessionState.SetInt(TapsKey, 2);

            Assert.That(PlayModeScreenshot.TryConsumeDialogueRequest(out string node, out int taps), Is.True);
            Assert.That(node, Is.EqualTo("course_complete"));
            Assert.That(taps, Is.EqualTo(2));
            Assert.That(PlayModeScreenshot.TryConsumeDialogueRequest(out _, out _), Is.False);
        }
    }
}
