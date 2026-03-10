using System.Collections.Generic;

[System.Serializable]
public class ChatSession
{
    public string id;
    public string chainId;
    public string name;
    public int index;
    public List<ChatMessage> messages;
}