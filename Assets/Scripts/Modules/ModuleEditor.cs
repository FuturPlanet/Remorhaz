using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ModuleEditor : Singleton<ModuleEditor>
{
    [HideInInspector]
    public List<Module> modules = new List<Module>();
    public UnityEvent onLoad;
    public UnityEvent onSave;
    public void OnEnable()
    {
        LoadModules();
    }
    public void CreateNewModule()
    {
        Debug.Log("Creating a new Module.");
        Module newModule = new Module
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Module",
            parameters = new List<ModuleParameter>()
        };
        modules.Add(newModule);
        SaveModules();
        LoadModules();
    }
    public void DeleteModule(string id)
    {
        foreach (var module in modules)
        {
            if (module.id == id)
            {
                modules.Remove(module);
                break;
            }
        }
        SaveModules();
    }
    public void SaveModules()
    {
        ES3.Save("modules", modules);
        onSave?.Invoke();
    }
    private void LoadModules()
    {
        if (ES3.KeyExists("modules"))
        {
            modules = ES3.Load<List<Module>>("modules");
        }
        onLoad?.Invoke();
    }
    public Module GetModuleById(string id)
    {
        return modules.Find(m => m.id == id);
    }
}