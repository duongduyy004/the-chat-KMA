using UnityEngine;

namespace KMA.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class FootballCameraFit : MonoBehaviour
    {
        [SerializeField] SpriteRenderer field;
        Camera view;
        public void Configure(SpriteRenderer fieldView) => field = fieldView;
        void Awake() => view = GetComponent<Camera>();
        void LateUpdate()
        {
            if (!view) return;
            view.orthographicSize = Mathf.Max(5.4f, 9.6f / Mathf.Max(.1f, view.aspect));
            if (!field || !field.sprite) return;
            // Fill the viewport with the backdrop only; actors and goal retain their scale.
            Vector3 size = field.sprite.bounds.size;
            float height = view.orthographicSize * 2f;
            field.transform.localScale = new Vector3(height * view.aspect / size.x, height / size.y, 1f);
        }
    }
}
