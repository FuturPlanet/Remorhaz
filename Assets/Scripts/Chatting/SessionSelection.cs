using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        ChatManager.i.onLoad.RemoveListener(LoadUnits);
    }
    public void LoadUnits()
    {
        var sessions = ChatManager.i.GetSessionsFromCurrentCollection();
        if(sessions == null) { return; }
        Transform createNewButton = null;
        foreach (Transform child in group)
        {
            if (child.tag == "Unit")
            {
                createNewButton = child;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        foreach (ChatSession session in sessions)
        {
            var sessionObj = Instantiate(unitPrefab, group);
            sessionObj.name = string.IsNullOrEmpty(session.name) ? "New Session" : session.name;
            var button = sessionObj.transform.Find("Button").GetComponent<Button>();
            sessionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = session.name;
            button.onClick.AddListener(() => OpenSession(session.id));
        }
        createNewButton.SetAsLastSibling();
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