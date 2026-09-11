using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SessionCollectionSelection : Singleton<SessionCollectionSelection>
{
    [SerializeField]
    private GameObject unitPrefab;
    [SerializeField]
    private Transform group;
    private void OnEnable()
    {
        ChatManager.i.onLoad.AddListener(LoadUnits);
        InvokeRepeating(nameof(LoadUnits), 2f, 2f);
    }
    private void OnDisable()
    {
        if (ChatManager.i != null)
        {
            ChatManager.i.onLoad.RemoveListener(LoadUnits);
        }
        CancelInvoke(nameof(LoadUnits));
    }
    public void LoadUnits()
    {
        if (DragPanelManager.i.isDragging) return;
        var collections = ChatManager.i.sessionCollections;
        Transform createNewButton = null;
        Transform createFolderButton = null;
        foreach (Transform child in group)
        {
            if (child.CompareTag("Unit"))
            {
                if (child.name == "Create New")
                {
                    createNewButton = child;
                }
                else if (child.name == "Create Folder")
                {
                    createFolderButton = child;
                }
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        foreach (ChatSessionCollection collection in collections.OrderBy(c => c.index))
        {
            var collectionObj = Instantiate(unitPrefab, group);

            if (ChatManager.i.trees.ContainsKey("sessionCollectionSelection"))
            {
                DragPanelManager.i.LoadTree(group, ChatManager.i.trees["sessionCollectionSelection"]);
            }
            else
            {
                ChatManager.i.trees["sessionCollectionSelection"] = DragPanelManager.i.CreateTree(group);
                ChatManager.i.Save();
            }
            collectionObj.name = string.IsNullOrEmpty(collection.name) ? "New Session" : collection.name;
            var button = collectionObj.transform.Find("Button").GetComponent<Button>();
            collectionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = collection.name;
            collectionObj.GetComponent<InfoHolder>().info.Add("collectionId", collection.id);
            button.onClick.AddListener(() => OpenCollection(collection.id));
        }
        createNewButton.SetAsLastSibling();
        createFolderButton.SetAsLastSibling();
    }
    public void UpdateIndexes()
    {
        List<string> orderedIds = new List<string>();
        foreach (Transform child in group)
        {
            if (child.tag == "Unit") continue;
            string collectionId = child.GetComponent<InfoHolder>().info["collectionId"].ToString();
            if (ChatManager.i.GetCollectionById(collectionId) == null)
            {
                Debug.LogWarning("Could not update Indexes.");
                return;
            }
            orderedIds.Add(collectionId);
        }
        ChatManager.i.SetCollectionIndexes(orderedIds);
    }
    public void OpenCollection(string id)
    {
        ChatSessionCollection collection = ChatManager.i.GetCollectionById(id);
        if (collection == null) 
        {
            Debug.LogWarning($"Corrupted Collection ID: {id}");
            return; 
        }
        ChatManager.i.SetActiveCollection(id);
        ChatView.i.OpenSessionPanel();
    }
}