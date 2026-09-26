using System;
using System.Runtime.CompilerServices;
using KMA.Gameplay.UI;
using KMA.Gameplay.Core;
using UnityEngine;

[assembly: InternalsVisibleTo("KMA.Gameplay.Football.PlayMode.Tests")]

namespace KMA.Gameplay
{
    public sealed class FootballController : MinigameBase
    {
        static FootballDifficulty preferredDifficulty = FootballDifficulty.Normal;
        static bool retryRequested;

        [SerializeField] FootballDifficultyConfig difficultyConfig;
        [SerializeField] FootballInputBridge inputBridge;
        [SerializeField] FootballPresentation presentation;
        [SerializeField] FootballHud hud;
        [SerializeField] FootballResultPanel resultPanel;

        FootballRules rules;
        MinigameResult lastResult;
        FootballDifficulty selectedDifficulty = FootballDifficulty.Normal;
        bool ready;
        bool listenersBound;
        bool matchRequested;
        bool rulesStarted;
        bool resultSubmitted;
        bool appPaused;
        bool hasFocus = true;
        FootballFlightSimulation kickedFlight, soundedOutcomeFlight;

        public FootballRules Rules => rules;
        public MinigameResult LastResult => lastResult;

        public void Configure(FootballDifficultyConfig config, FootballInputBridge input,
            FootballPresentation view, FootballHud gameHud, FootballResultPanel result)
        {
            difficultyConfig = config;
            inputBridge = input;
            presentation = view;
            hud = gameHud;
            resultPanel = result;
        }

        public bool BeginMatch(FootballDifficulty difficulty)
        {
            if (!ready || matchRequested || rules == null || rules.State != FootballState.Start ||
                PresentationPhase != MinigamePhase.Tutorial)
                return false;

            selectedDifficulty = difficulty;
            preferredDifficulty = difficulty;
            rules = new FootballRules(difficultyConfig.Get(difficulty));
            matchRequested = true;
            rulesStarted = false;
            resultSubmitted = false;
            lastResult = null;
            hud.HideStart();
            hud.ShowCountdown("SẴN SÀNG!");
            SetTutorialGate(false);
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            SetTutorialGate(true);
            if (!ValidateReferences())
            {
                Debug.LogError("FootballController disabled: required scene references are missing", this);
                enabled = false;
                return;
            }

            selectedDifficulty = preferredDifficulty;
            rules = new FootballRules(difficultyConfig.Get(selectedDifficulty));
            inputBridge.Configure(hud.DirectionSlider, hud.ShootButton);
            ready = true;
            BindListeners();
            inputBridge.SetEnabled(false, false);

            bool startRetry = retryRequested;
            retryRequested = false;
            if (startRetry)
                BeginMatch(selectedDifficulty);
            else
                hud.ShowStart(selectedDifficulty);
            hud.Render(rules);
            presentation.Render(rules);
        }

        public bool ValidateReferences() => difficultyConfig && inputBridge && presentation && hud && resultPanel &&
            hud.ValidateReferences() && presentation.ValidateReferences() && resultPanel.ValidateReferences() &&
            hud.DirectionSlider && hud.ShootButton;

        protected override void Update()
        {
            if (!ready) return;
            if (IsSuspended)
            {
                inputBridge.CancelActivePointer();
                inputBridge.SetEnabled(false, false);
                presentation.Render(rules);
                hud.Render(rules);
                return;
            }
            base.Update();
            hud.Render(rules);
            presentation.Render(rules);
        }

        protected override void TickPlay(float deltaTime)
        {
            if (!ready || IsSuspended || !matchRequested || PresentationPhase != MinigamePhase.Play || resultSubmitted)
                return;

            if (!rulesStarted)
            {
                rulesStarted = rules.Start();
                hud.HideCountdown();
                RefreshInputAvailability();
            }
            if (!rulesStarted)
                return;

            FootballFlightSimulation previousFlight = rules.Flight;
            rules.Tick(deltaTime);
            UpdateAudio(rules.Flight ?? previousFlight);
            RefreshInputAvailability();
            if (rules.State == FootballState.MatchResult)
            {
                resultSubmitted = true;
                inputBridge.SetEnabled(false, false);
                lastResult = rules.BuildResult();
                resultPanel.SetGoals(rules.Goals);
                Finish(lastResult);
            }
        }

        protected override MinigameHudState BuildHudState() => MinigameHudState.Empty;

