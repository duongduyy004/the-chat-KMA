using UnityEngine;

namespace KMA.Gameplay
{
    /// Draws the campus sky, skyline and optional ground behind a 2D scene and keeps them fitted to
    /// an orthographic camera. Owns its child renderers; Refit reuses them.
    [ExecuteAlways]
    public sealed class CampusBackdropWorld : MonoBehaviour
    {
        public const int SkyOrder = -40, SkylineOrder = -35, GroundOrder = -34;

        [SerializeField] CampusBackdropArt art;
        [SerializeField] Camera targetCamera;
        [SerializeField] float horizonY;
        [SerializeField, Min(0f)] float skylineHeight = 2f;
        [SerializeField] bool drawGround = true;
        [SerializeField] bool overrideGround;
        [SerializeField] Color groundOverride = Color.white;

        SpriteRenderer sky, ground;
        float fittedAspect, fittedSize;
        Vector3 fittedPosition;

        public SpriteRenderer Sky => sky;
        public SpriteRenderer Ground => ground;
        public int SkylineTileCount { get; private set; }

        public void Configure(CampusBackdropArt backdropArt, Camera camera, float horizon, float height, bool withGround,
            Color? groundColor = null)
        {
            art = backdropArt;
            targetCamera = camera;
            horizonY = horizon;
            skylineHeight = height;
            drawGround = withGround;
            overrideGround = groundColor.HasValue;
            groundOverride = groundColor ?? Color.white;
            Refit();
        }

        void OnEnable() => Refit();

        void LateUpdate()
        {
            if (targetCamera != null && (targetCamera.aspect != fittedAspect ||
                targetCamera.orthographicSize != fittedSize || targetCamera.transform.position != fittedPosition))
                Refit();
        }

        public void Refit()
        {
            if (art == null || targetCamera == null)
                return;
            fittedAspect = targetCamera.aspect;
            fittedSize = targetCamera.orthographicSize;
            fittedPosition = targetCamera.transform.position;
            float height = fittedSize * 2f, width = height * fittedAspect;
            var view = new Rect(fittedPosition.x - width * .5f, fittedPosition.y - height * .5f, width, height);

            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = art.SkyColor;

            sky = Child("Sky", art.Sky, SkyOrder);
            Place(sky, CampusBackdropLayout.Cover(view, CampusBackdropArt.Aspect(art.Sky)));

            Rect[] tiles = CampusBackdropLayout.Band(view, horizonY, skylineHeight, CampusBackdropArt.Aspect(art.Skyline));
            for (int i = 0; i < tiles.Length; i++)
                Place(Child("Skyline" + i, art.Skyline, SkylineOrder), tiles[i]);
            for (int i = tiles.Length; transform.Find("Skyline" + i) != null; i++)
                transform.Find("Skyline" + i).gameObject.SetActive(false);
            SkylineTileCount = tiles.Length;

            ground = Child("Ground", art.Pixel, GroundOrder);
            ground.color = overrideGround ? groundOverride : art.GroundColor;
            ground.enabled = drawGround;
            Place(ground, new Rect(view.xMin, view.yMin, view.width, Mathf.Max(0f, horizonY - view.yMin)));
        }

        SpriteRenderer Child(string childName, Sprite sprite, int order)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }
            child.gameObject.SetActive(true);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sortingOrder = order;
            return renderer;
        }

        static void Place(SpriteRenderer renderer, Rect rect)
        {
            Vector2 size = renderer.sprite.bounds.size;
            Transform t = renderer.transform;
            t.position = new Vector3(rect.center.x, rect.center.y, t.position.z);
            Vector3 parentScale = t.parent == null ? Vector3.one : t.parent.lossyScale;
            t.localScale = new Vector3(rect.width / size.x / parentScale.x, rect.height / size.y / parentScale.y, 1f);
        }
    }
}
