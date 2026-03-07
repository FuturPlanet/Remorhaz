using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconApplier : MonoBehaviour
{
    [SerializeField]
    private int iconId;
    [SerializeField]
    private List<Sprite> icons = new List<Sprite>();
    private void Awake()
    {
        this.GetComponent<Image>().sprite = icons[iconId];
    }
}
