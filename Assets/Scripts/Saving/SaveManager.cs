using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SaveManager : Singleton<SaveManager>
{
    private const string Extension = ".json";

    private readonly Dictionary<string, Dictionary<string, JToken>> _cache = new();
    public void Save<T>(string key, T value, string fileName, string directoryPath = null)
    {
        if (!TryGetFilePath(fileName, directoryPath, out string filePath))
            return;

        var data = GetData(filePath);

        try
        {
            data[key] = JToken.FromObject(value);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (System.Exception e)
        {
            Logger.i.Log($"SaveManager: Failed to save key '{key}' to '{filePath}'. {e.Message}", LogType.Error);
        }
    }
    public T Load<T>(string key, string fileName, string directoryPath = null)
    {
        if (!TryGetFilePath(fileName, directoryPath, out string filePath))
            return default;

        var data = GetData(filePath);

        if (data.TryGetValue(key, out JToken token))
        {
            try
            {
                return token.ToObject<T>();
            }
            catch (System.Exception e)
            {
                Logger.i.Log($"SaveManager: Failed to convert key '{key}' in '{filePath}' to type '{typeof(T).Name}'. {e.Message}", LogType.Error);
                return default;
            }
        }

        return default;
    }
    public void DeleteFile(string fileName, string directoryPath = null)
    {
        if (!TryGetFilePath(fileName, directoryPath, out string filePath))
            return;

        _cache.Remove(filePath);

        if (!File.Exists(filePath))
            return;

        try
        {
            File.Delete(filePath);
        }
        catch (System.Exception e)
        {
            Logger.i.Log($"SaveManager: Failed to delete file '{filePath}'. {e.Message}", LogType.Error);
        }
    }
    public void DeleteKey(string key, string fileName, string directoryPath = null)
    {
        if (!TryGetFilePath(fileName, directoryPath, out string filePath))
            return;

        var data = GetData(filePath);

        if (!data.Remove(key))
            return;

        try
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (System.Exception e)
        {
            Logger.i.Log($"SaveManager: Failed to remove key '{key}' from '{filePath}'. {e.Message}", LogType.Error);
        }
    }
    public bool KeyExists(string key, string fileName, string directoryPath = null)
    {
        if (!TryGetFilePath(fileName, directoryPath, out string filePath))
            return false;

        var data = GetData(filePath);
        return data.ContainsKey(key);
    }
    private bool TryGetFilePath(string fileName, string directoryPath, out string filePath)
    {
        filePath = null;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            Logger.i.Log("SaveManager: fileName is required and cannot be null or empty.", LogType.Error);
            return false;
        }

        directoryPath ??= Application.persistentDataPath;

        try
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }
        catch (System.Exception e)
        {
            Logger.i.Log($"SaveManager: Failed to create directory '{directoryPath}'. {e.Message}", LogType.Error);
            return false;
        }

        filePath = Path.Combine(directoryPath, fileName + Extension);
        return true;
    }
    private Dictionary<string, JToken> GetData(string filePath)
    {
        if (_cache.TryGetValue(filePath, out var data))
            return data;

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                data = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json)
                       ?? new Dictionary<string, JToken>();
            }
            catch (System.Exception e)
            {
                Logger.i.Log($"SaveManager: Failed to read/parse '{filePath}'. {e.Message}", LogType.Error);
                data = new Dictionary<string, JToken>();
            }
        }
        else
        {
            data = new Dictionary<string, JToken>();
        }

        _cache[filePath] = data;
        return data;
    }
}