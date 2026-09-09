using UnityEngine;
using UnityEngine.Events;

public class ToggleObject : MonoBehaviour
{
    [SerializeField]
    private GameObject obj;
    public UnityEvent onToggle;
    public void Toggle()
    {
        obj.SetActive(!obj.activeSelf);
        onToggle?.Invoke();
    }
}