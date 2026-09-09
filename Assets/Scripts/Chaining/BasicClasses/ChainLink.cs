[System.Serializable]
public class ChainLink
{
    public string id;
    public int priority;
    public string moduleId;
    public int maxTokens;
    public int messageDepth;
    public int chainDepth;
    public string prompt;

    public bool replayReasoning = false;
}