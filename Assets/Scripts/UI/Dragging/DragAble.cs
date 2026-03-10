using UnityEngine;
using UnityEngine.EventSystems;

public class DragAble : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] 
    private float dragThreshold = 10f;
    private CanvasGroup canvasGroup;
    private Vector2 pointerStartPos;
    private GameObject dragCopy;
    private bool isDragging;

    void OnEnable()
    {
        if (!this.TryGetComponent<CanvasGroup>(out canvasGroup))
        {
            canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        }
    }
    public void OnPointerDown(PointerEventData eventData) => pointerStartPos = eventData.position;
    public void OnDrag(PointerEventData eventData)
    {
        if (Vector2.Distance(eventData.position, pointerStartPos) <= dragThreshold) return;

        if (!isDragging)
            StartDrag();

        UpdateDragPosition(eventData);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
            EndDrag();
    }
    private void StartDrag()
    {
        isDragging = true;
        DragManager.i.isDragging = true;
        DragManager.i.originalObject = transform;

        dragCopy = Instantiate(gameObject, DragManager.i.transform);

        var copyDragAble = dragCopy.GetComponent<DragAble>();
        if (copyDragAble != null)
        {
            copyDragAble.enabled = false;
            Destroy(copyDragAble);
        }

        dragCopy.GetComponent<CanvasGroup>().blocksRaycasts = false;
        canvasGroup.alpha = 0.3f;
    }
    private void UpdateDragPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            DragManager.i.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragCopy.GetComponent<RectTransform>().localPosition = localPoint;
    }
    private void EndDrag()
    {
        isDragging = false;
        canvasGroup.alpha = 1f;

        if (dragCopy != null)
        {
            Destroy(dragCopy);
            dragCopy = null;
        }

        DragManager.i.isDragging = false;
        DragManager.i.originalObject = null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging) OnClick();
    }
    private void OnClick() => Debug.Log("Clicked!");
}