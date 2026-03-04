[System.Serializable]
public class ChatMessage
{
    public string role; // "user", "assistant", "system"
    public string content;
    public bool isLinkOutput;
    public string timestamp;
}