using System;
using System.Collections;
using System.Linq;
using KMA.Gameplay.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace KMA.Tests.Presentation
{
    public sealed class VietnameseInputTests
    {
        [UnityTest]
        public IEnumerator CommittedInputComposesVietnameseWithoutRecursiveValueCallbacks()
        {
            var root = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
            try
            {
                var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(root.transform, false);
                var text = textObject.GetComponent<TextMeshProUGUI>();
                text.font = VietTypography.Library.regular;
                var input = root.GetComponent<TMP_InputField>();
                input.textComponent = text;
                input.textViewport = (RectTransform)root.transform;
                Type normalizer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("VietInputNormalizer")).First(t => t != null);
                root.AddComponent(normalizer);
                yield return null;
                input.SetTextWithoutNotify("Nguye\u0302\u0303n");
                int changes = 0;
                input.onValueChanged.AddListener(_ => changes++);
                input.onEndEdit.Invoke(input.text);
                Assert.That(input.text, Is.EqualTo("Nguyễn"));
                Assert.That(changes, Is.Zero);
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
    }
}
