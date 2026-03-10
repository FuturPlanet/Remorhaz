using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatView : Singleton<ChatView>
{
    [SerializeField]
    private GameObject messagePrefab;
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
        ChatManager.i.onMessageAdded.RemoveListener(Populate);
        ChatManager.i.onMessageDeleted.RemoveListener(Refresh);
        DragManager.i.onReorder.RemoveListener(UpdateIndexes);
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
            var messageObj = Instantiate(messagePrefab, group);
            messageObj.name = timestamp;
            TMP_InputField input = messageObj.transform.Find("Bubble/InputField (TMP)").GetComponent<TMP_InputField>();
            input.text = message.content;
            input.onEndEdit.AddListener(_ => EditMessageText(timestamp));
            Button deleteButton = messageObj.transform.Find("Delete").GetComponent<Button>();
            if (message.role != "user")
            {
                var messageRect = input.GetComponent<RectTransform>();
                messageRect.anchoredPosition += new Vector2(-250, 0);

                var deleteRect = deleteButton.GetComponent<RectTransform>();
                deleteRect.anchoredPosition += new Vector2(-250, 0);
            }
            if (message.isLinkOutput)
            {
                messageObj.SetActive(false);
            }
            messages.Add(timestamp, input);
            deleteButton.onClick.AddListener(() => ChatManager.i.DeleteMessage(timestamp));
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