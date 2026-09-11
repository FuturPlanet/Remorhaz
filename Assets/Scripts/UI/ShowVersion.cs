using TMPro;
using UnityEngine;

public class ShowVersion : MonoBehaviour
{
    private TMP_Text versionText;

    private void Awake()
    {
        versionText = this.GetComponent<TMP_Text>();
    }
    private void OnEnable()
    {
        versionText.text = $"v{Application.version}";
    }
}