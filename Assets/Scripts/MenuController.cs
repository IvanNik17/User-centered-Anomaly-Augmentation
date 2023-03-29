using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class MenuController : MonoBehaviour
{
    [Header("Stuff for designers")]
    [SerializeField]
    public Action onRoadTransformUpdateEvent;

    [SerializeField]
    List<AnomalyOption> anomalyOptions = new List<AnomalyOption>();

    [SerializeField]
    List<TrafficSetting> trafficSettings = new List<TrafficSetting>();

    [SerializeField]
    LightingSetting lightingSettings = new LightingSetting();

    [SerializeField]
    ExportSetting exportSettings = new ExportSetting();

    [SerializeField]
    RoadTransformSetting roadTransformSettings = new RoadTransformSetting();

    [Header("Stuff to make it work")]

    [SerializeField]
    ViewportHandler viewportHandler;

    [SerializeField]
    PointController pointController;

    [SerializeField]
    VisualTreeAsset anomolyController;

    [SerializeField]
    VisualTreeAsset trafficSettingController;

    [SerializeField]
    VisualTreeAsset lightingTab;


    private UIDocument UIDoc;
    private VisualElement tabMenuElement;
    private List<SettingTabButton> tabButtons = new List<SettingTabButton>();
    private List<TabElement> tabElements = new List<TabElement>();

    void Start()
    {
        UIDoc = GetComponent<UIDocument>();
        tabMenuElement = UIDoc.rootVisualElement.Q<VisualElement>("settings-window");
        VisualElement tabs = UIDoc.rootVisualElement.Q<VisualElement>("tabs");

        tabButtons.Add(new SettingTabButton(tabs.Q<Button>("tab-anomalies"), SettingTabButton.TabType.Anomalies));
        tabButtons[0].button.RegisterCallback<MouseUpEvent>(x => SwitchSettingTab(tabButtons[0].buttonTab));

        tabButtons.Add(new SettingTabButton(tabs.Q<Button>("tab-traffic"), SettingTabButton.TabType.Traffic));
        tabButtons[1].button.RegisterCallback<MouseUpEvent>(x => SwitchSettingTab(tabButtons[1].buttonTab));

        tabButtons.Add(new SettingTabButton(tabs.Q<Button>("tab-lighting"), SettingTabButton.TabType.Light));
        tabButtons[2].button.RegisterCallback<MouseUpEvent>(x => SwitchSettingTab(tabButtons[2].buttonTab));

        CreateTabs();

        SwitchSettingTab(SettingTabButton.TabType.Anomalies);

        UIDoc.rootVisualElement.Q<Button>("bt-add-footage").RegisterCallback<MouseUpEvent>(x => viewportHandler.AddFootage(x.currentTarget as Button));
        UIDoc.rootVisualElement.Q<Button>("bt-draw-foreground").RegisterCallback<MouseUpEvent>(x => pointController.AddMarking());

        SetupExportUI();
        SetupTransformMenu();
    }

    //
    //Transform menu functions
    //

    void SetupTransformMenu()
    {
        NumberField posXField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-pos-x"));
        NumberField posYField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-pos-y"));
        NumberField posZField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-pos-z"));

        NumberField rotXField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-rot-x"));
        NumberField rotYField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-rot-y"));
        NumberField rotZField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-rot-z"));

        NumberField scaleField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-scale"), false);

        VectorFieldController vectorFieldPosition = new VectorFieldController(posXField, posYField, posZField, "Position");
        VectorFieldController vectorFieldRotation = new VectorFieldController(rotXField, rotYField, rotZField, "Rotation");

        vectorFieldPosition.onVectorUpdateEvent += UpdateRoadTransform;
        vectorFieldRotation.onVectorUpdateEvent += UpdateRoadTransform;
        scaleField.onValueUpdateEvent += UpdateRoadScale;
    }

    void UpdateRoadScale(NumberField field)
    {
        roadTransformSettings.scale = field.value;
        UpdateRoadTransform();
    }

    void UpdateRoadTransform(Vector3 value = new Vector3(), string vectorName = "")
    {
        switch (vectorName)
        {
            case "Position":
                roadTransformSettings.position = value;
                break;
            case "Rotation":
                roadTransformSettings.rotation = value;
                break;
        }
        try
        {
            onRoadTransformUpdateEvent.Invoke();
        }
        catch(Exception e)
        {
            Debug.LogWarning("No event tied to road transform update: \n" + e);
        }
        
    }

    //
    //     Export UI functions
    //

    void SetupExportUI()
    {
        NumberField.instance = this;
        NumberField lengthField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-length"), false);
        NumberField videoAmountField = new NumberField(UIDoc.rootVisualElement.Q<TextField>("tf-amount"), false);
        Toggle mixAnomalyToggle = UIDoc.rootVisualElement.Q<Toggle>("tg-mix-anomalies");
        RadioButtonGroup rbgOutType = UIDoc.rootVisualElement.Q<RadioButtonGroup>("rbg-output-type");
        Button exportButton = UIDoc.rootVisualElement.Q<Button>("bt-export");

        lengthField.onValueUpdateEvent += UpdateLengthValue;
        videoAmountField.onValueUpdateEvent += UpdateAmountValue;
        mixAnomalyToggle.RegisterValueChangedCallback(x => UpdateAnomalyMix(x.currentTarget as Toggle));
        rbgOutType.RegisterValueChangedCallback(x => UpdateOutputType(x.currentTarget as RadioButtonGroup));

    }

    void UpdateOutputType(RadioButtonGroup rbg)
    {
        switch (rbg.value)
        {
            case 0:
                exportSettings.outputType = ExportOutputType.ImageSequence;
                break;
            case 1:
                exportSettings.outputType = ExportOutputType.VideoFile;
                break;
        }
    }

    void UpdateAnomalyMix(Toggle toggle)
    {
        exportSettings.mixAnomalies = toggle.value;
    }

    void UpdateLengthValue(NumberField numberField)
    {
        exportSettings.videoLength = (int)numberField.value;
    }

    void UpdateAmountValue(NumberField numberField)
    {
        exportSettings.videoAmount = (int)numberField.value;
    }

    //
    //    Tab functions
    //

    void SwitchSettingTab(SettingTabButton.TabType tab)
    {
        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].DisplayIfType(tab);
        }

        for (int i = 0; i < tabElements.Count; i++)
        {
            tabElements[i].DisplayIfType(tab);
        }
    }

    void CreateTabs()
    {
        //Creating anomaly tab
        ListView anomalyList = new ListView();
        for (int i = 0; i < anomalyOptions.Count; i++)
        {
            //if you are marco, good luck lmao this is a lost cause to understand lmao
            VisualElement anomaly = anomolyController.Instantiate();
            anomaly.Q<Label>("l-anomaly-name").text = anomalyOptions[i].name;
            Slider anomalySlider = anomaly.Q<Slider>("anomaly-slider");
            anomalySlider.bindingPath = i.ToString();
            anomalySlider.value = anomalyOptions[i].value;
            anomalySlider.RegisterValueChangedCallback(x => UpdateAnomalyValue(x.currentTarget as Slider));
            Toggle anomalyToggle = anomaly.Q<Toggle>("anomaly-toggle");
            anomalyToggle.bindingPath = i.ToString();
            anomalyToggle.value = anomalyOptions[i].active;
            anomalyToggle.RegisterValueChangedCallback(x => UpdateAnomalyValue(x.currentTarget as Toggle));
            anomalyList.hierarchy.Add(anomaly);
        }
        tabElements.Add(new TabElement(anomalyList, SettingTabButton.TabType.Anomalies));
        tabMenuElement.Add(anomalyList);


        //Creating traffic tab
        ListView trafficSettingList = new ListView();
        for (int i = 0; i < trafficSettings.Count; i++)
        {
            VisualElement setting = trafficSettingController.Instantiate();
            Slider trafficSlider = setting.Q<Slider>("traffic-slider");
            trafficSlider.label = trafficSettings[i].name;
            trafficSlider.bindingPath = i.ToString();
            trafficSlider.value = trafficSettings[i].value;
            trafficSlider.RegisterValueChangedCallback(x => UpdateTrafficValue(x.currentTarget as Slider));
            trafficSettingList.hierarchy.Add(setting);
        }
        tabElements.Add(new TabElement(trafficSettingList, SettingTabButton.TabType.Traffic));
        tabMenuElement.Add(trafficSettingList);


        //creating lighting tab, this one is simple hihi
        VisualElement lightingElement = lightingTab.Instantiate();
        Slider ambientLight = lightingElement.Q<Slider>("slider-ambient");
        ambientLight.value = lightingSettings.ambient;
        ambientLight.RegisterValueChangedCallback(x => UpdateAmbient(x.newValue));
        Slider intensityLight = lightingElement.Q<Slider>("slider-intensity");
        intensityLight.value = lightingSettings.intensity;
        intensityLight.RegisterValueChangedCallback(x => UpdateIntensity(x.newValue));

        NumberField xField = new NumberField(lightingElement.Q<TextField>("tf-x"));
        NumberField yField = new NumberField(lightingElement.Q<TextField>("tf-y"));
        NumberField zField = new NumberField(lightingElement.Q<TextField>("tf-z"));
        VectorFieldController vectorFieldController = new VectorFieldController(xField, yField, zField);
        vectorFieldController.onVectorUpdateEvent += UpdateLightDirection;

        tabElements.Add(new TabElement(lightingElement, SettingTabButton.TabType.Light));
        tabMenuElement.Add(lightingElement);
    }
    
    void UpdateLightDirection(Vector3 vector, string name)
    {
        lightingSettings.direction = vector;
    }
    void UpdateAnomalyValue(Slider slider)
    {
        anomalyOptions[int.Parse(slider.bindingPath)].value = slider.value;
    }
    void UpdateAnomalyValue(Toggle toggle)
    {
        anomalyOptions[int.Parse(toggle.bindingPath)].active = toggle.value;
    }
    void UpdateAmbient(float value)
    {
        lightingSettings.ambient = value;
    }
    void UpdateIntensity(float value)
    {
        lightingSettings.intensity = value;
    }
    void UpdateTrafficValue(Slider slider)
    {
        trafficSettings[int.Parse(slider.bindingPath)].value = slider.value;
    }

    //
    //     GET functions
    //

    public List<AnomalyOption> GetAnomalies()
    {
        return anomalyOptions;
    }

    public List<TrafficSetting> GetTrafficSettings()
    {
        return trafficSettings;
    }

    public LightingSetting GetLightingSettings()
    {
        return lightingSettings;
    }

    public Texture2D GetMask()
    {
        return viewportHandler.RenderMask();
    }

    public ExportSetting GetExportSettings()
    {
        return exportSettings;
    }

    public RoadTransformSetting GetRoadTransform()
    {
        return roadTransformSettings;
    }

}

