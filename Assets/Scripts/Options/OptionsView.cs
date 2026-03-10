using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsView : Singleton<OptionsView>
{
    [SerializeField]
    private Slider maxVolSlider;
    [SerializeField]
    private Slider musicVolSlider;
    [SerializeField]
    private Slider sfxVolSlider;
    [SerializeField]
    private TMP_InputField apikey;
    private void OnEnable()
    {
        maxVolSlider.onValueChanged.AddListener(UpdateMaxVolume);
        musicVolSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxVolSlider.onValueChanged.AddListener(UpdateSfxVolume);
        apikey.onValueChanged.AddListener(UpdateApiKey);
        InvokeRepeating(nameof(Refresh), 0, 1);
    }
    private void OnDisable()
    {
        maxVolSlider.onValueChanged.RemoveListener(UpdateMaxVolume);
        musicVolSlider.onValueChanged.RemoveListener(UpdateMusicVolume);
        sfxVolSlider.onValueChanged.RemoveListener(UpdateSfxVolume);
        apikey.onValueChanged.RemoveListener(UpdateApiKey);
    }
    private void Refresh()
    {
        AudioSettings audioSettings = OptionsManager.i.audioSettings;
        OpenRouterSettings openRouterSettings = OptionsManager.i.openRouterSettings;

        maxVolSlider.value = audioSettings.masterVolume;
        musicVolSlider.value = audioSettings.musicVolume;
        sfxVolSlider.value = audioSettings.sfxVolume;

        apikey.text = openRouterSettings.apikey;
    }
    private void UpdateMaxVolume(float value)
    {
        OptionsManager.i.audioSettings.masterVolume = value;
    }
    private void UpdateMusicVolume(float value)
    {
        OptionsManager.i.audioSettings.musicVolume = value;
    }
    private void UpdateSfxVolume(float value)
    {
        OptionsManager.i.audioSettings.sfxVolume = value;
    }
    private void UpdateApiKey(string value)
    {
        OptionsManager.i.openRouterSettings.apikey = value;
    }
}