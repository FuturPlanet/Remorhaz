using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct ColorPallate
{
    public string name;
    public Color color1;
    public Color color2;
    public Color color3;
    public Color color4;
    public Color color5;
}
public class ColorManager : Singleton<ColorManager>
{
    [SerializeField]
    private string gloablPallte = "Remorhaz";
    public UnityEvent onGlobalPallteChange;
    public UnityEvent onMasterUpdateColors;
    public List<ColorPallate> pallates = new List<ColorPallate>();

#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        EditorApplication.update += WaitForSceneReady;
    }
    private static void WaitForSceneReady()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            EditorApplication.update -= WaitForSceneReady;

            var mgr = Object.FindFirstObjectByType<ColorManager>();
            if (mgr != null && mgr.pallates != null && mgr.pallates.Count > 0)
            {
                mgr.MasterUpdateAllColors();
            }
        }
    }
#endif
    [Button("Update All Colors")]
    public void MasterUpdateAllColors()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var allApplyPallate = FindObjectsByType<ApplyPallate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (ApplyPallate obj in allApplyPallate)
            {
                obj.UpdateColors();
            }
        }
#endif
        onMasterUpdateColors?.Invoke();
    }
    public void ChangeGlobalColor(string pallateName)
    {
        gloablPallte = pallateName;
        onGlobalPallteChange?.Invoke();
    }
    public Color GetGlobalColor(int colorId)
    {
        return GetColor(gloablPallte, colorId);
    }
    public Color GetColor(string pallateName, int colorId)
    {
        foreach (ColorPallate pallate in pallates)
        {
            if (pallate.name == pallateName)
            {
                switch (colorId)
                {
                    case 1: return pallate.color1;
                    case 2: return pallate.color2;
                    case 3: return pallate.color3;
                    case 4: return pallate.color4;
                    case 5: return pallate.color5;
                    default:
                        Debug.LogWarning($"ColorManager: Invalid colorId {colorId}. Must be 1-5.");
                        return Color.white;
                }
            }
        }
        Debug.LogWarning($"ColorManager: Palette '{pallateName}' not found.");
        return Color.white;
    }
}