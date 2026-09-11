using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SessionSelection : Singleton<SessionSelection>
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
        var sessions = ChatManager.i.GetSessionsFromCurrentCollection();
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
        foreach (ChatSession session in sessions.OrderBy(c => c.index))
        {
            var sessionObj = Instantiate(unitPrefab, group);

            if (ChatManager.i.trees.ContainsKey("sessionSelection"))
            {
                DragPanelManager.i.LoadTree(group, ChatManager.i.trees["sessionSelection"]);
            }
            else
            {
                ChatManager.i.trees["sessionSelection"] = DragPanelManager.i.CreateTree(group);
                ChatManager.i.Save();
            }
            sessionObj.name = string.IsNullOrEmpty(session.name) ? "New Session" : session.name;
            var button = sessionObj.transform.Find("Button").GetComponent<Button>();
            sessionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = session.name;
            sessionObj.GetComponent<InfoHolder>().info.Add("sessionId", session.id);
            button.onClick.AddListener(() => OpenSession(session.id));
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
            string sessionId = child.GetComponent<InfoHolder>().info["sessionId"].ToString();
            if (ChatManager.i.GetSessionById(sessionId) == null)
            {
                Debug.LogWarning("Could not update Indexes.");
                return;
            }
            orderedIds.Add(sessionId);
        }
        ChatManager.i.SetSessionIndexes(orderedIds);
    }
    public void OpenSession(string id)
    {
        ChatSession session = ChatManager.i.GetSessionById(id);
        if (session == null)
        {
            Debug.LogWarning($"Corrupted Session ID: {id}");
            return;
        }
        ChatManager.i.SetActiveSession(id);
        ChatView.i.OpenChatHistory();
    }
}