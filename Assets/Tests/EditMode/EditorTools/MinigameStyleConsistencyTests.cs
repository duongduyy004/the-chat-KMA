#if UNITY_EDITOR
using System.Linq;
using KMA.EditorTools;
using KMA.Gameplay.UI;
using KMA.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class MinigameStyleConsistencyTests
    {
        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TestCase(MinigamePrefabStyler.HudPrefab)]
        [TestCase(MinigamePrefabStyler.PhasePrefab)]
        [TestCase(MinigamePrefabStyler.ResultPrefab)]
        public void SharedPrefabsFollowTheKit(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var problems = MinigameStyleAudit.Audit(root);
                Assert.That(problems, Is.Empty, string.Join("\n", problems));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [TestCase(FootballSceneConfigurator.ScenePath)]
        [TestCase(VolleyballSceneConfigurator.ScenePath)]
        public void MinigameHudFollowsTheKit(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject hud = scene.GetRootGameObjects().Single(go => go.name == "S2_HUD_Minigame");
            var problems = MinigameStyleAudit.Audit(hud);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void PunishmentPauseButtonFollowsTheKit()
        {
            EditorSceneManager.OpenScene(MinigamePrefabStyler.PunishmentScene, OpenSceneMode.Single);
            var pause = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            var problems = MinigameStyleAudit.Audit(pause.gameObject);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void AuditCatchesOffKitSpritesColoursFontsAndLegacyText()
        {
            var root = new GameObject("Offender", typeof(RectTransform));
            try
            {
                var image = new GameObject("Knob", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(root.transform, false);
                image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                image.color = new Color32(255, 152, 0, 255);
                var small = new GameObject("Small", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                small.transform.SetParent(root.transform, false);
                small.fontSize = 18f;
                new GameObject("Legacy", typeof(RectTransform), typeof(UnityEngine.UI.Text)).transform.SetParent(root.transform, false);

                var problems = MinigameStyleAudit.Audit(root);
                Assert.That(problems.Any(p => p.Contains("Knob") && p.Contains("sprite")), Is.True, string.Join("\n", problems));
                Assert.That(problems.Any(p => p.Contains("colour")), Is.True);
                Assert.That(problems.Any(p => p.Contains("Small") && p.Contains("size")), Is.True);
                Assert.That(problems.Any(p => p.Contains("legacy")), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
