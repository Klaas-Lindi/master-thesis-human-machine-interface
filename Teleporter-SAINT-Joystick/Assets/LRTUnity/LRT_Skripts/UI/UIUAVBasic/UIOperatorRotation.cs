using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOperatorRotation : MonoBehaviour {

    // OperatorState
    public ControllerHandle controller;
    private OperatorState operatorState;
    public float distance = 1.0f;

    // Offsets of the screen form the origin pose
    private Vector3 offsetRot = new Vector3(-90, 0, 0); //new Vector3(-90, 0, 0);
    private Vector3 offsetPos = new Vector3(0, 0, 0);

    // Use this for initialization
    void Start () {
        this.operatorState = controller.getActiveOperatorState();
    }
	
	// Update is called once per frame
	void Update () {
        // Set rotation
        this.transform.rotation = operatorState.OperatorPose.rotation;

        // Calculate Sphere position
        Vector3 tmp = this.transform.rotation * Vector3.forward;
        this.transform.position = tmp * distance;

        // Add offset to rotation 
        this.transform.rotation *= Quaternion.Euler(offsetRot);

        // Add position from player
        this.transform.position += (operatorState.OperatorPose.position) - offsetPos;
    }
}
