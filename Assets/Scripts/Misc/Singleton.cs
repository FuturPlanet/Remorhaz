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
            if (applicationIsQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                if (instance == null)
                {
                    GameObject gameObject = new GameObject(typeof(T).Name);
                    instance = gameObject.AddComponent<T>();
                }
            }
            return instance;
        }
    }
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            //DontDestroyOnLoad(this.gameObject);
            //Debug.Log($"Official instance of {typeof(T).Name} is at {this.transform.name}.");
        }
        else
        {
            if (instance != this)
            {
                Destroy(this.gameObject);
                Debug.LogWarning($"Danger! Second Singleton instance of {typeof(T).Name} destroyed at {this.transform.name}.");
            }
        }
    }
    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}