using UnityEngine;
using UnityEngine.UI;

public class CreateDragFolderHere : MonoBehaviour
{
    private Button button;
    private void OnEnable()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(CreateFolder);
        }
    }
    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(CreateFolder);
        }
    }
    private void CreateFolder()
    {
        DragPanelManager.i.CreateFolder(this.transform.parent.parent);
    }
}