using System.Collections.Generic;
using UnityEngine;

public class MainWindow : MonoBehaviour
{
    public Transform mainwindow;
    public bool allChildrenAreWindows;
    public List<Transform> windows;

    private void OnEnable()
    {
        if (allChildrenAreWindows)
        {
            foreach (Transform child in mainwindow.parent)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            foreach (Transform child in windows)
            {
                child.gameObject.SetActive(false);
            }
        }
        mainwindow.gameObject.SetActive(true);
    }
}
