using KMA.Gameplay;
using KMA.Gameplay.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Presentation
{
    public sealed class CampusBackdropUiTests
    {
        GameObject canvasObject;
        RectTransform root;
        CampusBackdropArt art;
        Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(1920f, 1080f);
            root = new GameObject("Backdrop", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasObject.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            texture = new Texture2D(200, 50);
            Sprite wide = Sprite.Create(texture, new Rect(0, 0, 200, 50), new Vector2(.5f, .5f), 100f);
            art = ScriptableObject.CreateInstance<CampusBackdropArt>();
            art.Configure(wide, wide, wide, Color.cyan, Color.green);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(art);
            Object.DestroyImmediate(texture);
        }

        CampusBackdropUi Build()
        {
            var backdrop = root.gameObject.AddComponent<CampusBackdropUi>();
            backdrop.Configure(art, .3f, .25f, true);
            return backdrop;
        }

        [Test]
        public void SkyCoversTheRectAndSkylineSpansIt()
        {
            CampusBackdropUi backdrop = Build();
            Rect sky = Local(backdrop.Sky.rectTransform);
            Assert.That(sky.xMin, Is.LessThanOrEqualTo(-960f + .5f));
            Assert.That(sky.xMax, Is.GreaterThanOrEqualTo(960f - .5f));
            Assert.That(sky.yMin, Is.LessThanOrEqualTo(-540f + .5f));
            Assert.That(sky.yMax, Is.GreaterThanOrEqualTo(540f - .5f));
            Assert.That(backdrop.SkylineTileCount, Is.GreaterThan(0));
            Rect first = Local((RectTransform)root.Find("Skyline0"));
            Assert.That(first.yMin, Is.EqualTo(-540f + 1080f * .3f).Within(.5f));
            Assert.That(first.height, Is.EqualTo(1080f * .25f).Within(.5f));
        }

        [Test]
        public void NothingInTheBackdropBlocksTouches()
        {
            Build();
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                Assert.That(image.raycastTarget, Is.False, image.name);
        }

        [Test]
        public void RefitsWhenTheRectChanges()
        {
            CampusBackdropUi backdrop = Build();
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(2400f, 1080f);
            backdrop.Refit();
            Assert.That(Local(backdrop.Sky.rectTransform).xMax, Is.GreaterThanOrEqualTo(1200f - .5f));
            Assert.That(Local(backdrop.Ground.rectTransform).width, Is.EqualTo(2400f).Within(.5f));
        }

        [Test]
        public void EditModeReEnableAndResizeDoNotRefit()
        {
            CampusBackdropUi backdrop = Build();
            Vector2 skySize = backdrop.Sky.rectTransform.sizeDelta;
            // A taller rect changes the cover size of the 4:1 test sky (a wider one would not).
            ((RectTransform)canvasObject.transform).sizeDelta = new Vector2(1920f, 1440f);
            root.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            Assert.That(backdrop.Sky.rectTransform.sizeDelta, Is.EqualTo(skySize),
                "outside Play Mode only Configure/Refit may touch the scene");
        }

        static Rect Local(RectTransform rect)
        {
            Vector2 size = rect.rect.size;
            Vector2 centre = rect.anchoredPosition;
            return new Rect(centre.x - size.x * .5f, centre.y - size.y * .5f, size.x, size.y);
        }
    }
}
