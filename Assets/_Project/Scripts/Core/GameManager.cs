using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: InternalsVisibleTo("KMA.Gameplay.Core.PlayMode.Tests")]
[assembly: InternalsVisibleTo("KMA.Gameplay.Progression.PlayMode.Tests")]

namespace KMA.Gameplay.Core
{
    public interface IGameSettingsService
    {
        void ApplySettings(Settings settings);
    }

    public sealed class GameManager : MonoBehaviour
    {
        const string MenuScene = "Menu";

        static GameManager instance;

        readonly List<IGameSettingsService> settingsServices = new List<IGameSettingsService>();
        SaveSystem saveSystem;
        Func<SaveData> loadData;
        Action<SaveData> saveData;
        Func<bool> hasExistingSave;
        Action<string> loadScene;
        SceneRouter router;
        GameSession session;
        Settings settings;
        bool[] tutorialSeen;
        bool gameCompleted;
        bool startupConfigured;
        bool initialized;

        public static GameManager Instance => instance;
        public SaveSystem SaveSystem => saveSystem;
        public GameSession Session => session;
        public Settings Settings => settings;
        public bool IsInitialized => initialized;
        public bool HasSavedCampaign { get; private set; }

        public event Action<Settings> SettingsChanged;

        internal void ConfigureStartup(
            Func<SaveData> configuredLoad,
            Action<SaveData> configuredSave,
            SceneRouter configuredRouter,
            Action<string> configuredSceneLoader,
            IEnumerable<IGameSettingsService> configuredServices = null,
            Func<bool> configuredHasExistingSave = null)
        {
            if (initialized)
                throw new InvalidOperationException("GameManager startup has already completed.");

            loadData = configuredLoad ?? throw new ArgumentNullException(nameof(configuredLoad));
            saveData = configuredSave ?? throw new ArgumentNullException(nameof(configuredSave));
            router = configuredRouter ?? throw new ArgumentNullException(nameof(configuredRouter));
            loadScene = configuredSceneLoader ?? throw new ArgumentNullException(nameof(configuredSceneLoader));
            hasExistingSave = configuredHasExistingSave;
            settingsServices.Clear();
            if (configuredServices != null)
            {
                foreach (IGameSettingsService service in configuredServices)
                    AddSettingsService(service);
            }

            startupConfigured = true;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (!startupConfigured)
                ConfigureProductionStartup();

            InitializeStartup();
        }

        void OnDestroy()
        {
            if (instance != this)
                return;

            UnsubscribeFromRouter();
            instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && initialized)
                SaveCurrentState();
        }

        public void RegisterSettingsService(IGameSettingsService service)
        {
            AddSettingsService(service);
            if (initialized)
                service.ApplySettings(settings);
        }

        public void UnregisterSettingsService(IGameSettingsService service)
        {
            if (service != null)
                settingsServices.Remove(service);
        }

        public void StartNewGame()
        {
            if (!initialized) throw new InvalidOperationException("GameManager has not initialized.");
            session.ResetCampaign();
            gameCompleted = false;
            SaveCurrentState();
            loadScene(MenuScene);
        }

        public void UpdateSettings(Settings updatedSettings)
        {
            if (updatedSettings == null)
                throw new ArgumentNullException(nameof(updatedSettings));

            settings = updatedSettings;
            ApplySettings();
            SettingsChanged?.Invoke(settings);
            SaveCurrentState();
        }

        public bool HasSeenTutorial(SubjectId subject)
        {
            int index = (int)subject;
            return tutorialSeen != null && index >= 0 && index < tutorialSeen.Length && tutorialSeen[index];
        }

        public void MarkTutorialSeen(SubjectId subject)
        {
            if (!initialized)
                throw new InvalidOperationException("GameManager has not initialized.");
            if (HasSeenTutorial(subject))
                return;

            int index = (int)subject;
            int requiredLength = Enum.GetValues(typeof(SubjectId)).Length;
            if (tutorialSeen == null || tutorialSeen.Length < requiredLength)
            {
                var resized = new bool[requiredLength];
                if (tutorialSeen != null)
                    Array.Copy(tutorialSeen, resized, tutorialSeen.Length);
                tutorialSeen = resized;
            }

            tutorialSeen[index] = true;
            SaveCurrentState();
        }

        void ConfigureProductionStartup()
        {
            saveSystem = new SaveSystem();
            loadData = saveSystem.Load;
            saveData = saveSystem.Save;
            hasExistingSave = () => saveSystem.HasSave;
            router = SceneRouter.EnsurePersistentInstance();
            loadScene = LoadScene;

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IGameSettingsService service)
                    AddSettingsService(service);
            }
        }

        void InitializeStartup()
        {
            if (initialized)
                return;

            HasSavedCampaign = hasExistingSave != null && hasExistingSave();
            SaveData loaded = loadData() ?? SaveData.CreateDefault();
            session = new GameSession();
            session.Restore(loaded);
            settings = loaded.settings ?? Settings.CreateDefault();
            tutorialSeen = CloneTutorialFlags(loaded.tutorialSeen);
            gameCompleted = loaded.gameCompleted;

            router.LoadSession(session);
            SubscribeToRouter();
            ApplySettings();
            initialized = true;
            loadScene(MenuScene);
        }

        void SubscribeToRouter() => router.SessionChanged += OnSessionChanged;

        void UnsubscribeFromRouter()
        {
            if (router == null)
                return;

            router.SessionChanged -= OnSessionChanged;
        }

        void OnSessionChanged() => SaveCurrentState();

        void SaveCurrentState()
        {
            SaveData current = session.ToSaveData();
            current.settings = settings;
            current.tutorialSeen = CloneTutorialFlags(tutorialSeen);
            current.gameCompleted = gameCompleted;
            saveData(current);
            HasSavedCampaign = true;
        }

        void ApplySettings()
        {
            for (int i = settingsServices.Count - 1; i >= 0; i--)
            {
                IGameSettingsService service = settingsServices[i];
                if (service is UnityEngine.Object unityObject && unityObject == null)
                {
                    settingsServices.RemoveAt(i);
                    continue;
                }

                service.ApplySettings(settings);
            }
        }

        void AddSettingsService(IGameSettingsService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (!settingsServices.Contains(service))
                settingsServices.Add(service);
        }

        static bool[] CloneTutorialFlags(bool[] source)
        {
            if (source == null)
                return SaveData.CreateDefault().tutorialSeen;

            var copy = new bool[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        static void LoadScene(string sceneName) =>
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
}
