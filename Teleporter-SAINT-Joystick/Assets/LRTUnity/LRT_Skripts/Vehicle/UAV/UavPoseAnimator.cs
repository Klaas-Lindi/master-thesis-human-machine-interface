using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In this scripts all state informations will be animated over the UAV 
/// representation.
/// </summary>

public class UavPoseAnimator : MonoBehaviour {

    private UavState uavState;

    // moveable gimbal parts
    private Transform gimbalPartYaw;
    private Transform gimbalPartRoll;
    private Transform gimbalPartPitch;

    //
    public Vector3 animationCameraOffset = new Vector3(0.0f, 0.15f, 0.0f);

    // Use this for initialization
    void Start () {
        uavState = this.GetComponent<UavState>();

        gimbalPartYaw = this.transform.Find("Gimbalpart_yaw");
        gimbalPartRoll = gimbalPartYaw.Find("Gimbalpart_roll");
        gimbalPartPitch = gimbalPartRoll.Find("Gimbalpart_pitch");

        uavState.CameraPose.position = new Vector3();
        uavState.VehiclePose = new Pose();
        //cameraPose.rotation = new Quaternion();

    }
	
	// Update is called once per frame
	void Update () {
        if (uavState.CameraPose != null)
        {
            this.transform.position = uavState.CameraPose.position + this.animationCameraOffset;

            gimbalPartYaw.eulerAngles = new Vector3(0, uavState.CameraPose.rotation.eulerAngles.y, 0);
            gimbalPartRoll.eulerAngles = new Vector3(0, uavState.CameraPose.rotation.eulerAngles.y, uavState.CameraPose.rotation.eulerAngles.z);
            gimbalPartPitch.eulerAngles = new Vector3(uavState.CameraPose.rotation.eulerAngles.x, uavState.CameraPose.rotation.eulerAngles.y, uavState.CameraPose.rotation.eulerAngles.z);
        }
    }
}
