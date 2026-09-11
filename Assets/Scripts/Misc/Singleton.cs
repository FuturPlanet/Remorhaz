using UnityEngine;

//Do not edit this script!
public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool applicationIsQuitting = false;

    public static T i => Instance;
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting) return null;
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                }
            }
            return instance;
        }
    }
    //If you need Awake in your Script use: protected override void OnSingletonAwake()
    private void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            Debug.LogWarning($"Danger! Second Singleton instance of {typeof(T).Name} destroyed at {this.transform.name}.");
            Destroy(this.gameObject);
            return;
        }
        OnSingletonAwake();
    }
    protected virtual void OnSingletonAwake() { }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}