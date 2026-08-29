using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

namespace KMA.Input
{
    public sealed class GameplayInputRouter : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] InputActionReference tapAction;
        [SerializeField] InputActionReference holdAction;
        [SerializeField] InputActionReference swipeAction;
        [SerializeField] InputActionReference rhythmAction;
        [SerializeField] string sprintActionMapName = "Sprint";
        [SerializeField] string enduranceActionMapName = "Endurance";
        [SerializeField] string bossActionMapName = "Boss";
        [SerializeField] string punishmentActionMapName = "Punishment";
        [SerializeField] string uiActionMapName = "UI";
        [SerializeField] double rhythmOffsetMs;

        TapMashInputDetector tapMashDetector;
        RhythmBeatInputDetector rhythmBeatDetector;
        HoldInputDetector holdDetector;
        AlternateTapInputDetector alternateTapDetector;
        SwipeInputDetector swipeDetector;
        int screenTapAreaCount;
        bool subscribed;

        public InputActionAsset InputActions => inputActions;
        public string SprintActionMapName => sprintActionMapName;
        public string EnduranceActionMapName => enduranceActionMapName;
        public string BossActionMapName => bossActionMapName;
        public string PunishmentActionMapName => punishmentActionMapName;
        public string UiActionMapName => uiActionMapName;
        public double RhythmOffsetMs { get => rhythmOffsetMs; set => rhythmOffsetMs = value; }

        void OnEnable()
        {
            if (subscribed)
                return;

            EnhancedTouch.EnhancedTouchSupport.Enable();
            EnhancedTouch.Touch.onFingerDown += OnFingerDown;
            EnhancedTouch.Touch.onFingerMove += OnFingerMove;
            EnhancedTouch.Touch.onFingerUp += OnFingerUp;
            subscribed = true;
        }

        void OnDisable()
        {
            if (!subscribed)
                return;

            EnhancedTouch.Touch.onFingerDown -= OnFingerDown;
            EnhancedTouch.Touch.onFingerMove -= OnFingerMove;
            EnhancedTouch.Touch.onFingerUp -= OnFingerUp;
            EnhancedTouch.EnhancedTouchSupport.Disable();
            subscribed = false;
        }

        public void SetDetectors(
            TapMashInputDetector tapMash,
            RhythmBeatInputDetector rhythmBeat,
            HoldInputDetector hold,
            AlternateTapInputDetector alternateTap,
            SwipeInputDetector swipe)
        {
            tapMashDetector = tapMash;
            rhythmBeatDetector = rhythmBeat;
            holdDetector = hold;
            alternateTapDetector = alternateTap;
            swipeDetector = swipe;
        }

        public void FeedPointerDown(Vector2 position) => FeedPointerDown(position, Timestamp());

        public void FeedPointerMove(Vector2 position) => FeedPointerMove(position, Timestamp());

        public void FeedPointerUp(Vector2 position) => FeedPointerUp(position, Timestamp());

        public void FeedPointerDownForTest(Vector2 position, double timestamp) => FeedPointerDown(position, timestamp);

        public void FeedPointerMoveForTest(Vector2 position, double timestamp) => FeedPointerMove(position, timestamp);

        public void FeedPointerUpForTest(Vector2 position, double timestamp) => FeedPointerUp(position, timestamp);

        public void FeedRhythmTap(double beatDsp) => FeedRhythmTap(AudioSettings.dspTime, beatDsp);

        public void FeedRhythmTapForTest(double inputDsp, double beatDsp) => FeedRhythmTap(inputDsp, beatDsp);

        internal void RegisterScreenTapArea() => screenTapAreaCount++;

        internal void UnregisterScreenTapArea()
        {
            if (screenTapAreaCount > 0)
                screenTapAreaCount--;
        }

        void OnFingerDown(EnhancedTouch.Finger finger)
        {
            if (screenTapAreaCount == 0)
                FeedPointerDown(finger.screenPosition);
        }

        void OnFingerMove(EnhancedTouch.Finger finger)
        {
            if (screenTapAreaCount == 0)
                FeedPointerMove(finger.screenPosition);
        }

        void OnFingerUp(EnhancedTouch.Finger finger)
        {
            if (screenTapAreaCount == 0)
                FeedPointerUp(finger.screenPosition);
        }

        void FeedPointerDown(Vector2 position, double timestamp)
        {
            tapMashDetector?.FeedTap(timestamp);
            holdDetector?.FeedDown(timestamp);
            alternateTapDetector?.FeedTap(position.x < Screen.width * .5f ? Side.Left : Side.Right, timestamp);
            swipeDetector?.FeedSample(position, timestamp);
        }

        void FeedPointerMove(Vector2 position, double timestamp) => swipeDetector?.FeedSample(position, timestamp);

        void FeedPointerUp(Vector2 position, double timestamp)
        {
            holdDetector?.FeedUp(timestamp);
            if (swipeDetector == null)
                return;

            swipeDetector.FeedSample(position, timestamp);
            swipeDetector.FeedEnd();
        }

        void FeedRhythmTap(double inputDsp, double beatDsp)
        {
            rhythmBeatDetector?.FeedTap(inputDsp + rhythmOffsetMs / 1000d, beatDsp);
        }

        static double Timestamp() => Time.realtimeSinceStartupAsDouble;
    }
}
