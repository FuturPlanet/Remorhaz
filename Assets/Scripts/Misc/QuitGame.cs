using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
        ChainEditor.i.SaveChains();
        ModuleEditor.i.SaveModules();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}