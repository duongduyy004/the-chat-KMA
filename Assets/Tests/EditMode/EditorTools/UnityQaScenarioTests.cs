using KMA.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace KMA.Tests.EditorTools
{
    public sealed class UnityQaScenarioTests
    {
        [Test]
        public void LegacyRequestWithoutActions_IsValid()
        {
            var request = JsonUtility.FromJson<UnityQaRequest>(
                "{\"id\":\"legacy\",\"output\":\"Builds/Screenshots/a.png\",\"waitSeconds\":3}");
            Assert.That(UnityQaScenarioValidator.Validate(request), Is.Null);
        }

        [Test]
        public void SupportedActions_WithValidFields_AreValid()
        {
            const string Json = "{\"id\":\"x\",\"actions\":["
                + "{\"type\":\"wait\",\"seconds\":0},"
                + "{\"type\":\"tap\",\"target\":\"Play\"},"
                + "{\"type\":\"tap\",\"position\":[0,1]},"
                + "{\"type\":\"hold\",\"target\":\"Fire\",\"duration\":0.5},"
                + "{\"type\":\"swipe\",\"from\":[0,0],\"to\":[1,1],\"duration\":0.2},"
                + "{\"type\":\"key\",\"key\":\"space\",\"duration\":0.1},"
                + "{\"type\":\"capture\",\"output\":\"Builds/Screenshots/checkpoint.png\"}]}";

            Assert.That(UnityQaScenarioValidator.Validate(Parse(Json)), Is.Null);
        }

        [TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"tap\",\"target\":\"Play\",\"position\":[0.5,0.5]}]}",
            "Action 0 must specify exactly one locator")]
        [TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"swipe\",\"from\":[-0.1,0.5],\"to\":[0.5,0.5],\"duration\":0.2}]}",
            "Action 0 from must contain coordinates within 0..1")]
        [TestCase("{\"id\":\"x\",\"actions\":[{\"type\":\"dance\"}]}",
            "Action 0 has unsupported type: dance")]
        public void InvalidAction_ReturnsIndexedDiagnostic(string json, string expected)
        {
            Assert.That(UnityQaScenarioValidator.Validate(Parse(json)), Is.EqualTo(expected));
        }

        [Test]
        public void NullRequest_ReturnsRequestDiagnostic()
        {
            Assert.That(UnityQaScenarioValidator.Validate(null),
                Is.EqualTo("Request must not be null"));
        }

        [TestCase(-0.01f)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidRequestWaitSeconds_ReturnsFieldDiagnostic(float seconds)
        {
            Assert.That(UnityQaScenarioValidator.Validate(new UnityQaRequest { waitSeconds = seconds }),
                Is.EqualTo("Request waitSeconds must be finite and non-negative"));
        }

        [Test]
        public void NullAction_ReturnsIndexedDiagnostic()
        {
            var request = new UnityQaRequest { actions = new UnityQaAction[] { null } };

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 must not be null"));
        }

        [TestCase(-0.01f)]
        [TestCase(float.NaN)]
        public void InvalidWaitSeconds_ReturnsFieldDiagnostic(float seconds)
        {
            var request = RequestWith(new UnityQaAction { type = "wait", seconds = seconds });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 seconds must be finite and non-negative"));
        }

        [TestCase("tap")]
        [TestCase("hold")]
        public void PointerActionWithoutLocator_ReturnsLocatorDiagnostic(string type)
        {
            var request = RequestWith(new UnityQaAction { type = type });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 must specify exactly one locator"));
        }

        [TestCase("tap")]
        [TestCase("hold")]
        public void PointerActionWithMalformedPosition_ReturnsShapeDiagnostic(string type)
        {
            var request = RequestWith(new UnityQaAction { type = type, position = new[] { 0.5f } });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 position must contain exactly two coordinates"));
        }

        [TestCase(-0.01f)]
        [TestCase(float.NaN)]
        public void PointerActionWithInvalidPosition_ReturnsRangeDiagnostic(float coordinate)
        {
            var request = RequestWith(new UnityQaAction
            {
                type = "tap",
                position = new[] { coordinate, 0.5f },
            });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 position must contain coordinates within 0..1"));
        }

        [TestCase("hold", -0.01f)]
        [TestCase("hold", float.PositiveInfinity)]
        [TestCase("key", -0.01f)]
        [TestCase("key", float.NaN)]
        public void InvalidActionDuration_ReturnsFieldDiagnostic(string type, float duration)
        {
            var action = type == "hold"
                ? new UnityQaAction { type = type, target = "Fire", duration = duration }
                : new UnityQaAction { type = type, key = "space", duration = duration };

            Assert.That(UnityQaScenarioValidator.Validate(RequestWith(action)),
                Is.EqualTo("Action 0 duration must be finite and non-negative"));
        }

        [TestCase("from")]
        [TestCase("to")]
        public void SwipeWithMalformedVector_ReturnsFieldShapeDiagnostic(string field)
        {
            var action = new UnityQaAction
            {
                type = "swipe",
                from = field == "from" ? new[] { 0.5f } : new[] { 0.5f, 0.5f },
                to = field == "to" ? new[] { 0.5f } : new[] { 0.5f, 0.5f },
                duration = 0.2f,
            };

            Assert.That(UnityQaScenarioValidator.Validate(RequestWith(action)),
                Is.EqualTo($"Action 0 {field} must contain exactly two coordinates"));
        }

        [TestCase("from")]
        [TestCase("to")]
        public void SwipeWithInvalidVector_ReturnsFieldRangeDiagnostic(string field)
        {
            var action = new UnityQaAction
            {
                type = "swipe",
                from = field == "from" ? new[] { float.PositiveInfinity, 0.5f } : new[] { 0.5f, 0.5f },
                to = field == "to" ? new[] { 0.5f, 1.01f } : new[] { 0.5f, 0.5f },
                duration = 0.2f,
            };

            Assert.That(UnityQaScenarioValidator.Validate(RequestWith(action)),
                Is.EqualTo($"Action 0 {field} must contain coordinates within 0..1"));
        }

        [TestCase(-0.01f)]
        [TestCase(float.PositiveInfinity)]
        public void SwipeWithInvalidDuration_ReturnsFieldDiagnostic(float duration)
        {
            var action = new UnityQaAction
            {
                type = "swipe",
                from = new[] { 0.5f, 0.5f },
                to = new[] { 0.5f, 0.5f },
                duration = duration,
            };

            Assert.That(UnityQaScenarioValidator.Validate(RequestWith(action)),
                Is.EqualTo("Action 0 duration must be finite and non-negative"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void KeyWithoutValue_ReturnsFieldDiagnostic(string key)
        {
            var request = RequestWith(new UnityQaAction { type = "key", key = key });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 key must not be empty"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void CaptureWithoutOutput_ReturnsFieldDiagnostic(string output)
        {
            var request = RequestWith(new UnityQaAction { type = "capture", output = output });

            Assert.That(UnityQaScenarioValidator.Validate(request),
                Is.EqualTo("Action 0 output must not be empty"));
        }

        [Test]
        public void ResultDefaultsToNoFailedAction()
        {
            Assert.That(new UnityQaResult().failedActionIndex, Is.EqualTo(-1));
        }

        private static UnityQaRequest Parse(string json)
        {
            return JsonUtility.FromJson<UnityQaRequest>(json);
        }

        private static UnityQaRequest RequestWith(UnityQaAction action)
        {
            return new UnityQaRequest { actions = new[] { action } };
        }
    }
}