        void UpdateAudio(FootballFlightSimulation flight)
        {
            if (flight == null) return;
            if (flight.Time > 0f && flight != kickedFlight)
            {
                kickedFlight = flight;
                GameAudio.Play(GameSound.Kick);
            }
            if (!flight.Outcome.HasValue || flight == soundedOutcomeFlight) return;
            soundedOutcomeFlight = flight;
            GameAudio.Play(flight.Outcome.Value switch
            {
                FootballOutcome.Goal => GameSound.Cheer,
                FootballOutcome.Saved => GameSound.Save,
                FootballOutcome.Post or FootballOutcome.Crossbar => GameSound.Post,
                _ => GameSound.Miss
            });
        }

        void SetAim(float direction)
        {
            if (ready && matchRequested && PresentationPhase == MinigamePhase.Play && rules.SetAim(direction))
                RefreshInputAvailability();
        }

        void BeginCharge()
        {
            if (ready && matchRequested && PresentationPhase == MinigamePhase.Play && rules.BeginCharge())
                RefreshInputAvailability();
        }

        void ReleaseShot()
        {
            if (ready && matchRequested && PresentationPhase == MinigamePhase.Play && rules.ReleaseShot())
            {
                presentation.HidePreview();
                RefreshInputAvailability();
            }
        }

        void CancelCharge()
        {
            if (rules != null)
                rules.CancelCharge();
            RefreshInputAvailability();
            if (presentation && rules != null) presentation.Render(rules);
        }

        void RefreshInputAvailability()
        {
            if (inputBridge == null || !ready || IsSuspended || !matchRequested ||
                PresentationPhase != MinigamePhase.Play || resultSubmitted)
            {
                inputBridge?.SetEnabled(false, false);
                return;
            }
            inputBridge.SetEnabled(rules.State == FootballState.Aiming,
                rules.State == FootballState.Aiming || rules.State == FootballState.Charging);
        }

        void OnApplicationPause(bool paused)
        {
            appPaused = paused;
            if (paused)
            {
                inputBridge?.CancelActivePointer();
                inputBridge?.SetEnabled(false, false);
            }
            else
                RefreshInputAvailability();
        }

        void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused)
            {
                inputBridge?.CancelActivePointer();
                inputBridge?.SetEnabled(false, false);
            }
            else
                RefreshInputAvailability();
        }

        void OnStartRequested(FootballDifficulty difficulty) => BeginMatch(difficulty);

        void OnResultAction(string action)
        {
            if (action == ResultPanelActions.Retry)
                retryRequested = true;
            else if (action == ResultPanelActions.Continue)
                retryRequested = false;
        }

        void OnEnable()
        {
            if (!ready) return;
            BindListeners();
            RefreshInputAvailability();
        }

        void BindListeners()
        {
            if (!ready || listenersBound) return;
            inputBridge.AimChanged += SetAim;
            inputBridge.ShootPressed += BeginCharge;
            inputBridge.ShootReleased += ReleaseShot;
            inputBridge.ShootCancelled += CancelCharge;
            hud.StartRequested += OnStartRequested;
            resultPanel.ActionRequested += OnResultAction;
            listenersBound = true;
        }

        public void ExitToMap()
        {
            if (!ready) return;
            inputBridge.CancelActivePointer();
            if (resultSubmitted)
            {
                // The result owner commits the loss/win exactly once before routing.
                if (resultPanel.IsVisible) resultPanel.Continue();
                return;
            }
            SceneRouter.Instance?.ExitActiveSubjectToMap();
        }

        void OnDisable()
        {
            if (inputBridge)
            {
                inputBridge.CancelActivePointer();
                inputBridge.SetEnabled(false, false);
                if (listenersBound)
                {
                    inputBridge.AimChanged -= SetAim;
                    inputBridge.ShootPressed -= BeginCharge;
                    inputBridge.ShootReleased -= ReleaseShot;
                    inputBridge.ShootCancelled -= CancelCharge;
                }
            }
            if (listenersBound && hud) hud.StartRequested -= OnStartRequested;
            if (listenersBound && resultPanel) resultPanel.ActionRequested -= OnResultAction;
            listenersBound = false;
            if (presentation) presentation.HidePreview();
        }

        bool IsSuspended => appPaused || !hasFocus || Time.timeScale == 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSessionPreference()
        {
            preferredDifficulty = FootballDifficulty.Normal;
            retryRequested = false;
        }

        internal static void ResetSessionPreferenceForTests() => ResetSessionPreference();
    }
}
