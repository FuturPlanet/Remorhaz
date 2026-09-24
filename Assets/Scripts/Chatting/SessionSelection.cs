//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//public class SessionSelection : Singleton<SessionSelection>
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
//        var sessions = ChatManager.i.GetSessionsFromCurrentCollection();
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
//        if (!ChatManager.i.trees.ContainsKey("sessionSelection"))
//        {
//            ChatManager.i.trees["sessionSelection"] = DragPanelManager.i.CreateTree(group);
//            ChatManager.i.Save();
//        }
//        foreach (ChatSession session in sessions.OrderBy(c => c.index))
//        {
//            DragPanelManager.i.LoadTree(group, ChatManager.i.trees["sessionSelection"]);
//            var sessionObj = Instantiate(unitPrefab, group);
//            var dragAble = DragPanelManager.i.MakeObjectDragable(sessionObj, session.folderData);
//            if (dragAble == null) Logger.i.Log(this, "Unit couldn't be made dragable.", LogType.Error);
//            dragAble.onDragEnd.AddListener(() => ChatManager.i.GetSessionById(session.id).folderData = dragAble.folderData);
//            sessionObj.name = string.IsNullOrEmpty(session.name) ? "New Session" : session.name;
//            var button = sessionObj.transform.Find("Button").GetComponent<Button>();
//            sessionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = session.name;
//            sessionObj.GetComponent<InfoHolder>().info.Add("sessionId", session.id);
//            button.onClick.AddListener(() => OpenSession(session.id));
//        }
//        createNewButton.SetAsLastSibling();
//        createFolderButton.SetAsLastSibling();
//        if (backButton != null) backButton.SetAsLastSibling();
//    }
//    public void UpdateIndexes()
//    {
//        List<string> orderedIds = new List<string>();
//        foreach (Transform child in group)
//        {
//            if (child.tag == "Unit") continue;
//            string sessionId = child.GetComponent<InfoHolder>().info["sessionId"].ToString();
//            if (ChatManager.i.GetSessionById(sessionId) == null)
//            {
//                Debug.LogWarning("Could not update Indexes.");
//                return;
//            }
//            orderedIds.Add(sessionId);
//        }
//        ChatManager.i.SetSessionIndexes(orderedIds);
//    }
//    public void OpenSession(string id)
//    {
//        ChatSession session = ChatManager.i.GetSessionById(id);
//        if (session == null)
//        {
//            Debug.LogWarning($"Corrupted Session ID: {id}");
//            return;
//        }
//        ChatManager.i.SetActiveSession(id);
//        ChatView.i.OpenChatHistory();
//    }
//}