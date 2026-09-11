[System.Serializable]
public class ChatMessage
{
    public string role;                 // "user", "assistant", "system"
    public string content;
    public string reasoning;
    public bool isLink;
    public string timestamp;
}