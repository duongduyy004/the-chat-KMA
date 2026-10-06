#if UNITY_EDITOR
using KMA.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KMA.EditorTools
{
    public static class GameOverSceneAuthoring
    {
        const string ScenePath = "Assets/_Project/Scenes/GameOver.unity";
        const string IllustrationPath = "Assets/_Project/Art/UI/HomeIllustration.png";

        [MenuItem("KMA/GameOver/Apply Presentation")]
        public static void Apply()
        {
            // The assembler strips minigame HUD/phase/result roots from non-gameplay scenes.
            MinigameUIAssembler.AssembleScenePath(ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = Object.FindFirstObjectByType<GameOverScreen>(FindObjectsInactive.Include);
            screen.SetBackground(AssetDatabase.LoadAssetAtPath<Sprite>(IllustrationPath));
            // The scene keeps its baked layout; Build adds the illustration and wash to the existing veil.
            GameOverPresentationBuilder.Build(screen, null);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
