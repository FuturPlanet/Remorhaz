using System.Collections.Generic;

[System.Serializable]
public class ChatSession
{
    public string id;
    public string chainId;
    public List<ChatMessage> messages;
}