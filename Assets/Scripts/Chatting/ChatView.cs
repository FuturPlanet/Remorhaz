using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChatView : Singleton<ChatView>
{
    [SerializeField]
    private GameObject messagePrefab;
    [SerializeField]
    private Transform group;
    [SerializeField]
    private GameObject window;
    private Dictionary<string, TMP_InputField> messages = new Dictionary<string, TMP_InputField>();
    private void OnEnable()
    {
        CloseView();
        ChatManager.i.onMessageAdded.AddListener(Populate);
        ChatManager.i.onMessageDeleted.AddListener(Populate);
    }
    private void OnDisable()
    {
        ChatManager.i.onMessageAdded.RemoveListener(Populate);
        ChatManager.i.onMessageDeleted.RemoveListener(Populate);
    }
    public void Populate()
    {
        foreach (ChatMessage message in ChatManager.i.activeSession.messages)
        {
            string timestamp = message.timestamp;
            if (messages.ContainsKey(timestamp)) { continue; }
            var messageObj = Instantiate(messagePrefab, group);
            messageObj.name = timestamp;
            TMP_InputField input = messageObj.transform.Find("Bubble/InputField (TMP)").GetComponent<TMP_InputField>();
            input.text = message.content;
            input.onEndEdit.AddListener(_ => EditMessageText(timestamp));
            if(message.role != "user")
            {
                input.GetComponent<RectTransform>().position = new Vector3(-150, 0, 0);
            }
            if (message.isLinkOutput)
            {
                messageObj.SetActive(false);
            }
            messages.Add(timestamp, input);
        }
    }
    public void EditMessageText(string timestamp)
    {
        ChatMessage message = ChatManager.i.activeSession.messages.Find(c => c.timestamp == timestamp);
        message.content = messages[timestamp].text;
    }
    public void ClearMessages()
    {
        foreach (Transform message in group)
        {
            if (message.tag != "Unit")
            {
                Destroy(message.gameObject);
            }
        }
        messages.Clear();
    }
    public void OpenView()
    {
        ClearMessages();
        Populate();
        window.gameObject.SetActive(true);
    }
    public void CloseView()
    {
        window.gameObject.SetActive(false);
    }
}