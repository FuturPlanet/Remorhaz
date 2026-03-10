using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ApplyPallate : MonoBehaviour
{
    public string pallateName;
    public bool isGlobal;
    public List<Component> color1 = new List<Component>();
    public List<Component> color2 = new List<Component>();
    public List<Component> color3 = new List<Component>();
    public List<Component> color4 = new List<Component>();
    public List<Component> color5 = new List<Component>();
    public UnityEvent onColorUpdate;
    private void OnEnable()
    {
        ColorManager.i.onGlobalPallteChange.AddListener(() => UpdateColors());
        ColorManager.i.onMasterUpdateColors.AddListener(() => UpdateColors());
        UpdateColors();
    }
    private void OnDisable()
    {
        if (ColorManager.i == null) return;
        ColorManager.i.onGlobalPallteChange.RemoveListener(UpdateColors);
        ColorManager.i.onMasterUpdateColors.RemoveListener(UpdateColors);
    }
    [Button("Update Colors")]
    public void UpdateColors()
    {
        ApplyColor(color1, 1);
        ApplyColor(color2, 2);
        ApplyColor(color3, 3);
        ApplyColor(color4, 4);
        ApplyColor(color5, 5);
        onColorUpdate?.Invoke();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var children = this.transform.GetComponentsInChildren<ApplyParentPallate>();
            foreach (ApplyParentPallate child in children)
            {
                child.UpdateColors();
            }
        }
#endif
    }
    private void ApplyColor(List<Component> components, int id)
    {
        Color color;
        if (isGlobal)
        {
            color = ColorManager.i.GetGlobalColor(id);
        }
        else
        {
            color = ColorManager.i.GetColor(pallateName, id);
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