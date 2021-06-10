using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;

public class UISaintSequencePlannerHandler : MonoBehaviour
{
    public Text pastSequence;
    public Text currentSequence;
    public Text nextSequence;

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
        //print(jsonString);

        // Here the JSON string has to merged
        JSONNode root = JSON.Parse(jsonString);
        //print(root["sequences"].Count);

        // Set text fields
        pastSequence.text = root["previous"];
        currentSequence.text = root["current"];
        nextSequence.text = root["next"];
    }
}
