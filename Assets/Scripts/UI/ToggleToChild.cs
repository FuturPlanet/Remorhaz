using UnityEngine;

public class ToggleToChild : MonoBehaviour
{
    public Transform parent;
    private void Awake()
    {
        if (parent == null)
        {
            parent = this.transform;
        }
    }
    public void ToggleTo(string childName)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            if(child.gameObject.name == childName)
            {
                child.gameObject.SetActive(true);
            }
        }
    }
}