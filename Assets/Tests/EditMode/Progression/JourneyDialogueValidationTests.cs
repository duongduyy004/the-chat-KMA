using System.Collections.Generic;
using KMA.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.Gameplay.Progression
{
    public sealed class JourneyDialogueValidationTests
    {
        readonly List<Object> created = new List<Object>();

        Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
            created.Add(texture);
            created.Add(sprite);
            return sprite;
        }

        JourneyCharacter Character(string id, bool player, params DialoguePose[] poses)
        {
            var list = new List<JourneyPoseSprite>();
            foreach (DialoguePose pose in poses) list.Add(new JourneyPoseSprite(pose, MakeSprite()));
            return new JourneyCharacter(id, id.ToUpperInvariant(), Color.white, player, list);
        }

        JourneyDialogueLibrary Library(JourneyCharacter[] cast, params JourneyDialogueLine[] lines)
        {
            var library = ScriptableObject.CreateInstance<JourneyDialogueLibrary>();
            created.Add(library);
            library.SetContent(cast, new[] { new JourneyDialogueNode("n", new List<JourneyDialogueLine>(lines)) });
            return library;
        }

        JourneyCharacter[] DefaultCast() => new[]
        {
            Character("me", true, DialoguePose.Idle),
            Character("friend", false, DialoguePose.Idle, DialoguePose.Cheer)
        };

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created) if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void ValidLibraryPassesAndResolvesCharacters()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Cheer, "Chào :sob:", "WOW"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.True, error);
            Assert.That(library.Player.Id, Is.EqualTo("me"));
            Assert.That(library.GetCharacter("friend").DisplayName, Is.EqualTo("FRIEND"));
            Assert.Throws<KeyNotFoundException>(() => library.GetCharacter("ghost"));
        }

        [Test]
        public void UnknownCharacterFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("ghost", DialoguePose.Idle, "Boo"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Á"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("ghost"));
        }

        [Test]
        public void PoseWithoutSpriteFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Duck, "Né"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("Duck"));
        }

        [Test]
        public void UnknownEmojiFailsValidation()
        {
            JourneyDialogueLibrary library = Library(DefaultCast(),
                new JourneyDialogueLine("friend", DialoguePose.Idle, "Nóng :hot:"),
                new JourneyDialogueLine("me", DialoguePose.Idle, "Ừ"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain(":hot:"));
        }

        [Test]
        public void CastMustHaveExactlyOnePlayer()
        {
            JourneyDialogueLibrary library = Library(new[]
                {
                    Character("a", false, DialoguePose.Idle),
                    Character("b", false, DialoguePose.Idle)
                },
                new JourneyDialogueLine("a", DialoguePose.Idle, "x"),
                new JourneyDialogueLine("b", DialoguePose.Idle, "y"));
            Assert.That(library.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("player"));
        }

        [Test]
        public void MissingPoseFallsBackToIdle()
        {
            JourneyCharacter friend = Character("friend", false, DialoguePose.Idle);
            Assert.That(friend.GetPose(DialoguePose.Jump), Is.SameAs(friend.GetPose(DialoguePose.Idle)));
            Assert.That(friend.HasPose(DialoguePose.Jump), Is.False);
            var empty = new JourneyCharacter("x", "X", Color.white, false, new JourneyPoseSprite[0]);
            Assert.That(empty.GetPose(DialoguePose.Idle), Is.Null);
        }
    }
}
