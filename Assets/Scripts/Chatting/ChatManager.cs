using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class ChatManager : Singleton<ChatManager>
{
    [HideInInspector]
    public List<ChatSession> sessions = new List<ChatSession>();
    [HideInInspector]
    public List<ChatSessionCollection> sessionCollections = new List<ChatSessionCollection>();
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
    public void OnEnable()
    {
        Load();
        InvokeRepeating(nameof(Save), 2f, 2f);
    }
    private void OnDisable()
    {
        Save();
    }
    private void Update()
    {
        if(activeCollection != null)
        {
            activeCollection.name = collectionName.text;
            if (activeSession != null)
            {
                activeSession.name = sessionName.text;
                LorePanel.i.Initialize();
                activeSession.chainId = LorePanel.i.GetSelectedChainId();
            }
        }
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
        var inputMessage = ChatView.i.input.text;
        ChatMessage userMessage = new ChatMessage
        {
            role = "user",
            content = inputMessage,
            isLinkOutput = false,
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
            Debug.LogError($"Chain '{activeSession.chainId}' not found.");
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
        Debug.Log("Created a new Collection.");
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
        Debug.Log("Deleted current Collection!");
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
        Debug.Log("Created a new Session.");
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
        Debug.Log("Deleted current Session!");
    }
    public void AddMessage(ChatMessage message)
    {
        if (activeSession == null)
        {
            Debug.LogWarning("No active session.");
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
            Debug.LogWarning("No active session.");
            return;
        }
        ChatMessage message = activeSession.messages.Find(m => m.timestamp == timestamp);
        if (message != null)
        {
            activeSession.messages.Remove(message);
            Save();
            onMessageDeleted?.Invoke();
        }
    }
    public void Save()
    {
        ES3.Save("chatSessions", sessions);
        ES3.Save("chatSessionCollections", sessionCollections);
        onSave?.Invoke();
    }
    public void Load()
    {
        if (ES3.KeyExists("chatSessions"))
        {
            sessions = ES3.Load<List<ChatSession>>("chatSessions");
        }
        if (ES3.KeyExists("chatSessionCollections"))
        {
            sessionCollections = ES3.Load<List<ChatSessionCollection>>("chatSessionCollections");
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
            Debug.LogWarning("No active collection.");
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
        if(activeCollection == null) 
        {
            Debug.LogWarning("Trying to access non-active collection.");
            return sessions; 
        }
        var sessionIds = sessionCollections.Find(c => c.id == activeCollection.id).sessionIds;
        if(sessionIds == null) { return sessions; }
        foreach (var sessionId in sessionIds)
        {
            sessions.Add(GetSessionById(sessionId));
        }
        return sessions;
    }
}