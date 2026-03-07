using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ApplyParentPallate : MonoBehaviour
{
    public int colorId = 1;
    public bool directGlobal = false;
    public List<Component> components = new List<Component>();

    private void OnEnable()
    {
        ColorManager.i.onGlobalPallteChange.AddListener(UpdateColors);
        ColorManager.i.onMasterUpdateColors.AddListener(UpdateColors);
        UpdateColors();
    }
    private void OnDisable()
    {
        ColorManager.i.onGlobalPallteChange.RemoveListener(UpdateColors);
        ColorManager.i.onMasterUpdateColors.RemoveListener(UpdateColors);
    }
    public void UpdateColors()
    {
        Color color;
        ApplyPallate parentPallate = GetComponentInParent<ApplyPallate>();
        if (directGlobal || parentPallate == null)
        {
            color = ColorManager.i.GetGlobalColor(colorId);
        }
        else
        {
            if (parentPallate.isGlobal)
            {
                color = ColorManager.i.GetGlobalColor(colorId);
            }
            else
            {
                color = ColorManager.i.GetColor(parentPallate.pallateName, colorId);
            }
        }
        foreach (var component in components)
        {
            if (component == null) continue;
            switch (component)
            {
                case Camera cam:
                    cam.backgroundColor = color;
                    break;
                case TMP_Text tmpText:
                    tmpText.color = color;
                    break;
                case Graphic g:
                    g.color = color;
                    break;
                case SpriteRenderer sr:
                    sr.color = color;
                    break;
                case Renderer r:
                    r.material.color = color;
                    break;
            }
        }
    }
}