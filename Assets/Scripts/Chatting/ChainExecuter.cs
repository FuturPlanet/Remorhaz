using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChainExecutor : Singleton<ChainExecutor>
{
    public bool isRunning { get; private set; }
    public int currentStep { get; private set; }
    public int totalSteps { get; private set; }
    public float progress => totalSteps > 0 ? (float)currentStep / totalSteps : 0f;

    public event Action<int, int> OnStepStarted;
    public event Action<int, string> OnStepCompleted;
    public event Action<int, string> OnStepReasoning;
    public event Action<string> OnChainCompleted;
    public event Action<string> OnChainFailed;

    public async Task Execute(Chain chain, List<ChatMessage> chatHistory)
    {
        if (isRunning)
        {
            Debug.LogWarning("[ChainExecutor] Chain already running.");
            return;
        }
        if (chain == null || chain.links == null || chain.links.Count == 0)
        {
            Fail("Chain is empty or null.");
            return;
        }
        isRunning = true;
        currentStep = 0;
        totalSteps = chain.links.Count;
        string lastResponse = null;
        Debug.Log($"[ChainExecutor] Starting chain '{chain.name}' with {totalSteps} steps.");
        try
        {
            for (int i = 0; i < chain.links.Count; i++)
            {
                ChainLink link = chain.links[i];
                currentStep = i + 1;
                OnStepStarted?.Invoke(currentStep, totalSteps);
                Debug.Log($"[ChainExecutor] Step {currentStep}/{totalSteps}");
                string json = PromptBuilder.Build(chatHistory, link);
                if (json == null)
                {
                    Fail($"Failed to build prompt at step {currentStep}");
                    return;
                }
                LlmResponse response = await OpenRouterManager.i.Send(json);
                if (response == null || !response.ok)
                {
                    Fail($"API call failed at step {currentStep}: " + (response?.error ?? "null response"));
                    return;
                }
                lastResponse = response.content;
                if (!string.IsNullOrEmpty(response.reasoning))
                {
                    Debug.Log($"[ChainExecutor] Thinking ({response.reasoningTokens} tok):\n{response.reasoning}");
                    OnStepReasoning?.Invoke(currentStep, response.reasoning);
                }
                ChatMessage linkMessage = new ChatMessage
                {
                    role = "assistant",
                    content = response.content,
                    reasoning = response.reasoning,
                    reasoningDetailsJson = response.reasoningDetailsJson,
                    isLinkOutput = (i != chain.links.Count - 1),
                    timestamp = DateTime.UtcNow.ToString("o")
                };
                chatHistory.Add(linkMessage);
                ChatManager.Instance.AddMessage(linkMessage);

                OnStepCompleted?.Invoke(currentStep, response.content);
                Debug.Log($"[ChainExecutor] Step {currentStep} completed " + $"({response.promptTokens} in / {response.completionTokens} out).");
                Debug.Log("[ChainExecutor] LLM-Answer: " + linkMessage.content);
            }
            OnChainCompleted?.Invoke(lastResponse);
            Debug.Log($"[ChainExecutor] Chain '{chain.name}' completed.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OnChainFailed?.Invoke($"Unhandled exception at step {currentStep}: {e.Message}");
        }
        finally
        {
            isRunning = false;
        }
    }
    private void Fail(string error)
    {
        Debug.LogError($"[ChainExecutor] {error}");
        OnChainFailed?.Invoke(error);
    }
}