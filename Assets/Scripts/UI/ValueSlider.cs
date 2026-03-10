using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ValueSlider : MonoBehaviour
{
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private TMP_InputField input;
    private void OnEnable()
    {
        input.onEndEdit.AddListener(UpdateSlider);
        slider.onValueChanged.AddListener(UpdateInput);
    }
    private void OnDisable()
    {
        input.onEndEdit.RemoveListener(UpdateSlider);
        slider.onValueChanged.RemoveListener(UpdateInput);
    }
    private void UpdateSlider(string value)
    {
        if (float.TryParse(value, out float result))
        {
            slider.value = result;
        }
    }
    private void UpdateInput(float value)
    {
        input.text = value.ToString("0.##");
    }
}