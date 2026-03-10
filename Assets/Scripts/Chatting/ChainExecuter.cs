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
    public event Action<string> OnChainCompleted;    
    public event Action<string> OnChainFailed;       
    public async Task Execute(Chain chain, List<ChatMessage> chatHistory)
    {
        if (isRunning)
        {
            Debug.LogWarning("Chain already running.");
            return;
        }
        isRunning = true;
        currentStep = 0;
        totalSteps = chain.links.Count;
        string lastResponse = null;
        Debug.Log($"[ChainExecutor] Starting chain '{chain.name}' with {totalSteps} steps.");
        for (int i = 0; i < chain.links.Count; i++)
        {
            ChainLink link = chain.links[i];
            currentStep = i + 1;
            OnStepStarted?.Invoke(currentStep, totalSteps);
            Debug.Log($"[ChainExecutor] Step {currentStep}/{totalSteps}");
            string json = PromptBuilder.Build(chatHistory, link);
            if (json == null)
            {
                string error = $"Failed to build prompt at step {currentStep}";
                Debug.LogError($"[ChainExecutor] {error}");
                OnChainFailed?.Invoke(error);
                isRunning = false;
                return;
            }
            string response = await OpenRouterManager.i.Send(json);
            if (response == null)
            {
                string error = $"API call failed at step {currentStep}";
                Debug.LogError($"[ChainExecutor] {error}");
                OnChainFailed?.Invoke(error);
                isRunning = false;
                return;
            }
            lastResponse = response;

            ChatMessage linkMessage = new ChatMessage
            {
                role = "assistant",
                content = response,
                isLinkOutput = (i != chain.links.Count - 1),
                timestamp = DateTime.Now.ToString("o")
            };
            chatHistory.Add(linkMessage);
            ChatManager.Instance.AddMessage(linkMessage);
            OnStepCompleted?.Invoke(currentStep, response);
            Debug.Log($"[ChainExecutor] Step {currentStep} completed.");
            Debug.Log("LLM-Answer: " + linkMessage.content);
        }
        isRunning = false;
        OnChainCompleted?.Invoke(lastResponse);
        Debug.Log($"[ChainExecutor] Chain '{chain.name}' completed.");
    }
}