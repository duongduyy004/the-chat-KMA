using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    /// Canvas version of the campus backdrop. Lives outside SafeAreaRoot so it covers the whole
    /// screen; children are centre-anchored and laid out in this rect's local pixels.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CampusBackdropUi : MonoBehaviour
    {
        [SerializeField] CampusBackdropArt art;
        [SerializeField, Range(0f, 1f)] float horizon01 = .3f;
        [SerializeField, Range(0f, 1f)] float skylineHeight01 = .25f;
        [SerializeField] bool drawGround = true;

        Image sky, ground;

        public Image Sky => sky;
        public Image Ground => ground;
        public int SkylineTileCount { get; private set; }

        public void Configure(CampusBackdropArt backdropArt, float horizon, float skylineHeight, bool withGround)
        {
            art = backdropArt;
            horizon01 = horizon;
            skylineHeight01 = skylineHeight;
            drawGround = withGround;
            Refit();
        }

        void OnEnable() => Refit();
        void OnRectTransformDimensionsChange() => Refit();

        public void Refit()
        {
            if (art == null)
                return;
            Rect view = ((RectTransform)transform).rect;
            view = new Rect(-view.width * .5f, -view.height * .5f, view.width, view.height);

            sky = Child("Sky", art.Sky, 0);
            Place(sky, CampusBackdropLayout.Cover(view, CampusBackdropArt.Aspect(art.Sky)));

            float horizonY = view.yMin + view.height * horizon01;
            ground = Child("Ground", art.Pixel, 1);
            ground.color = art.GroundColor;
            ground.enabled = drawGround;
            Place(ground, new Rect(view.xMin, view.yMin, view.width, horizonY - view.yMin));

            Rect[] tiles = CampusBackdropLayout.Band(view, horizonY, view.height * skylineHeight01,
                CampusBackdropArt.Aspect(art.Skyline));
            for (int i = 0; i < tiles.Length; i++)
                Place(Child("Skyline" + i, art.Skyline, 2 + i), tiles[i]);
            for (int i = tiles.Length; transform.Find("Skyline" + i) != null; i++)
                transform.Find("Skyline" + i).gameObject.SetActive(false);
            SkylineTileCount = tiles.Length;
        }

        Image Child(string childName, Sprite sprite, int sibling)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName, typeof(RectTransform)).transform;
                child.SetParent(transform, false);
            }
            child.gameObject.SetActive(true);
            child.SetSiblingIndex(Mathf.Min(sibling, transform.childCount - 1));
            Image image = child.GetComponent<Image>();
            if (image == null)
                image = child.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        static void Place(Image image, Rect rect)
        {
            RectTransform t = image.rectTransform;
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(.5f, .5f);
            t.anchoredPosition = rect.center;
            t.sizeDelta = rect.size;
        }
    }
}
