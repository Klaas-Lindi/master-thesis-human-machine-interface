using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOperatorPosition : MonoBehaviour {

    public ControllerHandle controller;
    private OperatorState operatorState;

	// Use this for initialization
	void Start () {
        this.operatorState = controller.getActiveOperatorState();
	}
	
	// Update is called once per frame
	void Update () {
		this.transform.position = operatorState.OperatorPose.position;
	}
}
