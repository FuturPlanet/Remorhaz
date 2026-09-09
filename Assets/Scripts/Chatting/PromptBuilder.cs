using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        JObject body = GetBaseJson(module);
        body["model"] = module.model;

        var messages = new JArray();
        if (!string.IsNullOrWhiteSpace(link.prompt))
            messages.Add(new JObject { ["role"] = "system", ["content"] = link.prompt });

        foreach (var m in SelectMessages(chatHistory, link))
            messages.Add(ToApiMessage(m, link.replayReasoning));

        body["messages"] = messages;

        if (link.maxTokens > 0) body["max_tokens"] = link.maxTokens;

        return body.ToString(Formatting.None);
    }
    private static JObject ToApiMessage(ChatMessage m, bool replayReasoning)
    {
        var o = new JObject { ["role"] = m.role, ["content"] = m.content ?? "" };

        if (replayReasoning && m.role == "assistant" && !string.IsNullOrEmpty(m.reasoningDetailsJson))
        {
            try { o["reasoning_details"] = JArray.Parse(m.reasoningDetailsJson); }
            catch { /* stale/garbage cache: just omit */ }
        }
        return o;
    }
    private static List<ChatMessage> SelectMessages(List<ChatMessage> chatHistory, ChainLink link)
    {
        var linkOutputs = chatHistory.Where(m => m.isLinkOutput).ToList();
        var regularMessages = chatHistory.Where(m => !m.isLinkOutput).ToList();

        var selected = new List<ChatMessage>();
        selected.AddRange(linkOutputs.Skip(Mathf.Max(0, linkOutputs.Count - link.chainDepth)));
        selected.AddRange(regularMessages.Skip(Mathf.Max(0, regularMessages.Count - link.messageDepth)));

        return selected.OrderBy(m => m.timestamp, System.StringComparer.Ordinal).ToList();
    }
    private static JObject GetBaseJson(Module module)
    {
        if (module.isRaw)
        {
            string raw = module.parameters.Find(p => p.key == "rawJson")?.value;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try { return JObject.Parse(raw); }
                catch (JsonReaderException e)
                {
                    Debug.LogError($"[PromptBuilder] Invalid rawJson on module '{module.id}': {e.Message}");
                    return new JObject();
                }
            }
        }

        var body = new JObject();
        foreach (var p in module.parameters)
        {
            if (p.key == "rawJson") continue;
            body[p.key] = ParseValue(p.value);
        }
        return body;
    }
    private static JToken ParseValue(string v)
    {
        if (string.IsNullOrEmpty(v)) return JValue.CreateNull();
        string t = v.Trim();

        if (t.StartsWith("{") || t.StartsWith("["))
        {
            try { return JToken.Parse(t); } catch { /* treat as string */ }
        }
        if (bool.TryParse(t, out bool b)) return new JValue(b);
        if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i)) return new JValue(i);
        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return new JValue(d);
        return new JValue(v);
    }
}