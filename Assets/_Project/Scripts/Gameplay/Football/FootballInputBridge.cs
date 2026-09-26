using System;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballInputBridge : MonoBehaviour
    {
        Slider directionSlider;
        FootballHoldButton shootButton;
        bool configured;
        bool listenersBound;
        bool aimEnabled;
        bool shootEnabled;

        public event Action<float> AimChanged;
        public event Action ShootPressed;
        public event Action ShootReleased;
        public event Action ShootCancelled;

        public void Configure(Slider aim, FootballHoldButton shoot)
        {
            if (aim == null)
                throw new ArgumentNullException(nameof(aim));
            if (shoot == null)
                throw new ArgumentNullException(nameof(shoot));

            CancelActivePointer();
            Unbind();
            directionSlider = aim;
            shootButton = shoot;
            configured = true;
            aimEnabled = false;
            shootEnabled = false;
            ApplyEnabledState();
            if (isActiveAndEnabled)
                Bind();
        }

        public void SetEnabled(bool aimEnabled, bool shootEnabled)
        {
            if (!configured)
                return;

            this.aimEnabled = aimEnabled;
            this.shootEnabled = shootEnabled;
            ApplyEnabledState();
        }

        public void CancelActivePointer() => shootButton?.Cancel();

        void OnEnable() => Bind();

        void OnDisable()
        {
            CancelActivePointer();
            Unbind();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                CancelActivePointer();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CancelActivePointer();
        }

        void Bind()
        {
            if (!configured || listenersBound)
                return;

            directionSlider.onValueChanged.AddListener(HandleAimChanged);
            shootButton.Pressed += HandleShootPressed;
            shootButton.Released += HandleShootReleased;
            shootButton.Cancelled += HandleShootCancelled;
            listenersBound = true;
        }

        void Unbind()
        {
            if (!listenersBound)
                return;

            directionSlider.onValueChanged.RemoveListener(HandleAimChanged);
            shootButton.Pressed -= HandleShootPressed;
            shootButton.Released -= HandleShootReleased;
            shootButton.Cancelled -= HandleShootCancelled;
            listenersBound = false;
        }

        void ApplyEnabledState()
        {
            if (directionSlider != null)
                directionSlider.interactable = aimEnabled;
            if (shootButton != null)
                shootButton.SetInteractable(shootEnabled);
        }

        void HandleAimChanged(float value)
        {
            if (aimEnabled)
                AimChanged?.Invoke(value);
        }

        void HandleShootPressed()
        {
            if (shootEnabled)
                ShootPressed?.Invoke();
        }

        void HandleShootReleased()
        {
            if (shootEnabled)
                ShootReleased?.Invoke();
        }

        void HandleShootCancelled() => ShootCancelled?.Invoke();
    }
}
