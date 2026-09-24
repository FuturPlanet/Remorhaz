using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class ModuleSelection : Singleton<ModuleSelection>
{
    [SerializeField]
    private GameObject unitPrefab;
    [SerializeField]
    private Transform group;
    [SerializeField]
    private GameObject visual;
    [SerializeField]
    private ToggleTwoObjects rawToggle;
    [SerializeField]
    private Transform basicParameters;
    [SerializeField]
    private Transform rawParameters;

    // Basic
    private TMP_InputField basicName;
    private TMP_InputField basicID;

    // Raw Input
    private TMP_InputField rawName;
    private TMP_InputField rawID;
    private TMP_InputField jsonInputField;

    private Slider temperature;
    private Slider topP;
    private Slider topK;
    private Slider frequencyPenalty;
    private Slider presencePenalty;
    private Slider repetitionPenalty;
    private Slider minP;

    private Module currentModule;

    private void Awake()
    {
        //Raw
        rawName = rawParameters.Find("ModuleName").GetComponent<TMP_InputField>();
        rawID = rawParameters.Find("modelID").GetComponent<TMP_InputField>();
        jsonInputField = rawParameters.Find("JsonInput").GetComponent<TMP_InputField>();

        //Basic
        basicName = basicParameters.Find("ModuleName").GetComponent<TMP_InputField>();
        basicID = basicParameters.Find("modelID").GetComponent<TMP_InputField>();

        temperature = basicParameters.Find("Temperature/Slider").GetComponent<Slider>();
        topP = basicParameters.Find("Top P/Slider").GetComponent<Slider>();
        topK = basicParameters.Find("Top K/Slider").GetComponent<Slider>();
        frequencyPenalty = basicParameters.Find("Frequency Penalty/Slider").GetComponent<Slider>();
        presencePenalty = basicParameters.Find("Presence Penalty/Slider").GetComponent<Slider>();
        repetitionPenalty = basicParameters.Find("Repetition Penalty/Slider").GetComponent<Slider>();
        minP = basicParameters.Find("Min P/Slider").GetComponent<Slider>();
    }
    private void OnEnable()
    {
        ModuleEditor.Instance.onLoad.AddListener(LoadUnits);
        ModuleEditor.i.OnEnable();
        CloseModule();
        RuntimeManager.i.AddTask(0, true, 3000).AddListener(this, SaveCurrentModule);
    }
    private void OnDisable()
    {
        ModuleEditor.Instance.onLoad.RemoveListener(LoadUnits);
        SaveCurrentModule();
    }
    private void SaveCurrentModule()
    {
        if (currentModule == null) return;
        if (currentModule.isRaw)
        {
            currentModule.name = rawName.text;
            currentModule.model = rawID.text;
            SetParameter(currentModule, "rawJson", jsonInputField.text);
        }
        else
        {
            currentModule.name = basicName.text;
            currentModule.model = basicID.text;
            SetParameter(currentModule, "temperature", temperature.value.ToString());
            SetParameter(currentModule, "top_p", topP.value.ToString());
            SetParameter(currentModule, "top_k", topK.value.ToString());
            SetParameter(currentModule, "frequency_penalty", frequencyPenalty.value.ToString());
            SetParameter(currentModule, "presence_penalty", presencePenalty.value.ToString());
            SetParameter(currentModule, "repetition_penalty", repetitionPenalty.value.ToString());
            SetParameter(currentModule, "min_p", minP.value.ToString());
        }
        ModuleEditor.i.SaveModules();
        LoadUnits();
    }
    private void SetParameter(Module module, string key, string value)
    {
        var param = module.parameters.Find(p => p.key == key);
        if (param == null)
        {
            module.parameters.Add(new ModuleParameter { key = key, value = value });
        }
        else
        {
            param.value = value;
        }
    }
    public void LoadUnits()
    {
        if (DragPanelManager.i.isDragging) return;
        var modules = ModuleEditor.i.modules;
        Transform createNewButton = null;
        foreach (Transform child in group)
        {
            if (child.CompareTag("Unit"))
            {
                createNewButton = child;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
        foreach (var module in modules)
        {
            var moduleObj = Instantiate(unitPrefab, group);
            moduleObj.name = module.name;
            moduleObj.transform.Find("Button/Text (TMP)").GetComponent<TMP_Text>().text = module.name;

            var button = moduleObj.transform.Find("Button").GetComponent<Button>();
            string moduleId = module.id;
            button.onClick.AddListener(() => OpenModule(moduleId));
        }
        createNewButton?.SetAsLastSibling();
    }
    public void DeleteCurrentModule()
    {
        if (currentModule == null) { return; }
        ModuleEditor.i.DeleteModule(currentModule.id);
        CloseModule();
    }
    public void CloseModule()
    {
        SaveCurrentModule();
        visual.SetActive(false);
        currentModule = null;
    }
    public void OpenModule(string id, bool toggle = false)
    {
        SaveCurrentModule();
        Module module = ModuleEditor.i.GetModuleById(id);
        if (module == null) return;
        rawToggle.onToggle.RemoveAllListeners();
        rawToggle.onToggle.AddListener(() => OpenModule(id, true));
        if(toggle)
        {
            module.isRaw = !module.isRaw;
        }
        currentModule = module;
        visual.SetActive(true);
        rawName.text = module.name;
        basicName.text = module.name;
        basicID.text = module.model;
        rawID.text = module.model;
        if (module.isRaw)
        {
            rawParameters.gameObject.SetActive(true);
            basicParameters.gameObject.SetActive(false);

            string json = FormatJson(GetParameter(module, "rawJson") ?? "");
            if (string.IsNullOrEmpty(json))
            {
                json = "{\r\n  \"temperature\": 0.7,\r\n  \"top_p\": 0.9,\r\n  \"frequency_penalty\": 0,\r\n  \"presence_penalty\": 0\r\n}";
            }
            jsonInputField.text = json;
        }
        else
        {
            rawParameters.gameObject.SetActive(false);
            basicParameters.gameObject.SetActive(true);

            temperature.value = GetFloatParameter(module, "temperature", 0.7f);
            topP.value = GetFloatParameter(module, "top_p", 0.9f);
            topK.value = GetFloatParameter(module, "top_k", 0f);
            frequencyPenalty.value = GetFloatParameter(module, "frequency_penalty", 0f);
            presencePenalty.value = GetFloatParameter(module, "presence_penalty", 0f);
            repetitionPenalty.value = GetFloatParameter(module, "repetition_penalty", 0f);
            minP.value = GetFloatParameter(module, "min_p", 0f);
        }
    }
    private string FormatJson(string json)
    {
        try
        {
            var obj = JToken.Parse(json);
            return obj.ToString(Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }
    private string GetParameter(Module module, string key)
    {
        var param = module.parameters.Find(p => p.key == key);
        return param?.value;
    }
    private float GetFloatParameter(Module module, string key, float defaultValue)
    {
        var param = module.parameters.Find(p => p.key == key);

        if (param == null)
        {
            param = new ModuleParameter { key = key, value = defaultValue.ToString() };
            module.parameters.Add(param);
            return defaultValue;
        }

        if (float.TryParse(param.value, out float result))
            return result;

        return defaultValue;
    }
}