#if UNITY_EDITOR
using System;
using System.Linq;
using KMA.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.EditorTools
{
    public static class SprintSceneConfigurator
    {
        const string ScenePath = "Assets/_Project/Scenes/MG_Sprint.unity";
        const string RivalPrefabPath = "Assets/_Project/Prefabs/Gameplay/RivalRunner.prefab";
        const string RivalTypeName = "KMA.Gameplay.RivalRunnerAI";
        const string ControllerTypeName = "KMA.Gameplay.SprintController";

        static readonly SprintRivalMapping[] RequiredRivals = SprintRivalMappings.Required;

        [MenuItem("KMA/Sprint/Create or Repair Rivals")]
        public static void CreateOrRepairRivals()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RivalPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Could not load Sprint rival prefab at {RivalPrefabPath}.");

            var controller = SceneComponents(scene, ControllerTypeName).SingleOrDefault();
            if (controller == null)
                throw new InvalidOperationException("MG_Sprint must contain exactly one SprintController.");

            var rivals = SceneComponents(scene, RivalTypeName);
            if (RivalsAreValid(rivals, controller))
            {
                Debug.Log("[KMA] Sprint rivals already valid; no scene save required.");
                return;
            }

            foreach (var rival in rivals)
                UnityEngine.Object.DestroyImmediate(rival.gameObject);

            foreach (var mapping in RequiredRivals)
                CreateRival(scene, prefab, controller, mapping);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[KMA] Sprint rivals repaired and scene saved.");
        }

        static bool RivalsAreValid(MonoBehaviour[] rivals, MonoBehaviour controller)
        {
            if (rivals.Length != RequiredRivals.Length)
                return false;

            var ordered = rivals.OrderBy(RivalLane).ThenBy(rival => rival.name).ToArray();
            for (var i = 0; i < RequiredRivals.Length; i++)
            {
                var rival = ordered[i];
                var mapping = RequiredRivals[i];
                var serializedRival = new SerializedObject(rival);
                if (rival.name != mapping.Name ||
                    RivalLane(rival) != mapping.Lane ||
                    serializedRival.FindProperty("rivalIndex").intValue != mapping.RivalIndex ||
                    serializedRival.FindProperty("controller").objectReferenceValue != controller ||
                    AssetDatabase.GetAssetPath(serializedRival.FindProperty("profileAsset").objectReferenceValue) !=
                    mapping.ProfilePath ||
                    rival.transform.localPosition != mapping.LocalPosition ||
                    PrefabUtility.GetCorrespondingObjectFromSource(rival.gameObject) == null ||
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rival.gameObject) != RivalPrefabPath)
                    return false;
            }

            return true;
        }

        static void CreateRival(Scene scene, GameObject prefab, MonoBehaviour controller, SprintRivalMapping mapping)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var rival = instance.GetComponents<MonoBehaviour>()
                .Single(component => component != null && component.GetType().FullName == RivalTypeName);
            var profile = AssetDatabase.LoadMainAssetAtPath(mapping.ProfilePath);
            if (profile == null)
                throw new InvalidOperationException($"Could not load Sprint rival profile at {mapping.ProfilePath}.");

            var serializedGameObject = new SerializedObject(instance);
            serializedGameObject.FindProperty("m_Name").stringValue = mapping.Name;
            serializedGameObject.ApplyModifiedPropertiesWithoutUndo();

            var serializedTransform = new SerializedObject(instance.transform);
            serializedTransform.FindProperty("m_LocalPosition").vector3Value = mapping.LocalPosition;
            serializedTransform.ApplyModifiedPropertiesWithoutUndo();

            var serializedRival = new SerializedObject(rival);
            serializedRival.FindProperty("controller").objectReferenceValue = controller;
            serializedRival.FindProperty("profileAsset").objectReferenceValue = profile;
            serializedRival.FindProperty("lane").intValue = mapping.Lane;
            serializedRival.FindProperty("rivalIndex").intValue = mapping.RivalIndex;
            serializedRival.ApplyModifiedPropertiesWithoutUndo();
        }

        static int RivalLane(MonoBehaviour rival) => new SerializedObject(rival).FindProperty("lane").intValue;

        static MonoBehaviour[] SceneComponents(Scene scene, string fullTypeName) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(component => component != null && component.GetType().FullName == fullTypeName)
            .ToArray();
    }
}
#endif
