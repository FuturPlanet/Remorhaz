using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PromptBuilder
{
    public static string Build(List<ChatMessage> chatHistory, ChainLink link)
    {
        Module module = ModuleEditor.i.modules.Find(m => m.id == link.moduleId);
        if (module == null)
        {
            Debug.LogError($"Module with id {link.moduleId} not found.");
            return null;
        }
        List<ChatMessage> selectedMessages = SelectMessages(chatHistory, link);
        string baseJson = GetBaseJson(module);
        string finalJson = MergeJson(baseJson, module.model, link.prompt, selectedMessages);
        return finalJson;
    }
    private static List<ChatMessage> SelectMessages(List<ChatMessage> chatHistory, ChainLink link)
    {
        var linkOutputs = chatHistory.Where(m => m.isLinkOutput).ToList();
        var regularMessages = chatHistory.Where(m => !m.isLinkOutput).ToList();
        var selectedLinkOutputs = linkOutputs.Skip(Mathf.Max(0, linkOutputs.Count - link.chainDepth)).ToList();
        var selectedRegular = regularMessages.Skip(Mathf.Max(0, regularMessages.Count - link.messageDepth)).ToList();
        var combined = new List<ChatMessage>();
        combined.AddRange(selectedLinkOutputs);
        combined.AddRange(selectedRegular);
        combined = combined.OrderBy(m => m.timestamp).ToList();
        return combined;
    }
    private static string GetBaseJson(Module module)
    {
        if (module.isRaw)
        {
            string rawJson = module.parameters.Find(p => p.key == "rawJson")?.value;
            if (!string.IsNullOrEmpty(rawJson)) return rawJson;
        }
        var paramDict = new Dictionary<string, object>();
        foreach (var param in module.parameters)
        {
            if (float.TryParse(param.value, out float floatVal))
                paramDict[param.key] = floatVal;
            else if (int.TryParse(param.value, out int intVal))
                paramDict[param.key] = intVal;
            else if (bool.TryParse(param.value, out bool boolVal))
                paramDict[param.key] = boolVal;
            else
                paramDict[param.key] = param.value;
        }
        return DictToJson(paramDict);
    }
    private static string MergeJson(string baseJson, string model, string systemPrompt, List<ChatMessage> messages)
    {
        string messagesArray = "[";
        messagesArray += $"{{\"role\":\"system\",\"content\":\"{EscapeJson(systemPrompt)}\"}}";
        foreach (var msg in messages)
        {
            messagesArray += $",{{\"role\":\"{msg.role}\",\"content\":\"{EscapeJson(msg.content)}\"}}";
        }
        messagesArray += "]";
        baseJson = baseJson.Trim();
        if (string.IsNullOrEmpty(baseJson) || baseJson == "{}")
        {
            return $"{{\"model\":\"{model}\",\"messages\":{messagesArray}}}";
        }
        baseJson = baseJson.TrimEnd('}');
        return $"{baseJson},\"model\":\"{model}\",\"messages\":{messagesArray}}}";
    }
    private static string DictToJson(Dictionary<string, object> dict)
    {
        string json = "{";
        int count = 0;
        foreach (var kvp in dict)
        {
            if (kvp.Value is string strVal)
                json += $"\"{kvp.Key}\":\"{EscapeJson(strVal)}\"";
            else if (kvp.Value is float f)
                json += $"\"{kvp.Key}\":{f.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            else if (kvp.Value is double d)
                json += $"\"{kvp.Key}\":{d.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            else
                json += $"\"{kvp.Key}\":{kvp.Value.ToString().ToLower()}";

            if (++count < dict.Count) json += ",";
        }
        json += "}";
        return json;
    }
    private static string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }
}