using System;
using System.Collections.Generic;
using UnityEngine;

public class RunTimeTask
{
    public string id;
    public float lastExecuted;
    public string finishInfo;
    public Action callback;
    public float delay;
    public bool repeating;
    public float repeat;
    public bool executeOnQuit;
    public bool surviveOwnerDisable;
    public UnityEngine.Object owner;
    public bool hasOwner;
    internal bool skipOwnerDedup;
    public RunTimeTask AddListener(Action listener)
    {
        callback += listener;
        return this;
    }
    public RunTimeTask AddListener(UnityEngine.Object owner, Action listener)
    {
        this.owner = owner;
        hasOwner = true;
        callback += listener;
        RuntimeManager.i.EnforceSingleTaskPerOwner(this, owner);
        return this;
    }
    public RunTimeTask RemoveListener(Action listener)
    {
        callback -= listener;
        return this;
    }
    public RunTimeTask SetExecuteOnQuit(bool value)
    {
        executeOnQuit = value;
        return this;
    }
    public override string ToString()
    {
        string ownerInfo = hasOwner ? (owner != null ? owner.name : "destroyed") : "none";
        return $"[RunTimeTask id={id}, delay={delay}ms, repeating={repeating}, repeat={repeat}ms, " + $"executeOnQuit={executeOnQuit}, surviveOwnerDisable={surviveOwnerDisable}, owner={ownerInfo}, " + $"lastExecuted={lastExecuted:F2}, finishInfo=\"{finishInfo}\"]";
    }
}

public readonly struct ArchivedTaskRecord
{
    public readonly string id;
    public readonly float lastExecuted;
    public readonly string finishInfo;
    public ArchivedTaskRecord(RunTimeTask source)
    {
        id = source.id;
        lastExecuted = source.lastExecuted;
        finishInfo = source.finishInfo;
    }
    public override string ToString()
    {
        return $"[ArchivedTaskRecord id={id}, lastExecuted={lastExecuted:F2}, finishInfo=\"{finishInfo}\"]";
    }
}

public class RuntimeManager : Singleton<RuntimeManager>
{
    public event Action<string> onTaskExecuted;

    private bool isQuitting;
    private Dictionary<float, List<RunTimeTask>> activeTasks = new();
    private List<RunTimeTask> finishedTasks = new();
    private Dictionary<string, ArchivedTaskRecord> archivedTasks = new();

    private const string infoRunning = "Is Running.";
    private const string infoStop = "Forced to stop.";
    private const string infoExecuted = "Got executed, no repeat.";
    private const string infoExecutedOnDisable = "Forced to execute early on disable.";
    private const string infoOwnerDisabled = "Owner disabled, task stopped.";
    private const string infoOwnerDestroyed = "Owner destroyed, task stopped.";
    private const string infoSupersededByNewTask = "Owner registered a new task, previous one superseded and stopped.";

    private const int finishedTasksArchiveThreshold = 10_000;

