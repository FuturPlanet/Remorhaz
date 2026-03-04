using UnityEngine;

public class MatchTargetSize : MonoBehaviour
{
    [SerializeField]
    private RectTransform target;
    [Header("Horizontal")]
    public bool horizontal = false;
    public float minimumTargetX = 100f;
    [Header("Vertical")]
    public bool vertical = false;
    public float minimumTargetY = 100f;

    private Vector2 initialSize;
    private Vector2 initialTargetSize;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialSize = rectTransform.sizeDelta;
        initialTargetSize = target.sizeDelta;
    }

    private void LateUpdate()
    {
        if (target.gameObject.activeInHierarchy)
        {
            float growthX = target.sizeDelta.x - initialTargetSize.x;
            float growthY = target.sizeDelta.y - initialTargetSize.y;

            float newWidth = initialSize.x;
            float newHeight = initialSize.y;

            if (horizontal && growthX > minimumTargetX)
            {
                newWidth = initialSize.x + (growthX - minimumTargetX);
            }

            if (vertical && growthY > minimumTargetY)
            {
                newHeight = initialSize.y + (growthY - minimumTargetY);
            }

            rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        }
        else
        {
            rectTransform.sizeDelta = initialSize;
        }
    }
}