using System;
using KMA.Gameplay.UI;
using NUnit.Framework;

namespace KMA.Tests.Presentation
{
    public sealed class DialogueTypewriterTests
    {
        [Test]
        public void RevealsCharactersOverTimeWithoutPassingTheTotal()
        {
            var writer = new DialogueTypewriter(10f);
            writer.Begin(25);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.That(writer.IsDone, Is.False);
            writer.Tick(.55f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(5));
            writer.Tick(1f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(15));
            writer.Tick(10f);
            Assert.That(writer.VisibleCharacters, Is.EqualTo(25));
            Assert.That(writer.IsDone, Is.True);
        }

        [Test]
        public void CompleteShowsEverythingAndBeginRestarts()
        {
            var writer = new DialogueTypewriter();
            writer.Begin(80);
            writer.Complete();
            Assert.That(writer.VisibleCharacters, Is.EqualTo(80));
            Assert.That(writer.IsDone, Is.True);
            writer.Begin(12);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.That(writer.IsDone, Is.False);
        }

        [Test]
        public void EmptyLineIsDoneImmediatelyAndBadInputIsIgnoredOrRejected()
        {
            var writer = new DialogueTypewriter();
            writer.Begin(0);
            Assert.That(writer.IsDone, Is.True);
            writer.Begin(10);
            writer.Tick(-1f);
            Assert.That(writer.VisibleCharacters, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => new DialogueTypewriter(0f));
        }
    }
}
