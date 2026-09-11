using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAble : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public string folderData;

    [SerializeField]
    private float dragThreshold = 10f;

    private CanvasGroup canvasGroup;
    private Vector2 pointerStartPos;
    private GameObject dragCopy;
    private bool isDragging;

    private void OnEnable()
    {
        if (!TryGetComponent(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    public void OnPointerDown(PointerEventData eventData) => pointerStartPos = eventData.position;
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging && Vector2.Distance(eventData.position, pointerStartPos) <= dragThreshold) return;

        if (!isDragging)
        {
            StartDrag();
        }

        UpdateDragPosition(eventData);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            EndDrag(eventData);
        }
    }
    private void StartDrag()
    {
        isDragging = true;
        DragPanelManager.i.isDragging = true;
        DragPanelManager.i.originalObject = transform;

        dragCopy = Instantiate(gameObject, DragPanelManager.i.transform);

        if (dragCopy.TryGetComponent(out DragAble copyDragAble))
        {
            Destroy(copyDragAble);
        }

        if (dragCopy.TryGetComponent(out CanvasGroup copyCanvasGroup))
        {
            copyCanvasGroup.blocksRaycasts = false;
        }

        canvasGroup.alpha = 0.3f;
    }
    private void UpdateDragPosition(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            DragPanelManager.i.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragCopy.GetComponent<RectTransform>().localPosition = localPoint;
    }
    private void EndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.alpha = 1f;

        ResolveDrop(eventData);

        if (dragCopy != null)
        {
            Destroy(dragCopy);
            dragCopy = null;
        }

        DragPanelManager.i.isDragging = false;
        DragPanelManager.i.originalObject = null;
        DragPanelManager.i.CurrentDropTarget = null;
    }
    private void ResolveDrop(PointerEventData eventData)
    {
        string entryId = DragPanelManager.i.GetObjId(folderData);
        if (string.IsNullOrEmpty(entryId)) return;
        DragFolderObj target = DragPanelManager.i.CurrentDropTarget;
        if (target != null && target.folderId != entryId)
        {
            DragPanelManager.i.MoveIntoFolder(entryId, target.folderId);
            return;
        }
        DragAble siblingHit = FindDragAbleUnderPointer(eventData);
        if (siblingHit != null && siblingHit != this && siblingHit.transform.parent == transform.parent)
        {
            int targetIndex = DragPanelManager.i.GetLevelIndex(siblingHit.transform);
            DragPanelManager.i.MoveEntry(entryId, targetIndex);
            return;
        }
    }
    private DragAble FindDragAbleUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (dragCopy != null && result.gameObject.transform.IsChildOf(dragCopy.transform)) continue;

            if (result.gameObject.TryGetComponent(out DragAble hit) && hit != this)
            {
                return hit;
            }

            DragAble parentHit = result.gameObject.GetComponentInParent<DragAble>();
            if (parentHit != null && parentHit != this)
            {
                return parentHit;
            }
        }
        return null;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging)
        {
            OnClick();
        }
    }

    private void OnClick()
    {
        if (TryGetComponent(out DragFolderObj folder))
        {
            folder.OpenFolder();
        }
    }
}