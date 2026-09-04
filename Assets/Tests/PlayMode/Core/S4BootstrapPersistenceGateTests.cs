using System.Collections;
using System.IO;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class S4BootstrapPersistenceGateTests
    {
        const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        const string MenuSceneName = "Menu";
        const string SprintSceneName = "MG_Sprint";
        const float SceneLoadTimeoutSeconds = 15f;
        static readonly string[] ExpectedBuildScenePaths =
        {
            BootstrapScenePath,
            "Assets/_Project/Scenes/Menu.unity",
            "Assets/_Project/Scenes/Map.unity",
            "Assets/_Project/Scenes/MG_Sprint.unity",
            "Assets/_Project/Scenes/MG_Endurance.unity",
            "Assets/_Project/Scenes/MG_Boss.unity",
            "Assets/_Project/Scenes/Punishment.unity",
            "Assets/_Project/Scenes/GameOver.unity"
        };

        SaveSystem saveSystem;
        string temporarySavePath;
        byte[] originalSave;
        byte[] originalTemporarySave;
        bool hadOriginalSave;
        bool hadOriginalTemporarySave;
        int originalTargetFrameRate;
        int originalVSyncCount;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalTargetFrameRate = Application.targetFrameRate;
            originalVSyncCount = QualitySettings.vSyncCount;
            saveSystem = new SaveSystem();
            temporarySavePath = Path.Combine(Path.GetDirectoryName(saveSystem.SavePath), "save.tmp");
            hadOriginalSave = File.Exists(saveSystem.SavePath);
            hadOriginalTemporarySave = File.Exists(temporarySavePath);
            originalSave = hadOriginalSave ? File.ReadAllBytes(saveSystem.SavePath) : null;
            originalTemporarySave = hadOriginalTemporarySave ? File.ReadAllBytes(temporarySavePath) : null;

            yield return DestroyPersistentRuntime();
            saveSystem.DeleteSave();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return DestroyPersistentRuntime();
            saveSystem.DeleteSave();

            if (hadOriginalSave || hadOriginalTemporarySave)
                Directory.CreateDirectory(Path.GetDirectoryName(saveSystem.SavePath));
            if (hadOriginalSave)
                File.WriteAllBytes(saveSystem.SavePath, originalSave);
            if (hadOriginalTemporarySave)
                File.WriteAllBytes(temporarySavePath, originalTemporarySave);

            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
        }

        [UnityTest]
        public IEnumerator Bootstrap_PersistsLivesAndSprintBestRankAcrossRelaunch()
        {
            var seeded = SaveData.CreateDefault();
            seeded.lives = 3;
            saveSystem.Save(seeded);

            yield return LoadBootstrapAndWaitForMenu();

            AssertPersistentStartupGraph();
            SceneRouter router = SceneRouter.Instance;
            Assert.That(router.Session.Lives, Is.EqualTo(3));

            Assert.That(router.StartSubject(SubjectId.Sprint), Is.True);
            yield return WaitForSceneAndCompletedTransition(router, SprintSceneName);
            Assert.That(router.SubmitSubjectResult(
                SubjectId.Sprint, new MinigameResult(true, 0.9f, Rank.A)), Is.True);

            SaveData saved = saveSystem.Load();
            SubjectRecordData sprint = FindSubject(saved, SubjectId.Sprint);
            Assert.That(saved.lives, Is.EqualTo(3));
            Assert.That(sprint.passed, Is.True);
            Assert.That(sprint.bestScore, Is.EqualTo(0.9f));
            Assert.That(sprint.bestRank, Is.EqualTo(Rank.A));

            yield return DestroyPersistentRuntime();
            yield return LoadBootstrapAndWaitForMenu();

            AssertPersistentStartupGraph();
            Assert.That(GameManager.Instance.Session.Lives, Is.EqualTo(3));
            Assert.That(GameManager.Instance.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(GameManager.Instance.Session.GetRecord(SubjectId.Sprint).BestScore,
                Is.EqualTo(0.9f));
            Assert.That(GameManager.Instance.Session.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
        }

        [UnityTest]
        public IEnumerator Bootstrap_TutorialCompletionPersistsInJsonWithoutPlayerPrefs()
        {
            const string tutorialKey = "KMA.tutorialSeen.Sprint";
            PlayerPrefs.DeleteKey(tutorialKey);
            saveSystem.Save(SaveData.CreateDefault());

            yield return LoadBootstrapAndWaitForMenu();

            var store = new SaveDataTutorialSeenStore();
            store.MarkSeen(nameof(SubjectId.Sprint));

            SaveData persisted = saveSystem.Load();
            Assert.That(persisted.tutorialSeen[(int)SubjectId.Sprint], Is.True);
            Assert.That(PlayerPrefs.HasKey(tutorialKey), Is.False);
        }

        [UnityTest]
        public IEnumerator Bootstrap_ResetPreservesPreferencesAndClearsCampaignAcrossRelaunch()
        {
            var seeded = SaveData.CreateDefault();
            seeded.lives = 1;
            seeded.subjects[0].passed = true;
            seeded.subjects[0].bestScore = 0.98f;
            seeded.subjects[0].bestRank = Rank.S;
            seeded.subjects[0].failedVisits = 4;
            seeded.bossUnlocked = true;
            seeded.gameCompleted = true;
            seeded.tutorialSeen = new[] { true, false, true, false, true, false, true };
            seeded.settings.musicVol = 0.35f;
            seeded.settings.sfxVol = 0.65f;
            seeded.settings.vibration = false;
            seeded.settings.rhythmOffsetMs = -24f;
            saveSystem.Save(seeded);

            yield return LoadBootstrapAndWaitForMenu();

            SaveData current = GameManager.Instance.SaveSystem.Load();
            SaveData reset = SaveData.CreateDefault();
            reset.settings = current.settings;
            reset.tutorialSeen = current.tutorialSeen;
            GameManager.Instance.SaveSystem.Save(reset);

            yield return DestroyPersistentRuntime();
            yield return LoadBootstrapAndWaitForMenu();

            GameManager manager = GameManager.Instance;
            Assert.That(manager.Session.Lives, Is.EqualTo(5));
            foreach (SubjectId subject in System.Enum.GetValues(typeof(SubjectId)))
            {
                SubjectRecord record = manager.Session.GetRecord(subject);
                Assert.That(record.Passed, Is.False, $"{subject} passed state must reset.");
                Assert.That(record.BestScore, Is.Zero, $"{subject} score must reset.");
                Assert.That(record.BestRank, Is.EqualTo(Rank.F), $"{subject} rank must reset.");
                Assert.That(record.FailedVisits, Is.Zero, $"{subject} failures must reset.");
            }

            SaveData restored = manager.SaveSystem.Load();
            Assert.That(restored.bossUnlocked, Is.False);
            Assert.That(restored.gameCompleted, Is.False);
            Assert.That(restored.tutorialSeen,
                Is.EqualTo(new[] { true, false, true, false, true, false, true }));
            Assert.That(restored.settings.musicVol, Is.EqualTo(0.35f));
            Assert.That(restored.settings.sfxVol, Is.EqualTo(0.65f));
            Assert.That(restored.settings.vibration, Is.False);
            Assert.That(restored.settings.rhythmOffsetMs, Is.EqualTo(-24f));
            Assert.That(manager.Settings.musicVol, Is.EqualTo(0.35f));
            Assert.That(manager.Settings.sfxVol, Is.EqualTo(0.65f));
            Assert.That(manager.Settings.vibration, Is.False);
            Assert.That(manager.Settings.rhythmOffsetMs, Is.EqualTo(-24f));

            AudioManager audio = Object.FindFirstObjectByType<AudioManager>();
            HapticsService haptics = Object.FindFirstObjectByType<HapticsService>();
            Assert.That(audio.MusicVolume, Is.EqualTo(0.35f));
            Assert.That(audio.SfxVolume, Is.EqualTo(0.65f));
            Assert.That(haptics.VibrationEnabled, Is.False);
        }

        static IEnumerator LoadBootstrapAndWaitForMenu()
        {
            AssertBuildSceneOrder();

            AsyncOperation load = SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);

            while (!load.isDone)
                yield return null;

            float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
            while (!string.Equals(SceneManager.GetActiveScene().name, MenuSceneName,
                       System.StringComparison.Ordinal))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "Bootstrap did not enter Menu before the timeout.");
                yield return null;
            }
        }

        static IEnumerator WaitForSceneAndCompletedTransition(SceneRouter router, string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + SceneLoadTimeoutSeconds;
            while (!string.Equals(SceneManager.GetActiveScene().name, sceneName,
                       System.StringComparison.Ordinal) || router.IsTransitioning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    $"Route to {sceneName} did not complete before the timeout.");
                yield return null;
            }
        }

        static IEnumerator DestroyPersistentRuntime()
        {
            foreach (GameManager manager in Object.FindObjectsByType<GameManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(manager.gameObject);

            foreach (SceneRouter router in Object.FindObjectsByType<SceneRouter>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(router.gameObject);

            foreach (AudioManager audio in Object.FindObjectsByType<AudioManager>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(audio.gameObject);

            foreach (HapticsService haptics in Object.FindObjectsByType<HapticsService>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(haptics.gameObject);

            yield return null;
        }

        static void AssertPersistentStartupGraph()
        {
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.IsInitialized, Is.True);
            Assert.That(SceneRouter.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.Session, Is.SameAs(SceneRouter.Instance.Session));
            Assert.That(Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<HapticsService>(
                FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        static void AssertBuildSceneOrder()
        {
            for (int index = 0; index < ExpectedBuildScenePaths.Length; index++)
            {
                Assert.That(SceneUtility.GetScenePathByBuildIndex(index),
                    Is.EqualTo(ExpectedBuildScenePaths[index]),
                    $"Unexpected build scene at index {index}.");
            }
        }

        static SubjectRecordData FindSubject(SaveData data, SubjectId subject)
        {
            foreach (SubjectRecordData record in data.subjects)
            {
                if (record.id == subject)
                    return record;
            }

            Assert.Fail($"Missing save record for {subject}.");
            return null;
        }
    }
}