//
//     UI classes
//

public class SettingTabButton
{
    public TabType buttonTab;
    public Button button;
    public VisualElement darkner;

    public SettingTabButton(Button b, TabType tab)
    {
        button = b;
        darkner = button.Q<VisualElement>("tab-darkner");
        buttonTab = tab;
    }

    public void DisplayIfType(TabType tab)
    {
        if (tab == buttonTab) darkner.style.display = DisplayStyle.None;
        else darkner.style.display = DisplayStyle.Flex;
    }

    public enum TabType
    {
        Anomalies,
        Traffic,
        Light
    }
}

public class TabElement
{
    public VisualElement visualElement;
    public SettingTabButton.TabType tabType;

    public TabElement(VisualElement element, SettingTabButton.TabType type)
    {
        visualElement = element;
        tabType = type;
    }

    public void DisplayIfType(SettingTabButton.TabType type)
    {
        try
        {
            if (type == tabType) visualElement.style.display = DisplayStyle.Flex;
            else visualElement.style.display = DisplayStyle.None;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }

    }
}

public class NumberField
{
    public TextField textField { get; private set; }
    VisualElement label;
    float lastXPos;
    float sensitivity = 0.5f;
    public bool allowNegativeNumbers = true;
    public float value { get; private set; }
    public Action<NumberField> onValueUpdateEvent;
    public static MonoBehaviour instance; //instance for doing dragging

