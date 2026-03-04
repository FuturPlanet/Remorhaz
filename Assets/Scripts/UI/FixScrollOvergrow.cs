using UnityEngine;
public class FixScrollOvergrow : MonoBehaviour
{
    private RectTransform rectTransform;
    private void Awake()
    {
        rectTransform = this.GetComponent<RectTransform>();
    }
    private void LateUpdate()
    {
        if (rectTransform.GetSiblingIndex() == 0)
        {

        }
    }
}