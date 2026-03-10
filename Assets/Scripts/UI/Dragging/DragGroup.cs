using UnityEngine;
using UnityEngine.InputSystem;

public class DragGroup : MonoBehaviour
{
    public bool foldering = true;
    private Transform dragged;
    [SerializeField] private GameObject highlighterPrefab;
    private GameObject highlighter;

    private void Awake()
    {
        highlighter = Instantiate(highlighterPrefab, transform);
        highlighter.SetActive(false);
    }
    private void Update()
    {
        if (!DragManager.i.isDragging)
        {
            if (highlighter.activeSelf)
            {
                dragged.SetSiblingIndex(highlighter.transform.GetSiblingIndex());
                highlighter.SetActive(false);
                DragManager.i.onReorder?.Invoke();
            }
            dragged = null;
            return;
        }
        dragged = DragManager.i.originalObject;
        if (dragged.parent != transform)
        {
            highlighter.SetActive(false);
            return;
        }
        highlighter.SetActive(true);
        int newIndex = GetInsertIndex(Mouse.current.position.ReadValue());
        highlighter.transform.SetSiblingIndex(newIndex);
    }
    private int GetInsertIndex(Vector2 screenPos)
    {
        int index = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == highlighter.transform) continue;
            if (child == DragManager.i.originalObject) continue;

            RectTransform rect = child as RectTransform;
            if (screenPos.y > rect.position.y)
                return index;

            index++;
        }
        return index;
    }
}