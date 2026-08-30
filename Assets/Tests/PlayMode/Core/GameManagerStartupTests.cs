using System;
using System.Collections;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Gameplay.Core
{
    public sealed class GameManagerStartupTests
    {
        int originalTargetFrameRate;
        int originalVSyncCount;

        [SetUp]
        public void SetUp()
        {
            originalTargetFrameRate = Application.targetFrameRate;
            originalVSyncCount = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameManager manager in UnityEngine.Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }

            foreach (SceneRouter router in UnityEngine.Object.FindObjectsByType<SceneRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(router.gameObject);
            }

            foreach (RecordingSettingsService service in
                     UnityEngine.Object.FindObjectsByType<RecordingSettingsService>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(service.gameObject);
            }

            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVSyncCount;
        }

        [Test]
        public void FreshManager_RestoresDefaultsBeforeLoadingMenuOnce()
        {
            SceneRouter router = CreateRouter();
            GameManager manager = CreateInactiveManager();
            int menuLoads = 0;
            manager.ConfigureStartup(
                SaveData.CreateDefault,
                _ => Assert.Fail("Startup must not save unchanged defaults."),
                router,
                sceneName =>
                {
                    Assert.That(sceneName, Is.EqualTo("Menu"));
                    Assert.That(manager.Session, Is.SameAs(router.Session));
                    Assert.That(manager.Session.Lives, Is.EqualTo(5));
                    Assert.That(manager.Session.Records.Count, Is.EqualTo(7));
                    menuLoads++;
                });

            manager.gameObject.SetActive(true);

            Assert.That(menuLoads, Is.EqualTo(1));
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
            Assert.That(QualitySettings.vSyncCount, Is.Zero);
        }

        [Test]
        public void PreparedSave_IsRestoredBeforeMenuAndSettingsAreApplied()
        {
            SaveData prepared = SaveData.CreateDefault();
            prepared.lives = 2;
            SubjectRecordData sprint = Array.Find(prepared.subjects, record => record.id == SubjectId.Sprint);
            sprint.passed = true;
            sprint.bestScore = 0.875f;
            sprint.bestRank = Rank.A;
            sprint.failedVisits = 1;
            prepared.settings.musicVol = 0.25f;

            SceneRouter router = CreateRouter();
            var serviceObject = new GameObject("GameManagerStartupTests.SettingsService");
            var settingsService = serviceObject.AddComponent<RecordingSettingsService>();
            GameManager manager = CreateInactiveManager();
            int menuLoads = 0;
            manager.ConfigureStartup(
                () => prepared,
                _ => Assert.Fail("Startup must not save restored data."),
                router,
                sceneName =>
                {
                    Assert.That(sceneName, Is.EqualTo("Menu"));
                    Assert.That(router.Session.Lives, Is.EqualTo(2));
                    Assert.That(router.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
                    Assert.That(router.Session.GetRecord(SubjectId.Sprint).BestScore, Is.EqualTo(0.875f));
                    Assert.That(router.Session.GetRecord(SubjectId.Sprint).BestRank, Is.EqualTo(Rank.A));
                    Assert.That(router.Session.GetRecord(SubjectId.Sprint).FailedVisits, Is.EqualTo(1));
                    Assert.That(settingsService.LastSettings, Is.SameAs(prepared.settings));
                    menuLoads++;
                },
                new[] { settingsService });

            manager.gameObject.SetActive(true);

            Assert.That(menuLoads, Is.EqualTo(1));
        }

        [Test]
        public void ApplicationPauseTrue_SavesExactlyOnce()
        {
            SceneRouter router = CreateRouter();
            GameManager manager = CreateInactiveManager();
            int saves = 0;
            SaveData saved = null;
            manager.ConfigureStartup(
                SaveData.CreateDefault,
                data =>
                {
                    saves++;
                    saved = data;
                },
                router,
                _ => { });
            manager.gameObject.SetActive(true);

            manager.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);

            Assert.That(saves, Is.EqualTo(1));
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.lives, Is.EqualTo(5));

            manager.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
            Assert.That(saves, Is.EqualTo(1));
        }

        [Test]
        public void SubjectCompleted_SavesExactlyOnce()
        {
            SceneRouter router = CreateRouter();
            int saves = 0;
            GameManager manager = CreateInitializedManager(router, _ => saves++);

            router.Session.StartSubject(SubjectId.Sprint);
            router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(true, 0.9f, Rank.A));

            Assert.That(manager.Session.GetRecord(SubjectId.Sprint).Passed, Is.True);
            Assert.That(saves, Is.EqualTo(1));
        }

        [Test]
        public void LifeLost_SavesExactlyOnce()
        {
            SceneRouter router = CreateRouter();
            int saves = 0;
            GameManager manager = CreateInitializedManager(router, _ => saves++);

            router.Session.StartSubject(SubjectId.Sprint);
            router.Session.SubmitResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F));
            router.Session.CompletePunishment();
            router.SubmitSubjectResult(SubjectId.Sprint, new MinigameResult(false, 0f, Rank.F));

            Assert.That(manager.Session.Lives, Is.EqualTo(4));
            Assert.That(saves, Is.EqualTo(1));
        }

        [Test]
        public void UpdateSettings_SavesExactlyOnce()
        {
            SceneRouter router = CreateRouter();
            int saves = 0;
            SaveData saved = null;
            GameManager manager = CreateInitializedManager(router, data =>
            {
                saves++;
                saved = data;
            });
            var updated = new Settings
            {
                musicVol = 0.25f,
                sfxVol = 0.5f,
                vibration = false,
                rhythmOffsetMs = -42f
            };

            manager.UpdateSettings(updated);

            Assert.That(saves, Is.EqualTo(1));
            Assert.That(saved.settings, Is.SameAs(updated));
        }

        [UnityTest]
        public IEnumerator DuplicateManager_DoesNotInitializeOrLoadMenu()
        {
            SceneRouter router = CreateRouter();
            int menuLoads = 0;
            GameManager first = CreateInactiveManager("GameManagerStartupTests.First");
            first.ConfigureStartup(SaveData.CreateDefault, _ => { }, router, _ => menuLoads++);
            first.gameObject.SetActive(true);

            GameManager duplicate = CreateInactiveManager("GameManagerStartupTests.Duplicate");
            duplicate.ConfigureStartup(SaveData.CreateDefault, _ => { }, router, _ => menuLoads++);
            duplicate.gameObject.SetActive(true);

            Assert.That(GameManager.Instance, Is.SameAs(first));
            Assert.That(duplicate.Session, Is.Null);
            Assert.That(menuLoads, Is.EqualTo(1));

            yield return null;

            GameManager[] managers = UnityEngine.Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(managers, Has.Length.EqualTo(1));
            Assert.That(managers[0], Is.SameAs(first));
        }

        static SceneRouter CreateRouter() =>
            new GameObject("GameManagerStartupTests.Router").AddComponent<SceneRouter>();

        static GameManager CreateInactiveManager(string name = "GameManagerStartupTests.Manager")
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(false);
            return gameObject.AddComponent<GameManager>();
        }

        static GameManager CreateInitializedManager(SceneRouter router, Action<SaveData> save)
        {
            GameManager manager = CreateInactiveManager();
            manager.ConfigureStartup(SaveData.CreateDefault, save, router, _ => { });
            manager.gameObject.SetActive(true);
            return manager;
        }

        sealed class RecordingSettingsService : MonoBehaviour, IGameSettingsService
        {
            public Settings LastSettings { get; private set; }

            public void ApplySettings(Settings settings) => LastSettings = settings;
        }
    }
}
