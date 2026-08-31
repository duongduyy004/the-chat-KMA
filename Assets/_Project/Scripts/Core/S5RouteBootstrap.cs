using System;
using System.Reflection;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Gameplay.Core
{
    static class S5RouteBootstrap
    {
        static readonly SceneRouter.SubjectScene[] Routes =
        {
            new SceneRouter.SubjectScene { Subject = SubjectId.Sprint, SceneName = "MG_Sprint" },
            new SceneRouter.SubjectScene { Subject = SubjectId.Endurance, SceneName = "MG_Endurance" },
            new SceneRouter.SubjectScene { Subject = SubjectId.Volleyball, SceneName = "MG_Volleyball" },
            new SceneRouter.SubjectScene { Subject = SubjectId.Basketball, SceneName = "MG_Basketball" },
            new SceneRouter.SubjectScene { Subject = SubjectId.PingPong, SceneName = "MG_PingPong" },
            new SceneRouter.SubjectScene { Subject = SubjectId.Badminton, SceneName = "MG_Badminton" },
            new SceneRouter.SubjectScene { Subject = SubjectId.Football, SceneName = "MG_Football" }
        };

        static readonly FieldInfo SubjectScenes = typeof(SceneRouter).GetField(
            "subjectScenes", BindingFlags.Instance | BindingFlags.NonPublic);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Apply;
            SceneManager.sceneLoaded += Apply;
            Apply(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void Apply(Scene scene, LoadSceneMode mode)
        {
            var router = SceneRouter.Instance;
            if (router != null && SubjectScenes != null)
                SubjectScenes.SetValue(router, Routes);

        }
    }
}
