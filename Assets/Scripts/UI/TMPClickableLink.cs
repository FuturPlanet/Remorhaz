using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class TMPClickableLink : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text tmpText;
    private Camera mainCamera;
    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        mainCamera = tmpText.canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : tmpText.canvas.worldCamera;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, mainCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            string linkID = linkInfo.GetLinkID();
            Application.OpenURL(linkID);
        }
    }
}