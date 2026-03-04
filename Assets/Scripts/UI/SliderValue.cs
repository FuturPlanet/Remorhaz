using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SliderValue : MonoBehaviour
{
    [SerializeField, Header("Enter 10, 1, 0.1, 0.01, 0.001, etc")]
    private float digitRound = 1;
    [SerializeField]
    private Slider slider;
    private TMP_Text text;
    private void Awake()
    {
        text = this.GetComponent<TMP_Text>();
    }
    private void LateUpdate()
    {
        float roundedValue = Mathf.Round(slider.value / digitRound) * digitRound;
        slider.value = roundedValue;
        text.text = slider.value.ToString();
    }
}