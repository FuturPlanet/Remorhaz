using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class OpenRouterManager : Singleton<OpenRouterManager>
{
    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private const string apiUrl = "https://openrouter.ai/api/v1/chat/completions";

    public async Task<LlmResponse> Send(string jsonBody)
    {
        Debug.Log($"[OpenRouterManager] Sending JSON:\n{jsonBody}");

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {OptionsManager.i.openRouterSettings.apikey}");
        request.Headers.TryAddWithoutValidation("X-Title", "YourAppName");

        string responseJson;
        try
        {
            var response = await httpClient.SendAsync(request);
            responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"[OpenRouterManager] HTTP {(int)response.StatusCode}: {responseJson}");
                return new LlmResponse { ok = false, error = responseJson, rawJson = responseJson };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[OpenRouterManager] Transport error: {e.Message}");
            return new LlmResponse { ok = false, error = e.Message };
        }

        return Parse(responseJson);
    }
    private LlmResponse Parse(string responseJson)
    {
        var result = new LlmResponse { rawJson = responseJson };

        JObject root;
        try { root = JObject.Parse(responseJson); }
        catch (Exception e)
        {
            result.error = "Malformed JSON: " + e.Message;
            Debug.LogError($"[OpenRouterManager] {result.error}\n{responseJson}");
            return result;
        }
        var err = root["error"];
        if (err != null && err.Type != JTokenType.Null)
        {
            result.error = err.ToString(Formatting.None);
            Debug.LogError($"[OpenRouterManager] API error: {result.error}");
            return result;
        }

        var choice = root["choices"]?[0];
        var message = choice?["message"];
        if (message == null)
        {
            result.error = "No choices/message in response.";
            Debug.LogError($"[OpenRouterManager] {result.error}\n{responseJson}");
            return result;
        }

        result.finishReason = choice["finish_reason"]?.ToString();
        result.content = message["content"]?.Type == JTokenType.String
                                ? message["content"].ToString() : "";
        result.reasoning = message["reasoning"]?.Type == JTokenType.String
                                ? message["reasoning"].ToString() : null;

        var details = message["reasoning_details"] as JArray;
        if (details != null && details.Count > 0)
            result.reasoningDetailsJson = details.ToString(Formatting.None);

        var usage = root["usage"];
        if (usage != null)
        {
            result.promptTokens = usage["prompt_tokens"]?.Value<int>() ?? 0;
            result.completionTokens = usage["completion_tokens"]?.Value<int>() ?? 0;
            result.reasoningTokens = usage["completion_tokens_details"]?["reasoning_tokens"]
                                        ?.Value<int>() ?? 0;
        }
        if (string.IsNullOrEmpty(result.content))
        {
            result.error = result.finishReason == "length"
                ? $"Empty content, finish_reason=length. Reasoning used {result.reasoningTokens} tokens — raise max_tokens or drop effort to \"high\"."
                : $"Empty content, finish_reason={result.finishReason}.";
            Debug.LogError($"[OpenRouterManager] {result.error}");
            return result;
        }
        result.ok = true;
        return result;
    }
}