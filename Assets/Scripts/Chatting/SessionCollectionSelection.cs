//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//public class SessionCollectionSelection : Singleton<SessionCollectionSelection>
//{
//    [SerializeField]
//    private GameObject unitPrefab;
//    [SerializeField]
//    private Transform group;
//    private void OnEnable()
//    {
//        ChatManager.i.onLoad.AddListener(LoadUnits);
//        RuntimeManager.i.AddTask(0, true, 3000).AddListener(this, LoadUnits);
//    }
//    private void OnDisable()
//    {
//        if (ChatManager.i != null)
//        {
//            ChatManager.i.onLoad.RemoveListener(LoadUnits);
//        }
//    }
//    public void LoadUnits()
//    {
//        if (DragPanelManager.i.isDragging) return;
//        var collections = ChatManager.i.sessionCollections;
//        Transform createNewButton = null;
//        Transform createFolderButton = null;
//        Transform backButton = null;
//        foreach (Transform child in group)
//        {
//            if (child.CompareTag("Unit"))
//            {
//                if (child.name == "Create New")
//                {
//                    createNewButton = child;
//                }
//                else if (child.name == "Create Folder")
//                {
//                    createFolderButton = child;
//                }
//                else if (child.name == "BackButton")
//                {
//                    backButton = child;
//                }
//            }
//            else
//            {
//                Destroy(child.gameObject);
//            }
//        }
//        if (!ChatManager.i.trees.ContainsKey("sessionCollectionSelection"))
//        {
//            ChatManager.i.trees["sessionCollectionSelection"] = DragPanelManager.i.CreateTree(group);
//            ChatManager.i.Save();
//        }
//        foreach (ChatSessionCollection collection in collections.OrderBy(c => c.index))
//        {
//            DragPanelManager.i.LoadTree(group, ChatManager.i.trees["sessionCollectionSelection"]);
//            var collectionObj = Instantiate(unitPrefab, group);
//            var dragAble = DragPanelManager.i.MakeObjectDragable(collectionObj, collection.folderData);
//            if (dragAble == null) Logger.i.Log(this, "Unit couldn't be made dragable.", LogType.Error);
//            dragAble.onDragEnd.AddListener(() => ChatManager.i.GetCollectionById(collection.id).folderData = dragAble.folderData);
//            collectionObj.name = string.IsNullOrEmpty(collection.name) ? "New Session" : collection.name;
//            var button = collectionObj.transform.Find("Button").GetComponent<Button>();
//            collectionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = collection.name;
//            collectionObj.GetComponent<InfoHolder>().info.Add("collectionId", collection.id);
//            button.onClick.AddListener(() => OpenCollection(collection.id));
//        }
//        createNewButton.SetAsLastSibling();
//        createFolderButton.SetAsLastSibling();
//        if(backButton != null) backButton.SetAsLastSibling();
//    }
//    public void UpdateIndexes()
//    {
//        List<string> orderedIds = new List<string>();
//        foreach (Transform child in group)
//        {
//            if (child.tag == "Unit") continue;
//            string collectionId = child.GetComponent<InfoHolder>().info["collectionId"].ToString();
//            if (ChatManager.i.GetCollectionById(collectionId) == null)
//            {
//                Debug.LogWarning("Could not update Indexes.");
//                return;
//            }
//            orderedIds.Add(collectionId);
//        }
//        ChatManager.i.SetCollectionIndexes(orderedIds);
//    }
//    public void OpenCollection(string id)
//    {
//        ChatSessionCollection collection = ChatManager.i.GetCollectionById(id);
//        if (collection == null) 
//        {
//            Debug.LogWarning($"Corrupted Collection ID: {id}");
//            return; 
//        }
//        ChatManager.i.SetActiveCollection(id);
//        ChatView.i.OpenSessionPanel();
//    }
//}