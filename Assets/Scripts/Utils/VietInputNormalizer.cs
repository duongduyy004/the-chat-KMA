using TMPro;
using UnityEngine;

/// Normalize committed input, leaving the IME composition untouched while typing.
[RequireComponent(typeof(TMP_InputField))]
public sealed class VietInputNormalizer : MonoBehaviour
{
    TMP_InputField field;

    void Awake() => field = GetComponent<TMP_InputField>();
    void OnEnable()
    {
        if (field == null) field = GetComponent<TMP_InputField>();
        field.onEndEdit.AddListener(Normalize);
    }
    void OnDisable() => field.onEndEdit.RemoveListener(Normalize);
    void Normalize(string value) => field.SetTextWithoutNotify(VietText.Fix(value));
}
