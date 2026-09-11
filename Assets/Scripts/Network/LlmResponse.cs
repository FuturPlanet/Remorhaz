public class LlmResponse
{
    public bool ok;
    public string error;

    public string content;
    public string reasoning;
    public string finishReason;

    public int promptTokens;
    public int completionTokens;
    public int reasoningTokens;

    public string rawJson;
}