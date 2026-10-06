#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class SprintSceneCleanupTests
    {
        const string ScenePath = "Assets/_Project/Scenes/MG_Sprint.unity";
        const string PlayerPrefab = "Assets/_Project/Prefabs/Gameplay/PlayerRunnerVisual.prefab";

        [TearDown]
        public void ReleaseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void SprintSceneHasNoEmptyRankOrFxRoots()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string[] roots = scene.GetRootGameObjects().Select(root => root.name).ToArray();
            Assert.That(roots, Has.No.Member("Rank"));
            Assert.That(roots, Has.No.Member("FX"));
        }

        [Test]
        public void PlayerRunnerPrefabCarriesNoEnglishLabel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            Assert.That(prefab, Is.Not.Null);
            foreach (TMP_Text label in prefab.GetComponentsInChildren<TMP_Text>(true))
                Assert.That(label.text, Is.Not.EqualTo("PLAYER"), label.name);
        }
    }
}
#endif
