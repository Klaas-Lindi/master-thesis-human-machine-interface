using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UISaintStateHandler : MonoBehaviour
{
    public SAINTState saintState;

    public Text controlState;
    public Text autonomoustate;
    public Text connection;
    //public Text operatorMessage;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (controlState != null && autonomoustate != null && connection)
        {
            controlState.text = saintState.ControlState.ToString();
            autonomoustate.text = saintState.AutonomousState.ToString();
            connection.text = (saintState.IsConnected) ? "Connected" : "Not Connected";
            connection.color = (saintState.IsConnected) ? Color.green : Color.red;
            //operatorMessage.text = saintState.OperatorMessage.ToString();
        }
    }
}
