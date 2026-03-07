using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class ChainEditor : Singleton<ChainEditor>
{
    [HideInInspector]
    public List<Chain> chains = new List<Chain>();
    public UnityEvent onLoad;
    public UnityEvent onSave;

    public void OnEnable()
    {
        LoadChains();
    }
    public void CreateNewChain()
    {
        Debug.Log("Creating a new Chain.");
        Chain newChain = new Chain
        {
            id = System.Guid.NewGuid().ToString(),
            name = "New Chain",
            links = new List<ChainLink>()
        };
        chains.Add(newChain);
        SaveChains();
        LoadChains();
    }
    public void AddLinkToChain(Chain chain)
    {
        ChainLink newLink = new ChainLink
        {
            id = System.Guid.NewGuid().ToString(),
            priority = chain.links.Count,
            maxTokens = 4096,
            messageDepth = 0,
            chainDepth = 1,
            prompt = "Write your prompt here."
        };

        chain.links.Add(newLink);
        SaveChains();
    }
    public void RemoveLinkFromChain(Chain chain, string linkId)
    {
        ChainLink linkToRemove = chain.links.Find(l => l.id == linkId);
        if (linkToRemove != null)
        {
            chain.links.Remove(linkToRemove);
            for (int i = 0; i < chain.links.Count; i++)
            {
                chain.links[i].priority = i;
            }
            SaveChains();
        }
    }
    public void DeleteChain(string id)
    {
        foreach (var chain in chains)
        {
            if (chain.id == id)
            {
                chains.Remove(chain);
                break;
            }
        }
        SaveChains();
    }
    public void SaveChains()
    {
        ES3.Save("chains", chains);
        onSave?.Invoke();
    }
    private void LoadChains()
    {
        if (ES3.KeyExists("chains"))
        {
            chains = ES3.Load<List<Chain>>("chains");
        }
        onLoad?.Invoke();
    }
    public Chain GetChainById(string id)
    {
        return chains.Find(c => c.id == id);
    }
}