    public NumberField(TextField textField, bool allowNegative = true)
    {
        allowNegativeNumbers = allowNegative;
        this.textField = textField;
        label = textField.Q<Label>();
        label.RegisterCallback<MouseDownEvent>(x => instance.StartCoroutine(DoMouseDrag()));
        this.textField.RegisterValueChangedCallback(x => UpdateValue());
    }

    public void UpdateValue()
    {
        KeepTextFieldAsNumbers();
        value = int.Parse(textField.value);
        onValueUpdateEvent.Invoke(this);
    }

    public void UpdateValue(float newValue)
    {
        textField.value = newValue.ToString();
    }

    public void UpdateValue(int newValue)
    {
        textField.value = newValue.ToString();
    }

    void KeepTextFieldAsNumbers()
    {
        string tempValue = textField.value;
        string newValue = "";
        bool isNegative = false;
        for (int i = 0; i < tempValue.Length; i++)
        {
            if (int.TryParse(tempValue[i].ToString(), out int intValue))
            {
                newValue += intValue;
            }
            else if(i == 0 && tempValue[i].ToString() == "-")
            {
                newValue += "-";
                isNegative = true;
            }
        }
        if (isNegative &&  allowNegativeNumbers == false) newValue = "1";
        textField.SetValueWithoutNotify(newValue);
    }

