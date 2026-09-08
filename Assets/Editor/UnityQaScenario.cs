using System;

namespace KMA.EditorTools
{
    [Serializable]
    public sealed class UnityQaRequest
    {
        public string id;
        public string scene;
        public string output;
        public float waitSeconds = 3f;
        public UnityQaAction[] actions;
    }

    [Serializable]
    public sealed class UnityQaAction
    {
        public string type;
        public string target;
        public float[] position;
        public float[] from;
        public float[] to;
        public float seconds;
        public float duration;
        public string key;
        public string output;
    }

    [Serializable]
    public sealed class UnityQaResult
    {
        public string id;
        public string status;
        public string message;
        public int failedActionIndex = -1;
        public float elapsedSeconds;
        public string[] screenshots;
    }

    public static class UnityQaScenarioValidator
    {
        public static string Validate(UnityQaRequest request)
        {
            if (request == null)
                return "Request must not be null";

            if (!IsFiniteNonNegative(request.waitSeconds))
                return "Request waitSeconds must be finite and non-negative";

            if (request.actions == null)
                return null;

            for (var i = 0; i < request.actions.Length; i++)
            {
                var diagnostic = ValidateAction(request.actions[i], i);
                if (diagnostic != null)
                    return diagnostic;
            }

            return null;
        }

        private static string ValidateAction(UnityQaAction action, int index)
        {
            if (action == null)
                return $"Action {index} must not be null";

            switch (action.type)
            {
                case "wait":
                    return IsFiniteNonNegative(action.seconds)
                        ? null
                        : $"Action {index} seconds must be finite and non-negative";

                case "tap":
                    return ValidatePointerAction(action, index, false);

                case "hold":
                    return ValidatePointerAction(action, index, true);

                case "swipe":
                    return ValidateSwipe(action, index);

                case "key":
                    if (string.IsNullOrWhiteSpace(action.key))
                        return $"Action {index} key must not be empty";
                    return IsFiniteNonNegative(action.duration)
                        ? null
                        : $"Action {index} duration must be finite and non-negative";

                case "capture":
                    return string.IsNullOrWhiteSpace(action.output)
                        ? $"Action {index} output must not be empty"
                        : null;

                default:
                    return $"Action {index} has unsupported type: {action.type}";
            }
        }

        private static string ValidatePointerAction(UnityQaAction action, int index, bool validateDuration)
        {
            var hasTarget = !string.IsNullOrWhiteSpace(action.target);
            var hasPosition = action.position != null;
            if (hasTarget == hasPosition)
                return $"Action {index} must specify exactly one locator";

            if (hasPosition)
            {
                var diagnostic = ValidateVector(action.position, index, "position");
                if (diagnostic != null)
                    return diagnostic;
            }

            if (validateDuration && !IsFiniteNonNegative(action.duration))
                return $"Action {index} duration must be finite and non-negative";

            return null;
        }

        private static string ValidateSwipe(UnityQaAction action, int index)
        {
            var diagnostic = ValidateVector(action.from, index, "from");
            if (diagnostic != null)
                return diagnostic;

            diagnostic = ValidateVector(action.to, index, "to");
            if (diagnostic != null)
                return diagnostic;

            return IsFiniteNonNegative(action.duration)
                ? null
                : $"Action {index} duration must be finite and non-negative";
        }

        private static string ValidateVector(float[] vector, int index, string field)
        {
            if (vector == null || vector.Length != 2)
                return $"Action {index} {field} must contain exactly two coordinates";

            if (!IsNormalizedCoordinate(vector[0]) || !IsNormalizedCoordinate(vector[1]))
                return $"Action {index} {field} must contain coordinates within 0..1";

            return null;
        }

        private static bool IsNormalizedCoordinate(float value)
        {
            return IsFinite(value) && value >= 0f && value <= 1f;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
