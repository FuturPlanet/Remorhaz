using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class OpenRouterManager : Singleton<OpenRouterManager>
{
    private static readonly HttpClient httpClient = new HttpClient();
    private string apiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private string apiKey = "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";

    public async Task<string> Send(string jsonBody)
    {
        Debug.Log($"[OpenRouterManager] Sending JSON:\n{jsonBody}");

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await httpClient.PostAsync(apiUrl, content);
        string responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"OpenRouter error: {responseJson}");
            return null;
        }

        return ParseResponse(responseJson);
    }
    private string ParseResponse(string responseJson)
    {
        int contentStart = responseJson.IndexOf("\"content\":\"") + 11;
        if (contentStart < 11) return null;

        int contentEnd = responseJson.IndexOf("\"", contentStart);
        while (contentEnd > 0 && responseJson[contentEnd - 1] == '\\')
        {
            contentEnd = responseJson.IndexOf("\"", contentEnd + 1);
        }

        if (contentEnd < 0) return null;

        string content = responseJson.Substring(contentStart, contentEnd - contentStart);
        content = content.Replace("\\n", "\n")
                         .Replace("\\\"", "\"")
                         .Replace("\\\\", "\\");

        return content;
    }
}