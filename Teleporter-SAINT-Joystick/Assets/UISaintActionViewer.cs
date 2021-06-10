using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.UI;
using System;

public class UISaintActionViewer : MonoBehaviour
{
    public OperatorState operatorState;
    public RobotControlSAINT robotControl;

    public GameObject[] buttons;
    public GameObject[] parameterSlots;

    private ActionOption[] actionOptions;
    private CommandOption[] commandOptions;
    private int lastbuttonClick = 0;
    private int currentParameter = 0;
    public bool InitializedActionButton = false;

    private Color buttonColor;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject button in buttons)
            button.SetActive(false);
        foreach (GameObject slot in parameterSlots)
            slot.SetActive(false);

        actionOptions = new ActionOption[buttons.Length];
        commandOptions = new CommandOption[buttons.Length];
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void setActions(string jsonString)
    {
        // Here the JSON string has to merged
        JSONNode root = JSON.Parse(jsonString);
        int actionCount = root["available_actions"].Count;

        for (int i = 0; (i < actionCount) && (i < buttons.Length); i++)
        {
            buttons[i].SetActive(true);

            buttons[i].GetComponentInChildren<Text>().text = root["available_actions"][i]["actionName"];
            //print(root["available_actions"][i]);
            if (actionOptions[i] == null)
                actionOptions[i] = new ActionOption();
            actionOptions[i].jsonParse(root["available_actions"][i]);
            if (actionOptions[i].parameters.Count > 0)

                TurnBrown(i);

            else TurnGreen(i);
        }

        for (int i = actionCount; i < buttons.Length; i++)
        {
            buttons[i].SetActive(false);
        }
        foreach (GameObject slot in parameterSlots)
            slot.SetActive(false);
    }

    public void TurnBrown(int buttonNo)
    {
        ColorBlock colors = buttons[buttonNo].GetComponent<Button>().colors;
        colors.normalColor = new Color32(181, 134, 76, 200); ;
        colors.highlightedColor = new Color32(181, 134, 76, 255);
        colors.selectedColor = new Color32(61, 160, 121, 255);
        buttons[buttonNo].GetComponent<Button>().colors = colors;
    }

    public void TurnGreen(int buttonNo)
    {
        ColorBlock colors = buttons[buttonNo].GetComponent<Button>().colors;
        colors.normalColor = new Color32(61, 160, 121, 200); ;
        colors.highlightedColor = new Color32(61, 160, 121, 255);
        colors.selectedColor = new Color32(61, 160, 121, 255);
        buttons[buttonNo].GetComponent<Button>().colors = colors;
    }

    //public void isButtonSelected(int buttonNo)
    //{
    //    buttonColor = 
    //}

    public void setCommands(string jsonString)
    {
        //print(jsonString);

        // Here the JSON string has to merged
        JSONNode root = JSON.Parse(jsonString);
        int actionCount = root["available_commands"].Count;

        for (int i = 0; (i < actionCount) && (i < buttons.Length); i++)
        {
            buttons[i].SetActive(true);
            buttons[i].GetComponentInChildren<Text>().text = root["available_commands"][i]["commandName"];
            print(buttons[i].GetComponentInChildren<Text>().text);
            if (commandOptions[i] == null)
                commandOptions[i] = new CommandOption();
            commandOptions[i].jsonParse(root["available_commands"][i]);
        }

        for (int i = actionCount; i < buttons.Length; i++)
        {
            buttons[i].SetActive(false);
        }
        foreach (GameObject slot in parameterSlots)
            slot.SetActive(false);
    }

    public void actionSelection(int buttonNumber)
    {
        foreach (GameObject slot in parameterSlots)
            slot.SetActive(false);

        //print(buttonNumber);

        lastbuttonClick = buttonNumber;
        //if (actionOptions[buttonNumber].parameters.Count > 0)
        //    if (buttons[buttonNumber].GetComponent<Button>().clicked)
        //    {

                robotControl.Sequence = "{\"operator_sequence\": [" +
                                            "{\"actionName\": \"" + actionOptions[buttonNumber].cmd + "\", " +
                                             "\"number\": 1, \"parameters\": []}]}";
        //    } 
        InitializedActionButton = true;

        for (int i = 0; (i < actionOptions[buttonNumber].parameters.Count) && (i < parameterSlots.Length); i++)
        {
            parameterSlots[i].SetActive(true);
            actionOptions[buttonNumber].parameters[i].SetUI(parameterSlots[i]);
        }
    }

    public void parameterChange(float value)
    {
        if (!InitializedActionButton)
        {
            robotControl.Sequence = "{\"operator_sequence\": [" +
                                        "{\"actionName\": \"" + actionOptions[lastbuttonClick].cmd + "\", " +
                                         "\"number\": 1, \"parameters\": [{ " +
                                            "\"parameterName\": \"" + actionOptions[lastbuttonClick].parameters[currentParameter].name + "\" ," +
                                            "\"currentValue\": \"" + value + "\"}]}]}";
        }
        else
        {
            InitializedActionButton = false;
        }
    }

    public void setParameterNumber(int parameterNumber)
    {
        currentParameter = parameterNumber;
    }
}

