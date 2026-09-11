using UnityEngine;

public class ChatFocuser : Singleton<ChatFocuser>
{
    public GameObject collectionPanel;
    public GameObject collectionSettings;
    public GameObject sessionPanel;
    public GameObject sessionSettings;
    public GameObject leftBackPanel;
    public GameObject chatHistory;
    private void OnEnable()
    {
        FocusManager.i.Add(new[] { chatHistory, sessionPanel, leftBackPanel, collectionSettings, sessionSettings }, new[] { collectionPanel }, nameof(collectionPanel));
        FocusManager.i.Add(new[] { chatHistory, leftBackPanel, sessionSettings }, new[] { collectionPanel, sessionPanel, collectionSettings }, nameof(sessionPanel));
        FocusManager.i.Add(new[] { collectionPanel, collectionSettings }, new[] { sessionPanel, leftBackPanel, chatHistory, sessionSettings }, nameof(chatHistory));
        ChatView.i.OpenCollectionPanel();
    }
    private void OnDisable()
    {
        if (FocusManager.i != null)
        {
            FocusManager.i.Remove(nameof(collectionPanel));
            FocusManager.i.Remove(nameof(sessionPanel));
            FocusManager.i.Remove(nameof(chatHistory));
        }
    }
}