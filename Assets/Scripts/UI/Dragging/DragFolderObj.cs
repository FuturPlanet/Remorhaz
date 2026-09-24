using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragFolderObj : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private GameObject backButtonPrefab;
    [SerializeField]
    private GameObject headerPrefab;
    private Image bound;
    [SerializeField]
    private Image highlight;

    public string folderId { get; private set; }
    public string treeId { get; private set; }
    private DragAble dragAble;
    private class ChromeInfo
    {
        public GameObject header;
        public GameObject backButton;
        public string treeId;
    }
    private static readonly Dictionary<Transform, ChromeInfo> chromeByContainer = new Dictionary<Transform, ChromeInfo>();
    private static bool navigationHooked;
    private Button openButton;
    private void Awake()
    {
        bound = GetComponent<Image>();
    }
    private void OnEnable()
    {
        openButton = GetComponent<Button>();
        if (openButton != null)
        {
            openButton.onClick.AddListener(Open);
        }
    }
    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(Open);
        }
    }
    private void Start()
    {
        CacheIds();
    }
    private void Open()
    {
        DragPanelManager.i.EnterFolder(folderId);
    }
    private void CacheIds()
    {
        dragAble = GetComponent<DragAble>();
        if (dragAble == null || string.IsNullOrEmpty(dragAble.folderData)) return;
        folderId = DragPanelManager.i.GetObjId(dragAble.folderData);
        treeId = DragPanelManager.i.GetTreeId(dragAble.folderData);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!DragPanelManager.i.isDragging) return;
        if (DragPanelManager.i.originalObject == transform) return; // can't drop a folder onto itself
        if (highlight != null) highlight.gameObject.SetActive(true);
        DragPanelManager.i.CurrentDropTarget = this;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight != null) highlight.gameObject.SetActive(false);
        if (DragPanelManager.i.CurrentDropTarget == this)
        {
            DragPanelManager.i.CurrentDropTarget = null;
        }
    }
    public void OpenFolder()
    {
        if (string.IsNullOrEmpty(folderId))
        {
            CacheIds();
            if (string.IsNullOrEmpty(folderId)) return;
        }

        Transform container = transform.parent;
        if (container == null) return;

        EnsureChrome(container);
        DragPanelManager.i.EnterFolder(folderId);
    }
    private void EnsureChrome(Transform container)
    {
        if (!chromeByContainer.TryGetValue(container, out ChromeInfo chrome))
        {
            chrome = new ChromeInfo();
            chromeByContainer[container] = chrome;
        }
        chrome.treeId = treeId;

        if (chrome.header == null && headerPrefab != null)
        {
            chrome.header = Instantiate(headerPrefab, container);
        }

        if (chrome.backButton == null && backButtonPrefab != null)
        {
            chrome.backButton = Instantiate(backButtonPrefab, container);
            Button backButton;
            if (chrome.backButton.TryGetComponent(out Button _backButton))
            {
                backButton = _backButton;
            }
            else
            {
                backButton = chrome.backButton.GetComponentInChildren<Button>(true);
            }
            backButton.name = "BackButton";
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => DragPanelManager.i.ExitFolder(container));
        }
        if (!navigationHooked)
        {
            DragPanelManager.i.onNavigate.AddListener(RefreshAllChromeDeferred);
            navigationHooked = true;
        }
        RefreshAllChromeDeferred();
    }
    private static void RefreshAllChromeDeferred()
    {
        DragPanelManager.i.StartCoroutine(RefreshAllChromeRoutine());
    }
    private static IEnumerator RefreshAllChromeRoutine()
    {
        yield return new WaitForEndOfFrame();
        foreach (KeyValuePair<Transform, ChromeInfo> entry in chromeByContainer)
        {
            RefreshChrome(entry.Key, entry.Value);
        }
    }
    private static void RefreshChrome(Transform container, ChromeInfo chrome)
    {
        if (container == null) return;

        if (chrome.header != null)
        {
            chrome.header.transform.SetAsFirstSibling();
            SetLabel(chrome.header.transform, DragPanelManager.i.GetFolderName(DragPanelManager.i.GetCurrentFolderId(chrome.treeId)));
        }

        if (chrome.backButton != null)
        {
            bool atRoot = DragPanelManager.i.IsAtRoot(chrome.treeId);
            chrome.backButton.SetActive(!atRoot);
            chrome.backButton.transform.SetAsLastSibling();
        }
    }
    private static void SetLabel(Transform target, string text)
    {
        if (target.TryGetComponent(out Text label))
        {
            label.text = text;
            return;
        }
        Text nested = target.GetComponentInChildren<Text>(true);
        if (nested != null)
        {
            nested.text = text;
        }
    }
}