class ActionOption
{
    public string label;
    public string cmd;
    public List<ActionParameter> parameters = new List<ActionParameter>();

    public void jsonParse(JSONNode node)
    {
        //if(this.parameters.Count > 0)
        this.parameters.Clear();

        this.label = node["actionName"];
        this.cmd = node["actionName"];

        if (node["parameters"].Count > 0)
        {
            Debug.Log("parameter found");
            foreach (JSONNode param in node["parameters"].Values)
            {
                if (param.HasKey("parameterName"))
                {
                    string a = param["parameterName"].Value;
                    float.Parse(param["currentValue"].Value);
                    float.Parse(param["minValue"].Value);
                    float.Parse(param["maxValue"].Value);
                    parameters.Add(new ActionParameter(param["parameterName"].Value,
                                    float.Parse(param["currentValue"].Value),
                                    float.Parse(param["minValue"].Value),
                                    float.Parse(param["maxValue"].Value)));
                }
            }
        }

    }
}

class CommandOption
{
    public string label;
    public string cmd;
    public List<CommandParameter> parameters = new List<CommandParameter>();

    public void jsonParse(JSONNode node)
    {
        //if(this.parameters.Count > 0)
        this.parameters.Clear();

        this.label = node["commandName"];
        this.cmd = node["commandName"];

        if (node["parameters"].Count > 0)
        {
            foreach (JSONNode param in node["parameters"].Values)
            {
                if (param.HasKey("parameterName"))
                {
                    string a = param["parameterName"].Value;
                    float.Parse(param["currentValue"].Value);
                    float.Parse(param["minValue"].Value);
                    float.Parse(param["maxValue"].Value);
                    parameters.Add(new CommandParameter(param["parameterName"].Value,
                                    float.Parse(param["currentValue"].Value),
                                    float.Parse(param["minValue"].Value),
                                    float.Parse(param["maxValue"].Value)));
                }
            }
        }

    }
}

public class CommandParameter
{
    public string name;

    public float value;
    public float min;
    public float max;

    public CommandParameter(string name, float value, float min, float max)
    {
        this.name = name;
        this.value = value;
        this.min = min;
        this.max = max;
    }

    internal void SetUI(GameObject gameObject)
    {
        gameObject.GetComponentInChildren<Text>().text = this.name + ":";
        Slider slider = gameObject.GetComponentInChildren<Slider>();
        slider.minValue = this.min;
        slider.maxValue = this.max;
        slider.value = this.value;
    }
}

public class ActionParameter
{
    public string name;

    public float value;
    public float min;
    public float max;

    public ActionParameter(string name, float value, float min, float max)
    {
        this.name = name;
        this.value = value;
        this.min = min;
        this.max = max;
    }

    internal void SetUI(GameObject gameObject)
    {
        gameObject.GetComponentInChildren<Text>().text = this.name + ":";
        Slider slider = gameObject.GetComponentInChildren<Slider>();
        slider.minValue = this.min;
        slider.maxValue = this.max;
        slider.value = this.value;
    }
}
