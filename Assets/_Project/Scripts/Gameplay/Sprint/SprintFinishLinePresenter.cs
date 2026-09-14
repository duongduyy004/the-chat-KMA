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
            IsVisible = controller != null && SprintUiLayout.FinishVisible(controller.Snapshot.Distance);
            if (finishRoot != null)
                finishRoot.SetActive(IsVisible);
        }

        void Update() => RefreshForTest();
    }
}
