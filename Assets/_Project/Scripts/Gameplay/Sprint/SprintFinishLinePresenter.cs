using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class SprintFinishLinePresenter : MonoBehaviour
    {
        SprintController controller;
        GameObject finishRoot;

        public bool IsVisible { get; private set; }

        public void Configure(SprintController sprintController, GameObject finishLineRoot)
        {
            controller = sprintController;
            finishRoot = finishLineRoot;
            RefreshForTest();
        }

        public void RefreshForTest()
        {
            float distance = controller == null ? 0f : controller.Snapshot.Distance;
            IsVisible = controller != null && SprintUiLayout.FinishVisible(distance);
            if (finishRoot == null)
                return;

            finishRoot.SetActive(IsVisible);
            if (!IsVisible || finishRoot.transform is not RectTransform rect)
                return;

            // The ribbon enters from beyond the right edge at the reveal distance and slides in as
            // the runner closes on the line, rather than appearing on the track all at once.
            rect.anchorMin = new Vector2(SprintUiLayout.FinishAnchorMinX(distance), 0f);
            rect.anchorMax = new Vector2(SprintUiLayout.FinishAnchorMaxX(distance), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void Update() => RefreshForTest();
    }
}
