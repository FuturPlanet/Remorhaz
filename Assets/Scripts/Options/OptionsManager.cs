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
        InvokeRepeating(nameof(Save), 1, 1);
    }
    private void OnDisable()
    {
        Save();
    }
    public void Save()
    {
        ES3.Save("graphicSettings", graphicSettings);
        ES3.Save("audioSettings", audioSettings);
        ES3.Save("openRouterSettings", openRouterSettings);
        onSave?.Invoke();
    }
    public void Load()
    {
        if (ES3.KeyExists("graphicSettings"))
        {
            graphicSettings = ES3.Load<GraphicSettings>("graphicSettings");
        }
        else
        {
            graphicSettings = new GraphicSettings();
        }
        if (ES3.KeyExists("audioSettings"))
        {
            audioSettings = ES3.Load<AudioSettings>("audioSettings");
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
        if (ES3.KeyExists("openRouterSettings"))
        {
            openRouterSettings = ES3.Load<OpenRouterSettings>("openRouterSettings");
        }
        else
        {
            openRouterSettings = new OpenRouterSettings();
        }
        onLoad?.Invoke();
    }
    public void DeleteAllCollections()
    {
        if (ES3.KeyExists("chatSessionCollections"))
        {
            ES3.DeleteDirectory("chatSessionCollections");
        }
    }
}