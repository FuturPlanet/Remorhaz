using System.Collections.Generic;

[System.Serializable]
public class Module
{
    public string id;
    public string name;
    public string model;
    public bool isRaw;
    public List<ModuleParameter> parameters;
}