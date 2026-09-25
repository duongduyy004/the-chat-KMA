using KMA.Gameplay;
using KMA.Gameplay.Volleyball;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    public sealed class PresentationTests
    {
        GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("PresentationTests");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        static Sprite[] Frames(string name, int count)
        {
            var texture = new Texture2D(4, 4);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, 0f));
                frames[i].name = $"{name}_{i}";
            }

            return frames;
        }

        SpriteRenderer Renderer(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child.AddComponent<SpriteRenderer>();
        }

        [Test]
        public void FlipbookLoopsAndClamps()
        {
            SpriteRenderer renderer = Renderer("Book");
            var book = renderer.gameObject.AddComponent<SpriteFlipbook>();
            Sprite[] frames = Frames("f", 3);
            book.Configure(renderer, frames, true, 10f);

            Assert.That(renderer.sprite, Is.SameAs(frames[0]));
            book.Advance(.15f);
            Assert.That(book.FrameIndex, Is.EqualTo(1));
            book.Advance(.2f);
            Assert.That(book.FrameIndex, Is.EqualTo(0));

            book.Play(frames, false);
            book.Advance(5f);
            Assert.That(book.FrameIndex, Is.EqualTo(2));
            Assert.That(renderer.sprite, Is.SameAs(frames[2]));
        }

        [Test]
        public void AthleteViewFollowsTheModel()
        {
            SpriteRenderer body = Renderer("Athlete");
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            Sprite[] idle = Frames("idle", 2), run = Frames("run", 2), receive = Frames("rec", 2),
                smash = Frames("smash", 2), block = Frames("block", 2), dive = Frames("dive", 2);
            view.Configure(body, book, true, idle, run, receive, smash, block, dive);

            var athlete = new VolleyAthlete(CourtSide.Opponent, 5f);
            athlete.PlaceAt(new Vector2(5f, 2f));
            view.Render(athlete);
            Assert.That(view.transform.position, Is.EqualTo(CourtSpace.ToWorld(new Vector2(5f, 2f), 0f)));
            Assert.That(body.sortingOrder, Is.EqualTo(80));
            Assert.That(body.flipX, Is.True);
            Assert.That(book.Frames, Is.SameAs(idle));

            athlete.BeginAction(AthleteAction.Smash, .45f);
            view.Render(athlete);
            Assert.That(book.Frames, Is.SameAs(smash));
            Assert.That(book.Loop, Is.False);
            Assert.That(view.FramesFor(AthleteAction.Serve), Is.SameAs(smash));
            Assert.That(view.FramesFor(AthleteAction.Dive), Is.SameAs(dive));
            Assert.That(view.FramesFor(AthleteAction.Run), Is.SameAs(run));
        }

        [Test]
        public void NearerAthletesDrawInFront()
        {
            Assert.That(VolleyAthleteView.SortingOrderFor(-3f), Is.GreaterThan(VolleyAthleteView.SortingOrderFor(3f)));
            Assert.That(VolleyAthleteView.SortingOrderFor(-5.5f), Is.LessThan(VolleyBallView.BallSortingOrder));
        }

        [Test]
        public void BallViewDrawsBallAboveItsShadow()
        {
            SpriteRenderer ball = Renderer("Ball"), shadow = Renderer("Shadow"),
                contact = Renderer("Contact"), aim = Renderer("Aim");
            var view = root.AddComponent<VolleyBallView>();
            view.Configure(ball, shadow, contact, aim);
            var match = new VolleyballMatch();

            view.Render(match);

            Assert.That(ball.transform.position,
                Is.EqualTo(CourtSpace.ToWorld(VolleyballMatch.PlayerServeSpot, VolleyballMatch.TossStartHeight)));
            Assert.That(shadow.transform.position, Is.EqualTo(CourtSpace.ToWorld(VolleyballMatch.PlayerServeSpot, 0f)));
            Assert.That(shadow.transform.localScale.x,
                Is.EqualTo(VolleyBallView.ShadowScaleFor(VolleyballMatch.TossStartHeight)).Within(1e-4f));
            Assert.That(ball.sortingOrder, Is.EqualTo(VolleyBallView.BallSortingOrder));
            Assert.That(contact.enabled, Is.False);
            Assert.That(aim.enabled, Is.False);

            match.PressAction();
            view.Render(match);
            Assert.That(contact.enabled, Is.True);
        }

        [Test]
        public void ShadowAndMarkerScalesShrinkAsDesigned()
        {
            Assert.That(VolleyBallView.ShadowScaleFor(0f), Is.EqualTo(1f));
            Assert.That(VolleyBallView.ShadowScaleFor(10f), Is.EqualTo(.6f));
            Assert.That(VolleyBallView.MarkerScaleFor(0f), Is.EqualTo(1f));
            Assert.That(VolleyBallView.MarkerScaleFor(.6f), Is.EqualTo(2.5f));
            Assert.That(VolleyBallView.MarkerScaleFor(-.2f), Is.EqualTo(1f));
        }

        [Test]
        public void HudTextsMatchTheSpec()
        {
            Assert.That(VolleyballHud.ScoreText(3, 2), Is.EqualTo("BẠN 3 – 2 MÁY"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Perfect, 0f), Is.EqualTo("PERFECT"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Good, .1f), Is.EqualTo("GOOD"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Late, -.2f), Is.EqualTo("EARLY"));
            Assert.That(VolleyballHud.FeedbackText(TimingGrade.Late, .2f), Is.EqualTo("LATE"));
        }

        [Test]
        public void HudShowsFeedbackOnlyForTimedPressesAndHidesItAgain()
        {
            var hud = root.AddComponent<VolleyballHud>();
            TMP_Text score = new GameObject("Score").AddComponent<TextMeshPro>();
            TMP_Text feedback = new GameObject("Feedback").AddComponent<TextMeshPro>();
            TMP_Text hint = new GameObject("Hint").AddComponent<TextMeshPro>();
            score.transform.SetParent(root.transform);
            feedback.transform.SetParent(root.transform);
            hint.transform.SetParent(root.transform);
            hud.Configure(score, feedback, hint);
            var match = new VolleyballMatch();

            hud.ShowFeedback(new ActionDecision(ActionKind.Block, TimingGrade.Miss, 0f));
            hud.Render(match, MinigamePhase.Play, 0f);
            Assert.That(feedback.enabled, Is.False);

            hud.ShowFeedback(new ActionDecision(ActionKind.Receive, TimingGrade.Perfect, 0f));
            hud.Render(match, MinigamePhase.Play, .1f);
            Assert.That(feedback.enabled, Is.True);
            Assert.That(feedback.text, Is.EqualTo("PERFECT"));
            Assert.That(score.text, Is.EqualTo("BẠN 0 – 0 MÁY"));
            Assert.That(hint.enabled, Is.False);

            hud.Render(match, MinigamePhase.Play, VolleyballHud.FeedbackSeconds);
            Assert.That(feedback.enabled, Is.False);

            hud.Render(match, MinigamePhase.Tutorial, 0f);
            Assert.That(hint.enabled, Is.True);
            Assert.That(hint.text, Is.EqualTo(VolleyballHud.HintText));
        }
    }
}
