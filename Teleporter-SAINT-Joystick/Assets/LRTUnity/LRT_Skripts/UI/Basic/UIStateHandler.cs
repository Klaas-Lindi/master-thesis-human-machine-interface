using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIStateHandler : MonoBehaviour {

    public UavState uavState;

    public Text condition;
    public Text operationState;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(condition != null && operationState != null)
        {
            condition.text = uavState.Condition.ToString();
            operationState.text = uavState.OperationState.ToString();
        }
	}
}
