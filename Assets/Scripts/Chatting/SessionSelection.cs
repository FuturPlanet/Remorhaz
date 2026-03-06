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
        ChatManager.i.OnEnable();
        InvokeRepeating(nameof(LoadUnits), 2f, 2f);
    }
    public void LoadUnits()
    {
        var sessions = ChatManager.i.sessions;
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
            sessionObj.name = session.name;
            var button = sessionObj.transform.Find("Button").GetComponent<Button>();
            sessionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = session.name;
            string sessionId = session.id;
            button.onClick.AddListener(() => OpenSession(sessionId));
        }
        createNewButton.SetAsLastSibling();
    }
    public void OpenSession(string id)
    {
        ChatSession session = ChatManager.i.GetSessionById(id);
        if (session == null) { return; }
        ChatManager.i.SetActiveSession(id);
        ChatView.i.OpenView();
    }
}