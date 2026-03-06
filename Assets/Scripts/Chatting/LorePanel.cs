using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LorePanel : Singleton<LorePanel>
{
    [SerializeField]
    private TMP_Dropdown chainSelection;
    public void Initialize()
    {
        UpdateChains();
    }
    private void UpdateChains()
    {
        chainSelection.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        int selectedIndex = 0;
        for (int i = 0; i < ChainEditor.i.chains.Count; i++)
        {
            Chain chain = ChainEditor.i.chains[i];
            options.Add(new TMP_Dropdown.OptionData(chain.name));

            if (chain.id == ChatManager.i.activeSession.chainId)
            {
                selectedIndex = i;
            }
        }
        chainSelection.AddOptions(options);
        chainSelection.value = selectedIndex;
        chainSelection.RefreshShownValue();
    }
    public string GetSelectedChainId()
    {
        return ChainEditor.i.chains[chainSelection.value].id;
    }
}