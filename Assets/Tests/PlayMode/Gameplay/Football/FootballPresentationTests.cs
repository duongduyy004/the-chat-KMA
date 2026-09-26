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

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FootballPresentationTests");
            rules = new FootballRules(FootballTuning.For(FootballDifficulty.Normal), () => 0f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [TestCase(FootballDifficulty.Easy, 2.4f, 1.4f, .40f, .75f)]
        [TestCase(FootballDifficulty.Normal, 1.7f, 1.4f, .27f, 1.20f)]
        [TestCase(FootballDifficulty.Hard, 1.1f, .9f, .15f, 1.70f)]
        public void DifficultyConfigMatchesSimulationTuning(FootballDifficulty difficulty,
            float aim, float power, float reaction, float speed)
        {
            var config = ScriptableObject.CreateInstance<FootballDifficultyConfig>();
            var tuning = config.Get(difficulty);
            Assert.That(tuning.AimTraverseSeconds, Is.EqualTo(aim));
            Assert.That(tuning.PowerRiseSeconds, Is.EqualTo(power));
            Assert.That(tuning.KeeperReactionSeconds, Is.EqualTo(reaction));
            Assert.That(tuning.KeeperSpeed, Is.EqualTo(speed));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RenderGatesAimAndShootAndShowsHighPowerWarning()
        {
            var refs = CreateHud();
            var hud = root.AddComponent<FootballHud>();
            hud.Configure(refs.aim, refs.shoot, refs.power, refs.percent, refs.warning, refs.score, refs.remaining, refs.markers, refs.startPanel);
            hud.Render(rules);
            Assert.That(refs.shoot.IsInteractable, Is.False);

            rules.Start();
            rules.LockAim();
            hud.Render(rules);
            Assert.That(refs.aim.interactable, Is.False);
            Assert.That(refs.shoot.IsInteractable, Is.True);

            rules.BeginCharge();
            rules.Tick(1.26f);
            hud.Render(rules);
            Assert.That(refs.percent.text, Is.EqualTo("90%"));
            Assert.That(refs.warning.activeSelf, Is.True);
        }

        [Test]
        public void GoalPlanePositionsBallAndKeeperUsingSerializedPostWorldPoints()
        {
            var presentation = root.AddComponent<FootballPresentation>();
            var refs = CreatePresentation();
            presentation.Configure(refs.field, refs.goal, refs.ball, refs.shadow, refs.player, refs.keeper,
                refs.crosshair, refs.leftPost, refs.rightPost);

            rules.Start();
            rules.Tick(.4f);
            rules.LockAim();
            rules.BeginCharge();
            rules.Tick(.7f);
            rules.ReleaseShot();
            rules.Tick(1.08f);
            presentation.Render(rules);

            Assert.That(rules.State, Is.EqualTo(FootballState.ShotResult));
            Assert.That(refs.ball.transform.position.x,
                Is.EqualTo(presentation.GoalXToWorld(rules.LastShot.Value.TargetX).x).Within(.001f));
            Assert.That(refs.keeper.transform.position.x,
                Is.EqualTo(presentation.GoalXToWorld(rules.KeeperX).x).Within(.001f));
        }

        [Test]
        public void HudScoreAndRemainingKicksComeFromRules()
        {
            var refs = CreateHud();
            var hud = root.AddComponent<FootballHud>();
            hud.Configure(refs.aim, refs.shoot, refs.power, refs.percent, refs.warning, refs.score, refs.remaining, refs.markers, refs.startPanel);
            rules.Start();
            rules.LockAim();
            rules.BeginCharge();
            rules.Tick(.7f);
            rules.ReleaseShot();
            rules.Tick(1.18f);
            hud.Render(rules);

            Assert.That(refs.score.text, Is.EqualTo("BÀN: 0"));
            Assert.That(refs.remaining.text, Is.EqualTo("CÒN 4 LƯỢT"));
            Assert.That(refs.markers[0].text, Is.EqualTo(rules.Outcomes[0].ToString().ToUpperInvariant()));
        }

        HudRefs CreateHud()
        {
            var aim = new GameObject("AIM", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            var shoot = new GameObject("SHOOT", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(FootballHoldButton)).GetComponent<FootballHoldButton>();
            var power = new GameObject("Power", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            power.type = Image.Type.Filled;
            var percent = new GameObject("Percent").AddComponent<TextMeshPro>();
            var score = new GameObject("Score").AddComponent<TextMeshPro>();
            var remaining = new GameObject("Remaining").AddComponent<TextMeshPro>();
            var warning = new GameObject("Warning");
            var start = new GameObject("Start");
            var markers = new TMP_Text[5];
            for (int i = 0; i < markers.Length; i++) markers[i] = new GameObject("Marker").AddComponent<TextMeshPro>();
            return new HudRefs(aim, shoot, power, percent, warning, score, remaining, markers, start);
        }

        PresentationRefs CreatePresentation()
        {
            SpriteRenderer Make(string name) => new GameObject(name).AddComponent<SpriteRenderer>();
            var left = new GameObject("LeftPost").transform;
            var right = new GameObject("RightPost").transform;
            left.position = new Vector3(-4, 0, 0);
            right.position = new Vector3(4, 0, 0);
            return new PresentationRefs(Make("Field"), Make("Goal"), Make("Ball"), Make("Shadow"),
                Make("Player"), Make("Keeper"), Make("Crosshair"), left, right);
        }

        readonly struct HudRefs
        {
            public HudRefs(Button aim, FootballHoldButton shoot, Image power, TMP_Text percent, GameObject warning,
                TMP_Text score, TMP_Text remaining, TMP_Text[] markers, GameObject startPanel)
            { this.aim=aim; this.shoot=shoot; this.power=power; this.percent=percent; this.warning=warning;
              this.score=score; this.remaining=remaining; this.markers=markers; this.startPanel=startPanel; }
            public readonly Button aim;
            public readonly FootballHoldButton shoot;
            public readonly Image power;
            public readonly TMP_Text percent, score, remaining;
            public readonly GameObject warning, startPanel;
            public readonly TMP_Text[] markers;
        }

        readonly struct PresentationRefs
        {
            public PresentationRefs(SpriteRenderer field, SpriteRenderer goal, SpriteRenderer ball, SpriteRenderer shadow,
                SpriteRenderer player, SpriteRenderer keeper, SpriteRenderer crosshair, Transform leftPost, Transform rightPost)
            { this.field=field; this.goal=goal; this.ball=ball; this.shadow=shadow; this.player=player;
              this.keeper=keeper; this.crosshair=crosshair; this.leftPost=leftPost; this.rightPost=rightPost; }
            public readonly SpriteRenderer field, goal, ball, shadow, player, keeper, crosshair;
            public readonly Transform leftPost, rightPost;
        }
    }
}
