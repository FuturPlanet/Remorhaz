using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatView : Singleton<ChatView>
{
    [SerializeField]
    private GameObject messagePrefab;
    [SerializeField]
    private GameObject linkHiderPrefab;
    [SerializeField]
    private GameObject reasonHiderPrefab;
    public TMP_InputField input;
    [SerializeField]
    private Transform group;
    [SerializeField]
    private GameObject chatHistory;
    private Dictionary<string, TMP_InputField> messages = new Dictionary<string, TMP_InputField>();
    
    private void OnEnable()
    {
        ChatManager.i.onMessageAdded.AddListener(Populate);
        ChatManager.i.onMessageDeleted.AddListener(Refresh);
        DragManager.i.onReorder.AddListener(UpdateIndexes);
    }
    private void OnDisable()
    {
        if (ChatManager.i != null)
        {
            ChatManager.i.onMessageAdded.RemoveListener(Populate);
            ChatManager.i.onMessageDeleted.RemoveListener(Refresh);
        }
        if (DragManager.i != null)
        {
            DragManager.i.onReorder.RemoveListener(UpdateIndexes);
        }
    }
    public void Refresh()
    {
        OpenCollectionPanel();
        OpenChatHistory();
    }
    private void Populate()
    {
        foreach (ChatMessage message in ChatManager.i.activeSession.messages)
        {
            string timestamp = message.timestamp;
            if (messages.ContainsKey(timestamp)) { continue; }
            if (!string.IsNullOrEmpty(message.reasoning))
            {
                var reasonObj = Instantiate(reasonHiderPrefab, group);
                reasonObj.name = timestamp + "_reasoning";
                reasonObj.transform.Find("Bubble/InputField (TMP)")
                         .GetComponent<TMP_InputField>().text = message.reasoning;
            }
            var messageObj = Instantiate(messagePrefab, group);
            messageObj.name = timestamp;
            TMP_InputField messageInput = messageObj.transform.Find("Bubble/InputField (TMP)").GetComponent<TMP_InputField>();
            messageInput.text = message.content;
            messageInput.onEndEdit.AddListener(_ => EditMessageText(timestamp));
            Button deleteButton = messageObj.transform.Find("Delete").GetComponent<Button>();
            deleteButton.onClick.AddListener(() => ChatManager.i.DeleteMessage(timestamp));
            if (message.isLink)
            {
                messageObj.SetActive(false);
                var linkObj = Instantiate(linkHiderPrefab, group);
                linkObj.name = timestamp + "_link";
                linkObj.transform.Find("Bubble/InputField (TMP)")
                       .GetComponent<TMP_InputField>().text = message.content;
            }
            messages.Add(timestamp, messageInput);
        }
        input.transform.parent.SetAsLastSibling();
    }
    private void UpdateIndexes()
    {
        SessionCollectionSelection.i.UpdateIndexes();
        SessionSelection.i.UpdateIndexes();
    }
    public void EditMessageText(string timestamp)
    {
        ChatMessage message = ChatManager.i.activeSession.messages.Find(c => c.timestamp == timestamp);
        message.content = messages[timestamp].text;
        ChatManager.i.Save();
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
    public void OpenCollectionPanel()
    {
        FocusManager.i.FocusOn(nameof(ChatFocuser.i.collectionPanel));
        SessionCollectionSelection.i.LoadUnits();
    }
    public void OpenSessionPanel()
    {
        FocusManager.i.FocusOn(nameof(ChatFocuser.i.sessionPanel));
        SessionSelection.i.LoadUnits();
    }
    public void OpenChatHistory()
    {
        FocusManager.i.FocusOn(nameof(ChatFocuser.i.chatHistory));
        ClearMessages();
        Populate();
    }
}