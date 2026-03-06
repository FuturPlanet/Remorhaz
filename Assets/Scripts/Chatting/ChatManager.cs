using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class ChatManager : Singleton<ChatManager>
{
    [HideInInspector]
    public List<ChatSession> sessions = new List<ChatSession>();
    [SerializeField]
    private TMP_InputField input;
    [SerializeField]
    private TMP_InputField sessionName;
    public ChatSession activeSession { get; private set; }

    public UnityEvent onLoad;
    public UnityEvent onSave;
    public UnityEvent onMessageAdded;
    public UnityEvent onMessageDeleted;
    public void OnEnable()
    {
        LoadSessions();
    }
    private void Update()
    {
        if(activeSession != null)
        {
            LorePanel.i.Initialize();
            activeSession.name = sessionName.text;
            activeSession.chainId = LorePanel.i.GetSelectedChainId();
        }
    }
    public void SetActiveSession(string sessionId)
    {
        activeSession = sessions.Find(s => s.id == sessionId);
        sessionName.text = activeSession.name;
    }
    public async void Send()
    {
        if (activeSession == null)
        {
            Debug.LogWarning("No active session.");
            return;
        }
        if (ChainExecutor.Instance.isRunning)
        {
            Debug.LogWarning("Chain already running.");
            return;
        }
        ChatMessage userMessage = new ChatMessage
        {
            role = "user",
            content = input.text,
            isLinkOutput = false,
            timestamp = System.DateTime.Now.ToString("o")
        };
        AddMessage(userMessage);
        Chain chain = ChainEditor.Instance.GetChainById(activeSession.chainId);
        if (chain == null)
        {
            Debug.LogError($"Chain '{activeSession.chainId}' not found.");
            return;
        }
        await ChainExecutor.Instance.Execute(chain, activeSession.messages);
    }
    public void CreateNewSession()
    {
        ChatSession newSession = new ChatSession
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Session",
            messages = new List<ChatMessage>()
        };
        sessions.Add(newSession);
        SaveSessions();
        SetActiveSession(newSession.id);
        Debug.Log("Created a new Session.");
    }
    public void DeleteCurrentSession()
    {
        if (activeSession != null)
        {
            ChatView.i.CloseView();
            sessions.Remove(activeSession);
            activeSession = null;
            SaveSessions();
        }
    }
    public void AddMessage(ChatMessage message)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("No active session.");
            return;
        }
        activeSession.messages.Add(message);
        SaveSessions();
        onMessageAdded?.Invoke();
    }
    public void DeleteMessage(string timestamp)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("No active session.");
            return;
        }
        ChatMessage message = activeSession.messages.Find(m => m.timestamp == timestamp);
        if (message != null)
        {
            activeSession.messages.Remove(message);
            SaveSessions();
            onMessageDeleted?.Invoke();
        }
    }
    public void DeleteMessageAt(int index)
    {
        if (activeSession == null || index < 0 || index >= activeSession.messages.Count)
            return;
        ChatMessage message = activeSession.messages[index];
        activeSession.messages.RemoveAt(index);
        SaveSessions();
        onMessageDeleted?.Invoke();
    }
    public void SaveSessions()
    {
        ES3.Save("chatSessions", sessions);
        onSave?.Invoke();
    }
    private void LoadSessions()
    {
        if (ES3.KeyExists("chatSessions"))
        {
            sessions = ES3.Load<List<ChatSession>>("chatSessions");
        }
        onLoad?.Invoke();
    }
    public ChatSession GetSessionById(string id)
    {
        return sessions.Find(c => c.id == id);
    }
}