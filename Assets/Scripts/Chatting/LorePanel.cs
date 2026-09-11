using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LorePanel : Singleton<LorePanel>
{
    [SerializeField]
    private TMP_Dropdown chainSelection;
    private void OnEnable()
    {
        chainSelection.onValueChanged.AddListener(OnChainSelectionChanged);
    }
    private void OnDisable()
    {
        chainSelection.onValueChanged.RemoveListener(OnChainSelectionChanged);
    }
    public void Initialize()
    {
        UpdateChains();
    }
    private void UpdateChains()
    {
        chainSelection.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        bool foundMatch = false;

        for (int i = 0; i < ChainEditor.i.chains.Count; i++)
        {
            Chain chain = ChainEditor.i.chains[i];
            options.Add(new TMP_Dropdown.OptionData(chain.name));

            if (chain.id == ChatManager.i.activeSession.chainId)
            {
                selectedIndex = i;
                foundMatch = true;
            }
        }
        chainSelection.AddOptions(options);
        if (ChainEditor.i.chains.Count > 0)
        {
            chainSelection.SetValueWithoutNotify(selectedIndex);
            if (!foundMatch)
            {
                ChatManager.i.SetActiveChain(ChainEditor.i.chains[selectedIndex].id);
            }
        }
        chainSelection.RefreshShownValue();
    }
    private void OnChainSelectionChanged(int index)
    {
        ChatManager.i.SetActiveChain(GetSelectedChainId());
    }
    public string GetSelectedChainId()
    {
        if (ChainEditor.i.chains.Count == 0) { return "No Chain Created"; }
        return ChainEditor.i.chains[chainSelection.value].id;
    }
}