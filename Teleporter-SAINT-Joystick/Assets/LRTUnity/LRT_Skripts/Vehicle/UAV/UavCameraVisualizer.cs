using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UavCameraVisualizer : MonoBehaviour {

    // screen objectes to visualize
    /// <summary>
    /// GameObject of the screen, where the UAV Camera should be visualized
    /// </summary>
    public GameObject Screen;

    private UavState uavState;
   
    // Offsets of the screen form the origin pose
    private Vector3 offsetRot = new Vector3(-90, 0, 0); //new Vector3(-90, 0, 0);
    private Vector3 offsetPos = new Vector3(0, 0.98f, 0);


    // Use this for initialization
    void Start () {
        uavState = this.GetComponent<UavState>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(uavState.CurrentFrame != null)
        {
            Screen.GetComponent<Renderer>().material.mainTexture = uavState.CurrentFrame;
        }

        // Set rotation
        Screen.transform.rotation = uavState.CameraPose.rotation;

        // Calculate Sphere position
        Vector3 tmp = Screen.transform.rotation * Vector3.forward;
        Screen.transform.position = tmp * 50f;

        // Add offset to rotation 
        Screen.transform.rotation *= Quaternion.Euler(offsetRot);

        // Add position from player
        Screen.transform.position += (uavState.CameraPose.position) - offsetPos;
    
    }
}
