using System.Collections.Generic;
using UnityEngine;
public enum LogStandard { Invoke }
[System.Serializable]
public struct LogCandidate
{
    public Component origin;
    public bool untrackError;
    public bool untrackWarning;
    public bool untrackLog;
}
public class Logger : Singleton<Logger>
{
    [Header("Mute all of Logger")]
    public bool muteError = false;
    public bool muteWarning = false;
    public bool muteLog = false;
    private bool muteAll => muteLog && muteWarning && muteError;
    [Header("Special Tracking")]
    public List<LogCandidate> special = new();

    private const string noOrigin = "???";
    private const string noLog = "...";
    private const string invokeString = "Invoked.";
    private void LogInConsole(string origin, string log, LogType type = LogType.Log)
    {
        if (muteAll) return;
        if (IsSpecialMute(origin)) return;
        switch (type)
        {
            case LogType.Error:
                if (muteError) return;
                Debug.LogError($"[{origin}] {log}");
                break;
            case LogType.Warning:
                if (muteWarning) return;
                Debug.LogWarning($"[{origin}] {log}");
                break;
            case LogType.Log:
                if (muteLog) return;
                Debug.Log($"[{origin}] {log}");
                break;
            default:
                Debug.LogError("[Logger] Wtffff? Error in the Logger? Inception!");
                break;
        }
    }
    public void Log<T>(T origin, LogStandard standard) where T : class
    {
        switch (standard)
        {
            case LogStandard.Invoke:
                LogInConsole(typeof(T).Name, invokeString);
                break;
            default:
                break;
        }
    }
    public void Log(string log, LogType type = LogType.Log)
    {
        LogInConsole(noOrigin, log, FilteredType(type));
    }
    public void Log(string origin, string log, LogType type = LogType.Log)
    {
        LogInConsole(origin, log, FilteredType(type));
    }
    public void Log<T>(T origin, string log, LogType type = LogType.Log) where T : class
    {
        LogInConsole(typeof(T).Name, log, FilteredType(type));
    }
    public void LogOriginOnly(string origin, LogType type = LogType.Log)
    {
        LogInConsole(origin, noLog, FilteredType(type));
    }
    public void LogOriginOnly<T>(T origin, LogType type = LogType.Log) where T : class
    {
        LogInConsole(typeof(T).Name, noLog, FilteredType(type));
    }
    private LogType FilteredType(LogType type)
    {
        if (type == LogType.Warning || type == LogType.Error)
        {
            return type;
        }
        return LogType.Log;
    }
    private bool IsSpecialMute(string origin, LogType type = LogType.Log)
    {
        foreach (var candidate in special)
        {
            if (candidate.origin != null && candidate.origin.GetType().Name == origin)
            {
                switch (FilteredType(type))
                {
                    case LogType.Error:
                        if (candidate.untrackError) return true;
                        break;
                    case LogType.Warning:
                        if (candidate.untrackWarning) return true;
                        break;
                    case LogType.Log:
                        if (candidate.untrackLog) return true;
                        break;
                }
            }
        }
        return false;
    }
}