using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class ChatManager : Singleton<ChatManager>
{
    private List<ChatSession> sessions = new List<ChatSession>();
    private List<ChatSessionCollection> sessionCollections = new List<ChatSessionCollection>();
    [SerializeField]
    private TMP_InputField sessionName;
    [SerializeField]
    private TMP_InputField collectionName;
    public ChatSession activeSession { get; private set; }
    public ChatSessionCollection activeCollection { get; private set; }

    public UnityEvent onLoad;
    public UnityEvent onSave;
    public UnityEvent onMessageAdded;
    public UnityEvent onMessageDeleted;
    public UnityEvent refresh;

    protected override void OnSingletonAwake()
    {
        sessionName.onValueChanged.AddListener(OnSessionNameChanged);
        collectionName.onValueChanged.AddListener(OnCollectionNameChanged);
    }
    public void OnEnable()
    {
        Load();
        DragPanelManager.i.onReorder.AddListener(Save);
        RuntimeManager.i.AddTask(0, true, 3000).AddListener(this, Save);
    }
    private void OnDisable()
    {
        if(DragPanelManager.i != null)
        {
            DragPanelManager.i.onReorder.RemoveListener(Save);
        }
        Save();
    }
    private void OnCollectionNameChanged(string value)
    {
        if (activeCollection != null)
        {
            activeCollection.name = value;
        }
    }
    private void OnSessionNameChanged(string value)
    {
        if (activeSession != null)
        {
            activeSession.name = value;
        }
    }
    public void SetActiveChain(string chainId)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("[ChatManager] No active session.");
            return;
        }
        activeSession.chainId = chainId;
        Save();
    }
    public void SetActiveCollection(string collectionId)
    {
        activeCollection = sessionCollections.Find(s => s.id == collectionId);
        collectionName.text = activeCollection.name;
    }
    public void SetActiveSession(string sessionId)
    {
        activeSession = sessions.Find(s => s.id == sessionId);
        sessionName.text = activeSession.name;
        LorePanel.i.Initialize();
    }
    public async void Send()
    {
        if (activeSession == null)
        {
            Debug.LogWarning("[ChatManager] No active session.");
            return;
        }
        if (ChainExecutor.Instance.isRunning)
        {
            Debug.LogWarning("[ChatManager] Chain already running.");
            return;
        }
        var inputMessage = ChatView.i.input.text;
        ChatMessage userMessage = new ChatMessage
        {
            role = "user",
            content = inputMessage,
            isLink = false,
            timestamp = System.DateTime.Now.ToString("o")
        };
        if (!string.IsNullOrEmpty(inputMessage))
        {
            AddMessage(userMessage);
        }
        ChatView.i.input.text = "";
        Chain chain = ChainEditor.Instance.GetChainById(activeSession.chainId);
        if (chain == null)
        {
            Debug.LogError($"[ChatManager] Chain '{activeSession.chainId}' not found.");
            return;
        }
        await ChainExecutor.Instance.Execute(chain, activeSession.messages);
    }
    public void CreateNewCollection()
    {
        ChatSessionCollection newCollection = new ChatSessionCollection
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Collection",
            sessionIds = new List<string>()
        };
        sessionCollections.Add(newCollection);
        Save();
        Load();
        Debug.Log("[ChatManager] Created a new Collection.");
    }
    public void DeleteCurrentCollection()
    {
        if (activeCollection != null)
        {
            ChatView.i.OpenCollectionPanel();
            sessionCollections.Remove(activeCollection);
            activeCollection = null;
            activeSession = null;
            Save();
        }
        Debug.Log("[ChatManager] Deleted current Collection!");
    }
    public void CreateNewSession()
    {
        string newId = System.Guid.NewGuid().ToString();
        ChatSession newSession = new ChatSession
        {
            id = newId,
            name = "New Session",
            messages = new List<ChatMessage>()
        };
        sessions.Add(newSession);
        sessionCollections.Find(s => s.id == activeCollection.id).sessionIds.Add(newId);
        Save();
        Load();
        Debug.Log("[ChatManager] Created a new Session.");
    }
    public void DeleteCurrentSession()
    {
        if (activeSession != null)
        {
            ChatView.i.OpenSessionPanel();
            sessions.Remove(activeSession);
            activeCollection.sessionIds.Remove(activeSession.id);
            activeSession = null;
            Save();
        }
        Debug.Log("[ChatManager] Deleted current Session!");
    }
    public void AddMessage(ChatMessage message)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("[ChatManager] No active session.");
            return;
        }
        activeSession.messages.Add(message);
        Save();
        onMessageAdded?.Invoke();
    }
    public void DeleteMessage(string timestamp)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("[ChatManager] No active session.");
            return;
        }
        List<ChatMessage> messages = activeSession.messages;
        int index = messages.FindIndex(m => m.timestamp == timestamp);
        if (index == -1)
        {
            return;
        }
        ChatMessage target = messages[index];
        List<ChatMessage> toRemove = new List<ChatMessage> { target };
        if (!target.isLink)
        {
            int i = index - 1;
            while (i >= 0 && messages[i].isLink)
            {
                toRemove.Add(messages[i]);
                i--;
            }
        }
        foreach (ChatMessage m in toRemove)
        {
            messages.Remove(m);
        }
        Save();
        onMessageDeleted?.Invoke();
    }
    public void Save()
    {
        SaveManager.i.Save("chatSessions", sessions, "Chat");
        SaveManager.i.Save("chatSessionCollections", sessionCollections, "Chat");
        onSave?.Invoke();
    }
    public void Load()
    {
        if (SaveManager.i.KeyExists("chatSessions", "Chat"))
        {
            sessions = SaveManager.i.Load<List<ChatSession>>("chatSessions", "Chat");
        }
        if (SaveManager.i.KeyExists("chatSessionCollections", "Chat"))
        {
            sessionCollections = SaveManager.i.Load<List<ChatSessionCollection>>("chatSessionCollections", "Chat");
        }
        onLoad?.Invoke();
    }
    public void SetCollectionIndexes(List<string> orderedCollectionIds)
    {
        for (int i = 0; i < orderedCollectionIds.Count; i++)
        {
            var collection = GetCollectionById(orderedCollectionIds[i]);
            if (collection != null)
            {
                collection.index = i;
            }
        }
        Save();
    }
    public void SetSessionIndexes(List<string> orderedSessionIds)
    {
        if (activeCollection == null)
        {
            Logger.i.Log(this, "No active collection.", LogType.Warning);
            return;
        }
        for (int i = 0; i < orderedSessionIds.Count; i++)
        {
            var session = GetSessionById(orderedSessionIds[i]);
            if (session != null)
            {
                session.index = i;
            }
        }
        activeCollection.sessionIds = new List<string>(orderedSessionIds);
        Save();
    }
    public ChatSession GetSessionById(string id)
    {
        return sessions.Find(c => c.id == id);
    }
    public ChatSessionCollection GetCollectionById(string id)
    {
        return sessionCollections.Find(c => c.id == id);
    }
    public List<ChatSession> GetSessionsFromCurrentCollection()
    {
        List<ChatSession> sessions = new List<ChatSession>();
        if (activeCollection == null)
        {
            Debug.LogWarning("[ChatManager] Trying to access non-active collection.");
            return sessions;
        }
        var sessionIds = sessionCollections.Find(c => c.id == activeCollection.id).sessionIds;
        if (sessionIds == null) { return sessions; }
        foreach (var sessionId in sessionIds)
        {
            sessions.Add(GetSessionById(sessionId));
        }
        return sessions;
    }
}