using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;

public class UISaintSequenceViewerHandler : MonoBehaviour
{
    public Text number;
    public Text sequence_name;
    public Text parameter;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void setContent(string jsonString)
    {
        print(jsonString);

        // Define header
        string newLine = "\n";
        string str_number = "#" + newLine;
        string str_name = "NAME" + newLine;
        string str_parameter = "PARAMETER" + newLine;

        // Here the JSON string has to merged
        JSONNode root = JSON.Parse(jsonString);

        foreach (JSONNode node in root["currentSequence"].Values)
        {
            str_number += node["number"] + newLine;
            str_name += node["name"] + newLine;
            str_parameter += node["parameters"].ToString() + newLine;
        }

        // Set text fields
        number.text = str_number;
        sequence_name.text = str_name;
        parameter.text = str_parameter;
    }
}