    IEnumerator DoMouseDrag()
    {
        lastXPos = Input.mousePosition.x;
        yield return new WaitUntil(() => MouseSpyware());
    }

    bool MouseSpyware()
    {
        UpdateValue((int)(value + (Input.mousePosition.x - lastXPos) * sensitivity));
        lastXPos = Input.mousePosition.x;
        return Input.GetMouseButtonUp(0);
    }

}

public class VectorFieldController
{
    NumberField xField;
    NumberField yField;
    NumberField zField;
    public Action<Vector3, string> onVectorUpdateEvent;
    public Vector3 value { get; private set; }
    public string name { get; private set; }

    public VectorFieldController(NumberField x, NumberField y, NumberField z, string name = "")
    {
        this.name = name;
        xField = x;
        yField = y;
        zField = z;

        xField.onValueUpdateEvent += UpdateVector;
        yField.onValueUpdateEvent += UpdateVector;
        zField.onValueUpdateEvent += UpdateVector;
    }

    void UpdateVector(NumberField field)
    {
        value = new Vector3(xField.value, yField.value, zField.value);
        onVectorUpdateEvent.Invoke(value, name);
    }
}



//
//    Serializable classes
//

[Serializable]
public class AnomalyOption
{
    public float value;
    public string name;
    public bool active;
}

[Serializable]
public class TrafficSetting
{
    public float value;
    public string name;
}


[Serializable]
public class LightingSetting
{
    public float intensity;
    public float ambient;
    public Vector3 direction;
}

[Serializable]
public class RoadTransformSetting
{
    public Vector3 position;
    public Vector3 rotation;
    public float scale;
}

[Serializable]
public class ExportSetting
{
    public int videoLength;
    public int videoAmount;
    public bool mixAnomalies;
    public ExportOutputType outputType;
}

public enum ExportOutputType
{
    ImageSequence,
    VideoFile,
    Gif,
    SingleImage
}

