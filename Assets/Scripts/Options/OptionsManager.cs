using UnityEngine;
using UnityEngine.Events;
public class OptionsManager : Singleton<OptionsManager>
{
    [HideInInspector]
    public GraphicSettings graphicSettings;
    [HideInInspector]
    public AudioSettings audioSettings;
    [HideInInspector]
    public OpenRouterSettings openRouterSettings;
    public UnityEvent onLoad;
    public UnityEvent onSave;
    private void OnEnable()
    {
        Load();
        RuntimeManager.i.AddTask(0, true, 3000).AddListener(this, Save);
    }
    private void OnDisable()
    {
        Save();
    }
    public void Save()
    {
        SaveManager.i.Save("graphicSettings", graphicSettings, "Settings");
        SaveManager.i.Save("audioSettings", audioSettings, "Settings");
        SaveManager.i.Save("openRouterSettings", openRouterSettings, "Settings");
        onSave?.Invoke();
    }
    public void Load()
    {
        if (SaveManager.i.KeyExists("graphicSettings", "Settings"))
        {
            graphicSettings = SaveManager.i.Load<GraphicSettings>("graphicSettings", "Settings");
        }
        else
        {
            graphicSettings = new GraphicSettings();
        }
        if (SaveManager.i.KeyExists("audioSettings", "Settings"))
        {
            audioSettings = SaveManager.i.Load<AudioSettings>("audioSettings", "Settings");
        }
        else
        {
            audioSettings = new AudioSettings
            {
                masterVolume = 0.5f,
                musicVolume = 0.5f,
                sfxVolume = 0.5f
            };
        }
        if (SaveManager.i.KeyExists("openRouterSettings", "Settings"))
        {
            openRouterSettings = SaveManager.i.Load<OpenRouterSettings>("openRouterSettings", "Settings");
        }
        else
        {
            openRouterSettings = new OpenRouterSettings();
        }
        onLoad?.Invoke();
    }
    public void DeleteAllCollections()
    {
        if (SaveManager.i.KeyExists("chatSessionCollections", "Settings"))
        {
            SaveManager.i.DeleteFile("chatSessionCollections", "Settings");
        }
    }
}