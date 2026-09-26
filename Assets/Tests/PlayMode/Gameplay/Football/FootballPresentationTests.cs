using KMA.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Tests.Gameplay.Football
{
    public sealed class FootballPresentationTests
    {
        GameObject root;
        FootballRules rules;
        [SetUp] public void SetUp(){root=new GameObject("FootballPresentationTests");rules=new FootballRules(FootballTuning.For(FootballDifficulty.Normal));}
        [TearDown] public void TearDown()=>Object.DestroyImmediate(root);
        T Make<T>(string name) where T:Component
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root.transform);var existing=go.GetComponent<T>();return existing ? existing : go.AddComponent<T>();
        }
        [Test]
        public void PreviewAppearsOnlyWhileChargingAndHidesOnReleaseCancelAndDisable()
        {
            var p=root.AddComponent<FootballPresentation>();
            var ball=Make<SpriteRenderer>("Ball");var shadow=Make<SpriteRenderer>("Shadow");var crosshair=Make<SpriteRenderer>("Crosshair");
            var dot=Make<SpriteRenderer>("Dot");var left=Make<RectTransform>("Left");var right=Make<RectTransform>("Right");
            left.position=Vector3.left;right.position=Vector3.right;
            p.Configure(Make<SpriteRenderer>("Field"),Make<SpriteRenderer>("Goal"),ball,shadow,
                Make<SpriteRenderer>("Player"),Make<SpriteRenderer>("Keeper"),crosshair,left,right,new[]{dot});
            rules.Start();rules.SetAim(.7f);p.Render(rules);
            Assert.That(crosshair.enabled,Is.False);Assert.That(dot.enabled,Is.False);
            rules.BeginCharge();rules.Tick(1f);p.Render(rules);
            Assert.That(crosshair.enabled,Is.True);Assert.That(dot.enabled,Is.True);
            rules.CancelCharge();p.Render(rules);Assert.That(crosshair.enabled,Is.False);Assert.That(dot.enabled,Is.False);
            rules.BeginCharge();rules.Tick(.9f);p.Render(rules);rules.ReleaseShot();p.Render(rules);
            Assert.That(crosshair.enabled,Is.False);Assert.That(dot.enabled,Is.False);
            rules.Tick(.6f);p.Render(rules);
            Assert.That(Vector3.Distance(ball.transform.position,FootballPresentation.BallToWorld(rules.Flight.Position)),Is.LessThan(.0001f));
            Assert.That(ball.transform.localScale.x,Is.LessThan(1f));
            Assert.That(shadow.transform.position.y,Is.LessThan(ball.transform.position.y));
            rules.Tick(20f);rules.BeginCharge();p.Render(rules);p.enabled=false;
            Assert.That(crosshair.enabled,Is.False);Assert.That(dot.enabled,Is.False);
        }
        [Test]
        public void AimArrowTracksDirectionBeforeChargingAndHidesAfterRelease()
        {
            var p = root.AddComponent<FootballPresentation>();
            var ball = Make<SpriteRenderer>("Ball");
            var left = Make<RectTransform>("Left");
            var right = Make<RectTransform>("Right");
            left.position = Vector3.left; right.position = Vector3.right;
            p.Configure(Make<SpriteRenderer>("Field"), Make<SpriteRenderer>("Goal"), ball,
                Make<SpriteRenderer>("Shadow"), Make<SpriteRenderer>("Player"), Make<SpriteRenderer>("Keeper"),
                Make<SpriteRenderer>("Crosshair"), left, right);
            rules.Start();
            foreach (float aim in new[] { -1f, 0f, 1f })
            {
                rules.SetAim(aim); p.Render(rules);
                var arrow = p.GetComponentInChildren<LineRenderer>();
                Assert.That(arrow, Is.Not.Null, "An aiming arrow must exist before holding SHOOT");
                Assert.That(arrow.enabled, Is.True);
                Vector3 direction = arrow.GetPosition(1) - arrow.GetPosition(0);
                Assert.That(direction.y, Is.GreaterThan(0f));
                Assert.That(direction.x, aim < 0 ? Is.LessThan(0f) : aim > 0 ? Is.GreaterThan(0f) : Is.EqualTo(0f).Within(.001f));
                Assert.That(Vector3.Distance(arrow.GetPosition(0), ball.transform.position), Is.LessThan(.6f));
            }
            rules.BeginCharge(); rules.Tick(.8f); p.Render(rules);
            var visibleArrow = p.GetComponentInChildren<LineRenderer>();
            Assert.That(visibleArrow.enabled, Is.True);
            rules.ReleaseShot(); p.Render(rules);
            Assert.That(visibleArrow.enabled, Is.False);
            rules.Tick(20f); p.Render(rules);
            Assert.That(visibleArrow.enabled, Is.True);
            p.enabled = false;
            Assert.That(visibleArrow.enabled, Is.False);
        }

        [Test]
        public void HudShowsDirectionPowerAndOutcomeWithoutOverridingInputOwnership()
        {
            var slider=Make<Slider>("Direction");slider.minValue=-1f;slider.maxValue=1f;
            var shoot=Make<FootballHoldButton>("Shoot");var power=Make<Image>("Power");power.type=Image.Type.Filled;
            var percent=Make<TextMeshProUGUI>("Percent");var score=Make<TextMeshProUGUI>("Score");var remaining=Make<TextMeshProUGUI>("Remaining");
            var warning=Make<RectTransform>("Warning").gameObject;var start=Make<RectTransform>("Start").gameObject;
            var markers=new TMP_Text[5];for(int i=0;i<5;i++)markers[i]=Make<TextMeshProUGUI>("Marker"+i);
            var hud=root.AddComponent<FootballHud>();hud.Configure(slider,shoot,power,percent,warning,score,remaining,markers,start);
            rules.Start();rules.SetAim(-.4f);rules.BeginCharge();rules.Tick(FootballTuning.For(FootballDifficulty.Normal).PowerRiseSeconds);
            slider.interactable=false;shoot.SetInteractable(false);hud.Render(rules);
            Assert.That(slider.value,Is.EqualTo(-.4f));Assert.That(percent.text,Is.EqualTo("100%"));Assert.That(warning.activeSelf,Is.True);
            Assert.That(slider.interactable,Is.False);Assert.That(shoot.IsInteractable,Is.False);
            rules.ReleaseShot();rules.Tick(20f);hud.Render(rules);
            Assert.That(score.text,Is.EqualTo("BÀN: 0"));Assert.That(remaining.text,Is.EqualTo("CÒN 4 LƯỢT"));Assert.That(markers[0].text,Is.EqualTo("×"));
        }
    }
}
