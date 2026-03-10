using UnityEngine;
using UnityEngine.Events;

public class DragManager : Singleton<DragManager>
{
    public Transform originalObject;
    public bool isDragging;
    public UnityEvent onReorder;
}