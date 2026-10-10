using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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
        bool frogJumpTutorialSeen;
        bool startupConfigured;
        bool initialized;
        bool journeySaveInProgress;

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
            if (router.IsTransitioning)
                return;

            router.ResetCampaign();
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
            int index = Array.IndexOf(Enum.GetValues(typeof(SubjectId)), subject);
            return tutorialSeen != null && index >= 0 && index < tutorialSeen.Length && tutorialSeen[index];
        }

        public void MarkTutorialSeen(SubjectId subject)
        {
            if (!initialized)
                throw new InvalidOperationException("GameManager has not initialized.");
            if (HasSeenTutorial(subject))
                return;

            int index = Array.IndexOf(Enum.GetValues(typeof(SubjectId)), subject);
            if (index < 0)
                return;
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

        public bool HasSeenFrogJumpTutorial => frogJumpTutorialSeen;

        public void MarkFrogJumpTutorialSeen()
        {
            if (!initialized)
                throw new InvalidOperationException("GameManager has not initialized.");
            if (frogJumpTutorialSeen)
                return;
            frogJumpTutorialSeen = true;
            SaveCurrentState();
        }

        void ConfigureProductionStartup()
        {
            saveSystem = new SaveSystem();
            loadData = saveSystem.Load;
            saveData = saveSystem.Save;
            hasExistingSave = () => saveSystem.HasLoadedValidSave;
            router = SceneRouter.EnsurePersistentInstance();
            loadScene = sceneName => router.TryLoadScene(sceneName);

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

            SaveData loaded = loadData() ?? SaveData.CreateDefault();
            session = new GameSession();
            session.Restore(loaded);
            settings = loaded.settings ?? Settings.CreateDefault();
            tutorialSeen = CloneTutorialFlags(loaded.tutorialSeen);
            frogJumpTutorialSeen = loaded.frogJumpTutorialSeen;
            HasSavedCampaign = hasExistingSave != null && hasExistingSave() && !loaded.settingsOnly;

            router.LoadSession(session);
            router.ConfigureJourneyPersistence(TryPersistSession);
            SubscribeToRouter();
            ApplySettings();
            initialized = true;
            // The forfeited frog jump cost a life; write it now so a second kill cannot re-roll it.
            if (session.ForfeitedFrogJumpOnRestore && HasSavedCampaign)
                TryPersistSession(out _);
            loadScene(MenuScene);
        }

        void SubscribeToRouter() => router.SessionChanged += OnSessionChanged;

        void UnsubscribeFromRouter()
        {
            if (router == null)
                return;

            router.SessionChanged -= OnSessionChanged;
        }

        void OnSessionChanged()
        {
            // Journey commits persist before notifying this view-facing event.
        }

        public bool TryPersistSession(out string error)
        {
            error = null;
            if (!initialized || journeySaveInProgress)
            {
                error = "The game session is not ready to save.";
                return false;
            }

            journeySaveInProgress = true;
            try
            {
                SaveData current = session.ToSaveData();
                current.settingsOnly = false;
                current.settings = settings;
                current.tutorialSeen = CloneTutorialFlags(tutorialSeen);
                current.frogJumpTutorialSeen = frogJumpTutorialSeen;
                saveData(current);
                HasSavedCampaign = true;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                journeySaveInProgress = false;
            }
        }

        public bool TryMarkJourneyDialogueSeen(string seenKey, out string error)
        {
            error = null;
            if (!initialized || session == null || string.IsNullOrWhiteSpace(seenKey))
            {
                error = "The journey dialogue cannot be saved right now.";
                return false;
            }
            if (session.Journey.IsDialogueSeen(seenKey)) return true;
            session.Journey.MarkDialogueSeen(seenKey);
            if (TryPersistSession(out error)) return true;
            session.Journey.UnmarkDialogueSeen(seenKey);
            return false;
        }

        public bool TryMarkCelebrationSeen(out string error)
        {
            error = null;
            if (!initialized || session == null)
            {
                error = "The celebration cannot be saved right now.";
                return false;
            }
            if (session.Journey.CelebrationSeen) return true;
            if (!session.Journey.MarkCelebrationSeen())
            {
                error = "The course is not complete.";
                return false;
            }
            if (TryPersistSession(out error)) return true;
            session.Journey.UnmarkCelebrationSeen();
            return false;
        }

        void SaveCurrentState()
        {
            SaveData current = session.ToSaveData();
            current.settingsOnly = !HasSavedCampaign;
            current.settings = settings;
            current.tutorialSeen = CloneTutorialFlags(tutorialSeen);
            current.frogJumpTutorialSeen = frogJumpTutorialSeen;
            saveData(current);

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
    }
}
