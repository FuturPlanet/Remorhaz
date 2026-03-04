using UnityEngine;
using UnityEngine.Events;

public class ToggleTwoObjects : MonoBehaviour
{
    [SerializeField]
    private GameObject obj1;
    [SerializeField]
    private GameObject obj2;
    public UnityEvent onToggle;
    private void OnEnable()
    {
        if ((obj1.activeSelf && obj2.activeSelf) || (!obj1.activeSelf && !obj2.activeSelf))
        {
            obj1.SetActive(true);
            obj2.SetActive(false);
        }
    }
    public void Toggle()
    {
        obj1.SetActive(!obj1.activeSelf);
        obj2.SetActive(!obj2.activeSelf);
        onToggle?.Invoke();
    }
    public void Toggle(bool firstActive)
    {
        obj1.SetActive(firstActive);
        obj2.SetActive(!firstActive);
        onToggle?.Invoke();
    }
}