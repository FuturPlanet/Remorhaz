using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public struct Focus
{
    public GameObject[] toTurnOff;
    public GameObject[] toTurnOn;
}

public class FocusManager : Singleton<FocusManager>
{
    private Dictionary<string, Focus> focuses = new Dictionary<string, Focus>();
    public string Add(GameObject off, GameObject on, string key = "")
    {
        return Add(new[] { off }, new[] { on }, key);
    }
    public string Add(List<GameObject> off, List<GameObject> on, string key = "")
    {
        return Add(off.ToArray(), on.ToArray(), key);
    }
    public string Add(GameObject[] off, GameObject[] on, string key = "")
    {
        if (string.IsNullOrEmpty(key))
        {
            key = System.Guid.NewGuid().ToString();
        }
        if(on == null)
        {
            on = System.Array.Empty<GameObject>();
        }
        if (off == null)
        {
            off = System.Array.Empty<GameObject>();
        }
        Focus newFocus = new Focus
        {
            toTurnOff = off,
            toTurnOn = on
        };
        focuses[key] = newFocus;
        return key;
    }
    public void Remove(string key)
    {
        focuses.Remove(key);
    }
    public void FocusOn(string key)
    {
        FocusOn(key, false);
    }
    public void FocusOn(string key, bool reverse)
    {
        if (!focuses.TryGetValue(key, out Focus focus))
        {
            Logger.i.Log(this, $"Focus key '{key}' not found!", LogType.Error);
            return;
        }
        foreach (var off in focus.toTurnOff)
        {
            off.SetActive(reverse);
        }
        foreach (var on in focus.toTurnOn)
        {
            on.SetActive(!reverse);
        }
        Logger.i.Log(this, $"Focusing on: {key}! Off: [{string.Join(", ", focus.toTurnOff.Select(g => g.name))}] | On: [{string.Join(", ", focus.toTurnOn.Select(g => g.name))}]");
    }
}