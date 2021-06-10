using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;

public class UISaintWorkspaceHandler : MonoBehaviour
{
    public Text number;
    public Text name;
    public Text value;
    public Text options;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setContent(string jsonWorkspace)
    {
        //print(jsonWorkspace);

        // Define header
        string newLine = "\n";
        string str_number = "#" + newLine;
        string str_name = "NAME" + newLine;
        string str_value = "VALUE" + newLine;
        string str_options = "OPTIONS" + newLine;

        // Here the JSON string has to merged
        JSONNode root = JSON.Parse(jsonWorkspace);

        foreach(JSONNode node in root["workspace"].Values)
        {
            str_number += node["number"] + newLine;
            str_name += node["name"] + newLine;
            str_value += node["value"] + newLine;
            str_options += node["options"].ToString() + newLine;
        }

        // Set text fields
        number.text = str_number;
        name.text = str_name;
        value.text = str_value;
        options.text = str_options;

    }
}
