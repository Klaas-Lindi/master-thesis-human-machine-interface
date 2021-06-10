using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISAINTTelemetryHandler : MonoBehaviour
{
    public Transform telemetryObject;
    public OperatorState operatorState;

    public Text field1;
    public Text field2;
    public Text field3;
    public Text field4;

    private string str_format = "{0,6:###.00}";

    private string saveCmd; 

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (field1 != null)
        {
            field1.text = "X: " + string.Format(str_format, telemetryObject.position.x + " m");
        }

        if (field2 != null)
        {
            field2.text = "Y: " + string.Format(str_format, telemetryObject.position.z + " m");
        }

        if (field3 != null)
        {
            field3.text = "Z: " + string.Format(str_format, telemetryObject.position.y + " m");
        }

        if (field4 != null)
        {
            if (operatorState.Command.Length > 0)
                saveCmd = operatorState.Command;
            field4.text = "Cmd: " + saveCmd;// evtl last command? "X: " + string.Format(str_format, telemetryObject.position.x);
        }
    }
}
