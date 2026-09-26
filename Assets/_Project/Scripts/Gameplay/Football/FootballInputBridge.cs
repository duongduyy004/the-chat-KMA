using System;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class FootballInputBridge : MonoBehaviour
    {
        Button aimButton;
        FootballHoldButton shootButton;
        bool configured;
        bool listenersBound;
        bool aimEnabled;
        bool shootEnabled;

        public event Action AimPressed;
        public event Action ShootPressed;
        public event Action ShootReleased;
        public event Action ShootCancelled;

        public void Configure(Button aim, FootballHoldButton shoot)
        {
            if (aim == null)
                throw new ArgumentNullException(nameof(aim));
            if (shoot == null)
                throw new ArgumentNullException(nameof(shoot));

            CancelActivePointer();
            Unbind();
            aimButton = aim;
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

            aimButton.onClick.AddListener(HandleAimPressed);
            shootButton.Pressed += HandleShootPressed;
            shootButton.Released += HandleShootReleased;
            shootButton.Cancelled += HandleShootCancelled;
            listenersBound = true;
        }

        void Unbind()
        {
            if (!listenersBound)
                return;

            aimButton.onClick.RemoveListener(HandleAimPressed);
            shootButton.Pressed -= HandleShootPressed;
            shootButton.Released -= HandleShootReleased;
            shootButton.Cancelled -= HandleShootCancelled;
            listenersBound = false;
        }

        void ApplyEnabledState()
        {
            if (aimButton != null)
                aimButton.interactable = aimEnabled;
            if (shootButton != null)
                shootButton.SetInteractable(shootEnabled);
        }

        void HandleAimPressed()
        {
            if (aimEnabled)
                AimPressed?.Invoke();
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
