using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.CoreUtils;

public class SessionCollectionSelection : Singleton<SessionCollectionSelection>
{
    [SerializeField]
    private GameObject unitPrefab;
    [SerializeField]
    private Transform group;
    private void OnEnable()
    {
        ChatManager.i.onLoad.AddListener(LoadUnits);
        InvokeRepeating(nameof(LoadUnits), 2f, 2f);
    }
    private void OnDisable()
    {
        ChatManager.i.onLoad.RemoveListener(LoadUnits);
    }
    public void LoadUnits()
    {
        var collections = ChatManager.i.sessionCollections;
        if (collections == null) { return; }
        Transform createNewButton = null;
        foreach (Transform child in group)
        {
            if (child.tag == "Unit")
            {
                createNewButton = child;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        foreach (ChatSessionCollection collection in collections)
        {
            var collectionObj = Instantiate(unitPrefab, group);
            collectionObj.name = string.IsNullOrEmpty(collection.name) ? "New Session" : collection.name;
            var button = collectionObj.transform.Find("Button").GetComponent<Button>();
            collectionObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = collection.name;
            button.onClick.AddListener(() => OpenCollection(collection.id));
        }
        createNewButton.SetAsLastSibling();
    }
    public void OpenCollection(string id)
    {
        ChatSessionCollection collection = ChatManager.i.GetCollectionById(id);
        if (collection == null) 
        {
            Debug.LogWarning($"Corrupted Collection ID: {id}");
            return; 
        }
        ChatManager.i.SetActiveCollection(id);
        ChatView.i.OpenSessionPanel();
    }
}