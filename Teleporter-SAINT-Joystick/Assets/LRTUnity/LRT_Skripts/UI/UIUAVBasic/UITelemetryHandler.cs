using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITelemetryHandler : MonoBehaviour {

    public UavState uavState;
    public OperatorState operatorState;

    public Text distance;
    public Text x;
    public Text y;
    public Text height;

    private string str_format = "{0,6:###.00}";

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(distance != null)
        {
            distance.text = "D: " + string.Format(str_format, Mathf.Sqrt(Mathf.Pow(uavState.CameraPose.position.x,2)+ Mathf.Pow(uavState.CameraPose.position.y, 2)+ Mathf.Pow(uavState.CameraPose.position.z, 2))) + " m";
        }

        if (x != null)
        {
            x.text = "X: " + string.Format(str_format, uavState.CameraPose.position.x) + " m";
        }

        if (y != null)
        {
            y.text = "Y: " + string.Format(str_format, uavState.CameraPose.position.z) + " m";
        }

        if (height != null)
        {
            height.text = "H: " + string.Format(str_format, uavState.CameraPose.position.y) + " m";
        }
    }
}
