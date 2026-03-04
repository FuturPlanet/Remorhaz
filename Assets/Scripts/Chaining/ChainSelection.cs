using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ChainSelection : Singleton<ChainSelection>
{
    [SerializeField]
    private GameObject unitPrefab;
    [SerializeField]
    private GameObject windowPrefab;
    [SerializeField]
    private GameObject arrowPrefab;
    [SerializeField]
    private Transform group;
    [SerializeField]
    private GameObject visual;
    [SerializeField]
    private Transform visualGroup;
    [SerializeField]
    private TMP_InputField chainName;
    private Chain currentChain;

    private void OnEnable()
    {
        ChainEditor.Instance.onLoad.AddListener(LoadUnits);
        ChainEditor.i.OnEnable();
        CloseChain();
        InvokeRepeating(nameof(SaveCurrentChain), 2f, 2f);
    }
    private void OnDisable()
    {
        ChainEditor.Instance.onLoad.RemoveListener(LoadUnits);
        SaveCurrentChain();
    }
    private void SaveCurrentChain()
    {
        if (currentChain == null) { return; }
        currentChain.name = chainName.text;
        int index = 0;
        foreach (Transform child in visualGroup)
        {
            if (child.tag == "Unit") { continue; }
            ChainQuickAccess access = child.GetComponent<ChainQuickAccess>();
            if (access == null || index >= currentChain.links.Count) { continue; }
            ChainLink link = currentChain.links[index];
            link.prompt = access.prompt.text;
            if (int.TryParse(access.maxTokens.text, out int maxTokens))
            {
                link.maxTokens = maxTokens;
            }
            if (int.TryParse(access.messageDepth.text, out int messageDepth))
            {
                link.messageDepth = messageDepth;
            }
            if (int.TryParse(access.chainDepth.text, out int chainDepth))
            {
                link.chainDepth = chainDepth;
            }
            if (access.moduleSelection.interactable &&
                ModuleEditor.i.modules != null &&
                ModuleEditor.i.modules.Count > 0)
            {
                int selectedIndex = access.moduleSelection.value;
                if (selectedIndex >= 0 && selectedIndex < ModuleEditor.i.modules.Count)
                {
                    Module selectedModule = ModuleEditor.i.modules[selectedIndex];
                    link.moduleEditorId = selectedModule.id;
                }
            }
            index++;
        }

        ChainEditor.i.SaveChains();
        LoadUnits();
    }
    public void LoadUnits()
    {
        var chains = ChainEditor.i.chains;
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
        foreach (var chain in chains)
        {
            var chainObj = Instantiate(unitPrefab, group);
            chainObj.name = chain.name;
            var button = chainObj.transform.Find("Button").GetComponent<Button>();
            chainObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = chain.name;
            string chainId = chain.id;
            button.onClick.AddListener(() => OpenChain(chainId));
        }
        createNewButton.SetAsLastSibling();
    }
    public void AddLinkToCurrentChain()
    {
        ChainEditor.i.AddLinkToChain(currentChain);
        OpenChain(currentChain.id);
    }
    public void RemoveLinkFromCurrentChain(string linkId)
    {
        ChainEditor.i.RemoveLinkFromChain(currentChain, linkId);
        OpenChain(currentChain.id);
    }
    public void DeleteCurrentChain()
    {
        if (currentChain == null) { return; }
        ChainEditor.i.DeleteChain(currentChain.id);
        CloseChain();
    }
    public void CloseChain()
    {
        SaveCurrentChain();
        visual.SetActive(false);
        currentChain = null;
    }
    public void OpenChain(string id)
    {
        Chain chain = ChainEditor.i.GetChainById(id);
        if (chain == null) { return; }
        SaveCurrentChain();

        foreach (Transform child in visualGroup)
        {
            if(child.tag != "Unit")
            {
                Destroy(child.gameObject);
            }
        }

        currentChain = chain;
        chainName.text = chain.name;
        visual.SetActive(true);

        foreach (var link in chain.links)
        {
            Instantiate(arrowPrefab, visualGroup);
            var linkObj = Instantiate(windowPrefab, visualGroup);
            var deleteButton = linkObj.transform.Find("Delete").GetComponent<Button>();
            deleteButton.onClick.AddListener(() => RemoveLinkFromCurrentChain(link.id));

            ChainQuickAccess access = linkObj.GetComponent<ChainQuickAccess>();
            access.prompt.text = link.prompt;
            access.maxTokens.text = link.maxTokens.ToString();
            access.messageDepth.text = link.messageDepth.ToString();
            access.chainDepth.text = link.chainDepth.ToString();

            access.moduleSelection.ClearOptions();

            if (ModuleEditor.i.modules == null || ModuleEditor.i.modules.Count == 0)
            {
                access.moduleSelection.AddOptions(new List<TMP_Dropdown.OptionData>{ new TMP_Dropdown.OptionData("No modules available") });
                access.moduleSelection.interactable = false;
                continue;
            }

            access.moduleSelection.interactable = true;

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            int selectedIndex = 0;
            for (int i = 0; i < ModuleEditor.i.modules.Count; i++)
            {
                Module module = ModuleEditor.i.modules[i];
                options.Add(new TMP_Dropdown.OptionData(module.name));

                if (module.id == link.moduleEditorId)
                {
                    selectedIndex = i;
                }
            }
            access.moduleSelection.AddOptions(options);

            access.moduleSelection.value = selectedIndex;
            access.moduleSelection.RefreshShownValue();
        }
    }
}