    private void OnEnable()
    {
        Application.quitting += OnQuitting;
    }
    private void OnDisable()
    {
        Application.quitting -= OnQuitting;

        if (activeTasks.Count == 0) return;

        if (!isQuitting)
        {
            Logger.i.Log(this, "RuntimeManager was disabled while tasks were still active.", LogType.Warning);
        }

        var snapshot = new List<RunTimeTask>();
        foreach (var bucket in activeTasks.Values)
        {
            snapshot.AddRange(bucket);
        }
        activeTasks.Clear();

        foreach (var task in snapshot)
        {
            if (task.executeOnQuit)
            {
                ForceExecuteAndFinish(task);
            }
            else
            {
                ForceStop(task);
            }
        }
    }
    private void Update()
    {
        if (activeTasks.Count == 0) return;

        PruneDeadTasks();

        float now = Time.unscaledTime;
        var keys = new List<float>(activeTasks.Keys);

        foreach (var key in keys)
        {
            if (now >= key)
            {
                if (!activeTasks.TryGetValue(key, out var bucket)) continue;

                var tasks = new List<RunTimeTask>(bucket);
                foreach (var task in tasks)
                {
                    ExecuteTask(key, task);
                }
            }
        }
    }
    public RunTimeTask AddTask(out string id, float delay = 0, bool repeating = false, float repeat = 2000f)
    {
        id = Guid.NewGuid().ToString();
        RunTimeTask newTask = new RunTimeTask
        {
            id = id,
            finishInfo = infoRunning,
            delay = delay,
            repeating = repeating,
            repeat = repeat
        };
        AddActiveTask(CalculateExecutionTime(delay), newTask);
        Logger.i.Log(this, $"Added Task {newTask}.");
        return newTask;
    }
    public RunTimeTask AddTask(float delay = 0, bool repeating = false, float repeat = 2000f)
    {
        return AddTask(out _, delay, repeating, repeat);
    }
    public RunTimeTask ForceAddTask(out string id, float delay = 0, bool repeating = false, float repeat = 2000f)
    {
        id = Guid.NewGuid().ToString();
        RunTimeTask newTask = new RunTimeTask
        {
            id = id,
            finishInfo = infoRunning,
            delay = delay,
            repeating = repeating,
            repeat = repeat,
            skipOwnerDedup = true
        };
        AddActiveTask(CalculateExecutionTime(delay), newTask);
        Logger.i.Log(this, $"Added Task (forced, owner dedup disabled) {newTask}.");
        return newTask;
    }
    public RunTimeTask ForceAddTask(float delay = 0, bool repeating = false, float repeat = 2000f)
    {
        return ForceAddTask(out _, delay, repeating, repeat);
    }
    public void KeepTaskAliveOnDisable(string id)
    {
        foreach (var bucket in activeTasks.Values)
        {
            foreach (var task in bucket)
            {
                if (task.id == id)
                {
                    task.surviveOwnerDisable = true;
                    return;
                }
            }
        }

        Logger.i.Log(this, "Task is not active; cannot mark it to survive disable.", LogType.Warning);
    }
    public void KeepTaskAliveOnDisable(RunTimeTask task)
    {
        if (task == null) return;
        task.surviveOwnerDisable = true;
    }
    public void StopTask(string id)
    {
        float? foundKey = null;
        RunTimeTask foundTask = null;

        foreach (var kvp in activeTasks)
        {
            foreach (var task in kvp.Value)
            {
                if (task.id == id)
                {
                    foundKey = kvp.Key;
                    foundTask = task;
                    break;
                }
            }
            if (foundTask != null) break;
        }
        if (foundTask == null)
        {
            if (TryReadTask(id, out _))
            {
                Logger.i.Log(this, "Task is already stopped.", LogType.Warning);
            }
            return;
        }
        var bucket = activeTasks[foundKey.Value];
        bucket.Remove(foundTask);
        if (bucket.Count == 0)
        {
            activeTasks.Remove(foundKey.Value);
        }

        foundTask.finishInfo = infoStop;
        AddFinishedTask(foundTask);
    }
    public RunTimeTask ReadTask(string id)
    {
        if (TryReadTask(id, out var task))
        {
            return task;
        }
        Logger.i.Log(this, "Task does not exist.", LogType.Warning);
        return null;
    }
    public bool TryReadTask(string id, out RunTimeTask task)
    {
        foreach (var collection in activeTasks.Values)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i].id == id)
                {
                    task = collection[i];
                    return true;
                }
            }
        }
        for (int i = 0; i < finishedTasks.Count; i++)
        {
            if (finishedTasks[i].id == id)
            {
                task = finishedTasks[i];
                return true;
            }
        }
        if (archivedTasks.TryGetValue(id, out var record))
        {
            task = new RunTimeTask
            {
                id = record.id,
                lastExecuted = record.lastExecuted,
                finishInfo = record.finishInfo,
                callback = null
            };
            return true;
        }

        task = null;
        return false;
    }
    internal void EnforceSingleTaskPerOwner(RunTimeTask newTask, UnityEngine.Object owner)
    {
        if (newTask.skipOwnerDedup) return;
        if (owner == null) return;

        RunTimeTask duplicate = null;
        float duplicateKey = 0f;

        foreach (var kvp in activeTasks)
        {
            foreach (var task in kvp.Value)
            {
                if (task == newTask) continue;
                if (task.hasOwner && task.owner == owner)
                {
                    duplicate = task;
                    duplicateKey = kvp.Key;
                    break;
                }
            }
            if (duplicate != null) break;
        }

        if (duplicate == null) return;

        Logger.i.Log(this, $"Owner \"{owner.name}\" already had an active task ({duplicate.id}) when registering task " + $"{newTask.id}. Stopping the previous task automatically. If this is intentional (multiple " + $"concurrent tasks for the same owner), use ForceAddTask instead.", LogType.Warning);

        var bucket = activeTasks[duplicateKey];
        bucket.Remove(duplicate);
        if (bucket.Count == 0) activeTasks.Remove(duplicateKey);

        duplicate.finishInfo = infoSupersededByNewTask;
        AddFinishedTask(duplicate);
    }
    private void ExecuteTask(float key, RunTimeTask task)
    {
        bool taskFound = false;
        if (activeTasks.TryGetValue(key, out var list))
        {
            taskFound = list.Remove(task);
            if (list.Count == 0)
                activeTasks.Remove(key);
        }
        if (!taskFound)
        {
            return;
        }
        task.lastExecuted = Time.unscaledTime;
        if (!task.repeating)
        {
            task.finishInfo = infoExecuted;
            AddFinishedTask(task);
        }
        else
        {
            AddActiveTask(CalculateExecutionTime(task.repeat), task);
        }

        InvokeTaskCallback(task);
        InvokeTaskExecuted(task.id);
        Logger.i.Log(this, $"Executed Task {($"{task}".Replace("RunTimeTask ", "") is var s && s.Length > 25 ? s[..22] + "..." : s)} by {task?.owner}.");
    }
    private void ForceExecuteAndFinish(RunTimeTask task)
    {
        task.lastExecuted = Time.unscaledTime;

        if (TryGetDeathReason(task, out var deathReason))
        {
            task.finishInfo = deathReason;
            AddFinishedTask(task);
            return;
        }

        task.finishInfo = infoExecutedOnDisable;
        InvokeTaskCallback(task);
        InvokeTaskExecuted(task.id);
        AddFinishedTask(task);
    }
    private void ForceStop(RunTimeTask task)
    {
        task.finishInfo = infoStop;
        AddFinishedTask(task);
    }
    private void PruneDeadTasks()
    {
        var toKill = new List<(float key, RunTimeTask task, string reason)>();

        foreach (var kvp in activeTasks)
        {
            foreach (var task in kvp.Value)
            {
                if (TryGetDeathReason(task, out var reason))
                {
                    toKill.Add((kvp.Key, task, reason));
                }
            }
        }
        foreach (var (key, task, reason) in toKill)
        {
            if (activeTasks.TryGetValue(key, out var bucket))
            {
                bucket.Remove(task);
                if (bucket.Count == 0) activeTasks.Remove(key);
            }
            task.finishInfo = reason;
            AddFinishedTask(task);
        }
    }
    private static bool TryGetDeathReason(RunTimeTask task, out string reason)
    {
        reason = null;

        if (task.hasOwner)
        {
            if (task.owner == null)
            {
                reason = infoOwnerDestroyed;
                return true;
            }
            if (!task.surviveOwnerDisable && task.owner is Behaviour ownerBehaviour && !ownerBehaviour.isActiveAndEnabled)
            {
                reason = infoOwnerDisabled;
                return true;
            }
            return false;
        }

        if (task.callback == null) return false;

        foreach (var d in task.callback.GetInvocationList())
        {
            if (d.Target is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                {
                    reason = infoOwnerDestroyed;
                    return true;
                }
                if (!task.surviveOwnerDisable && unityObj is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                {
                    reason = infoOwnerDisabled;
                    return true;
                }
            }
        }
        return false;
    }
    private void InvokeTaskCallback(RunTimeTask task)
    {
        try
        {
            task.callback?.Invoke();
        }
        catch (Exception e)
        {
            Logger.i.Log(this, $"Task {task.id} threw an exception during execution:\n{e}", LogType.Error);
        }
    }
    private void InvokeTaskExecuted(string id)
    {
        PruneDeadListeners(ref onTaskExecuted);
        try
        {
            onTaskExecuted?.Invoke(id);
        }
        catch (Exception e)
        {
            Logger.i.Log(this, $"onTaskExecuted listener threw an exception:\n{e}", LogType.Error);
        }
    }
    private static void PruneDeadListeners(ref Action<string> action)
    {
        if (action == null) return;
        foreach (var d in action.GetInvocationList())
        {
            if (!IsListenerAlive(d)) action -= (Action<string>)d;
        }
    }
    private static bool IsListenerAlive(Delegate d)
    {
        var target = d.Target;
        if (target == null) return true;
        if (target is UnityEngine.Object unityObj) return unityObj != null;
        return true;
    }
    private void AddActiveTask(float key, RunTimeTask task)
    {
        if (!activeTasks.TryGetValue(key, out var tasks))
        {
            tasks = new List<RunTimeTask>();
            activeTasks[key] = tasks;
        }
        tasks.Add(task);
    }
    private void AddFinishedTask(RunTimeTask task)
    {
        finishedTasks.Add(task);

        if (finishedTasks.Count >= finishedTasksArchiveThreshold)
        {
            ArchiveFinishedTasks();
        }
    }
    private void ArchiveFinishedTasks()
    {
        foreach (var task in finishedTasks)
        {
            archivedTasks[task.id] = new ArchivedTaskRecord(task);
        }
        int archivedCount = finishedTasks.Count;
        finishedTasks.Clear();
        Logger.i.Log(this, $"Archived {archivedCount} finished tasks into compact storage " + $"(total archived: {archivedTasks.Count}).");
    }
    private float CalculateExecutionTime(float milliseconds)
    {
        return Time.unscaledTime + milliseconds / 1000f;
    }
    private void OnQuitting()
    {
        isQuitting = true;
    }
}