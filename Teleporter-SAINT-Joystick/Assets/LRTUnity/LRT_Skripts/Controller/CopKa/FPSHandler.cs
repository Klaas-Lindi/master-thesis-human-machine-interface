using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSHandler : MonoBehaviour {

    private OperatorState operatorState;
    public UavState uavState;

    public GameObject uiOperatorOrientation;
    public GameObject uiOperatorPosition;
    public GameObject uiCursorPosition;
    public GameObject uiCamera;
    public GameObject uiAutomationText;

    public float moveYFaktor = 0.25f;

    public float moveYFaktorWheel = 0.25f;
    public float radiusFaktorWheel = 0.25f;

    private float f_key_timer = 0.0f;
    private float backspace_key_timer = 0.0f;
    private float realTime;

    // Automatism variables
    private int autoCircleState = 0; // 0: not active, 1: set postion, 2: set radius 
    private float cursorHeight = 0f;
    private float wheelAxisValue = 0f;

    private int autoWaypointsState = 0; // 0: not active, 1: set postions, 1: set height, 3: end path
    private List<Vector3> uiWaypoints;

    // Use this for initialization
    void Start () {
        uiWaypoints = new List<Vector3>();
        uiCursorPosition.GetComponent<OperatorState>().Waypoints = new List<Vector3>();
        operatorState = this.GetComponent<OperatorState>();
        operatorState.Waypoints = new List<Vector3>();
        uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
        uiAutomationText.SetActive(false);
        uiCursorPosition.SetActive(false);
        //uiVirtualPosition.GetComponent<OperatorState>() = operatorState;
        operatorState.Command = TORCommand.VehicleRequestState;
    }
	
	// Update is called once per frame
	void Update () {
        // Update camera position
        uiCamera.transform.position = this.transform.position;
        uiCamera.transform.rotation = this.transform.rotation;

        // Add y movement
        if (Input.GetKey(KeyCode.E))
        {
            Vector3 cur_pos = this.transform.position;
            cur_pos.y += moveYFaktor;
            this.transform.position = cur_pos;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            Vector3 cur_pos = this.transform.position;
            cur_pos.y -= moveYFaktor;
            this.transform.position = cur_pos;
        }


        // Guidance mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (uavState.OperationState == UavState.UavOperationState.IDLE)
            {
                operatorState.Command = TORCommand.VehicleActive;
            }
            else
            {
                operatorState.Command = TORCommand.VehicleIdle;
            }
        }


        // Handle Takeoff and land
        if (Input.GetKey(KeyCode.F) )
        {
            if (this.f_key_timer > 2)
            {
                if (uavState.Condition == UavState.UavCondition.LANDED)
                {
                    operatorState.Command = TORCommand.VehicleTakeOff;
                }
                else if (uavState.Condition == UavState.UavCondition.FLYING)
                {
                    operatorState.Command = TORCommand.VehicleLand;
                }
                this.f_key_timer = -100;
            }
            else
            {
                this.f_key_timer += (Time.realtimeSinceStartup - realTime);
            }
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            this.f_key_timer = 0;
        }


        // Escape sequences
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Waypoints
            if (autoWaypointsState != 0)
            {
                if (autoWaypointsState == 2)
                {
                    autoWaypointsState = 1;
                }
                else
                {
                    if (uiWaypoints.Count > 0)
                    {
                        uiWaypoints.RemoveAt(uiWaypoints.Count - 1);
                        uiCursorPosition.GetComponent<OperatorState>().Waypoints.RemoveAt(uiCursorPosition.GetComponent<OperatorState>().Waypoints.Count - 1);
                    }
                    else
                    {
                        autoWaypointsState = 0;
                        uiCursorPosition.SetActive(false);
                        uiAutomationText.SetActive(false);
                        uiWaypoints.Clear();
                        uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    }
                }
            }
            // Autoscan
            autoCircleState -= 1;
            if (autoCircleState == 0)
            {
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                uiCursorPosition.SetActive(false);
                uiAutomationText.SetActive(false);
            }
        }
        // AUTOSCAN
        if (Input.GetKeyDown(KeyCode.C) && !isUiAutomatismActive())
        {
            //if (uavState.Condition == UavState.UavCondition.FLYING)
            //operatorState.Command = "D:AUTOSCAN:CIRCLE";
            autoCircleState = 1;
            uiCursorPosition.SetActive(true);
            uiAutomationText.SetActive(true);

            // Postition of groundplane intersection
            Vector3 direction = this.transform.TransformDirection(Vector3.forward);
            float factor = -this.transform.position.y / direction.y;
            uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor *direction + new Vector3(0, 0f, 0f);

            uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.AUTOSCAN_CIRCLE);
        }

        if (Input.GetKeyDown(KeyCode.V) && !isUiAutomatismActive())
        {
            autoWaypointsState = 1;
            uiCursorPosition.SetActive(true);
            
            // Postition of groundplane intersection
            Vector3 direction = this.transform.TransformDirection(Vector3.forward);
            float factor = -this.transform.position.y / direction.y;
            uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor * direction + new Vector3(0, 0f, 0f);

            uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.WAYPOINT);
        }

        // RTL
        if (Input.GetKeyDown(KeyCode.R) )
        {
            operatorState.Command = TORCommand.VehicleRTL;
        }

        // Waypoints


        // Request State
        if (Input.GetKeyDown(KeyCode.P))
        {
            operatorState.Command = TORCommand.VehicleRequestState;
        }
        // Request Init Point
        if (Input.GetKeyDown(KeyCode.I))
        {
            operatorState.Command = TORCommand.VehicleRequestInitPoint;
        }

        // Important control funcition
        // Activate Teleoperator Commands
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (uavState.Mode == UavState.UavMode.WAITING)
            {
                operatorState.Command = TORCommand.TMTCStartGuidance;
            }
            else if (uavState.Mode == UavState.UavMode.GUIDANCE_ACTIVE)
            {
                operatorState.Command = TORCommand.TMTCStopGuidance;
            }
        }

        if (Input.GetKey(KeyCode.L) )
        {
            if (this.backspace_key_timer > 5)
            {
                operatorState.Command = TORCommand.SystemReboot;
                this.backspace_key_timer = -100;
            }
            else
            {
                this.backspace_key_timer += (Time.realtimeSinceStartup - realTime);
            }
        }
        if (Input.GetKeyUp(KeyCode.L))
        {
            this.backspace_key_timer = 0;
        }

        if (!this.GetComponent<FlyingScript>().SkriptEnabled && !operatorState.ReadFromLog && !isUiAutomatismActive()) // Handle Script
        {
            //if (Input.GetMouseButton(1))
            //{
            // Get pose from camera
            if(uavState.OperationState == UavState.UavOperationState.AUTO)
            {
                operatorState.OperatorPose.position = uavState.CameraPose.position;
            }

            if (Input.GetKey(KeyCode.Mouse1))
            {
                //uiOperatorOrientation.SetActive(true);
                //uiOperatorPosition.SetActive(true);

                Pose tmp = new Pose();

                tmp.position = this.transform.position;
                tmp.rotation = this.transform.rotation;

                // Set parameters into operator state
                operatorState.OperatorPose = tmp;
                operatorState.VehicleActive = true;
            }
            else
            {
                //uiOperatorOrientation.SetActive(false);
                //uiOperatorPosition.SetActive(false);
            }
            //}
        }

        // Handle autocircleroutine
        float wheelAxis = Input.GetAxis("Mouse ScrollWheel");
        switch (autoCircleState)
        {
            case 1:
                Vector3 direction = this.transform.TransformDirection(Vector3.forward);
                float factor = -this.transform.position.y / direction.y;
                uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor * direction + new Vector3(0, 0f, 0f);
                uiCursorPosition.GetComponent<OperatorState>().StateChanged = true;
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    autoCircleState = 2;
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor * direction + new Vector3(0, 10f, 0f);
                }
                if (wheelAxis < 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().RadiusAutoCircle += radiusFaktorWheel;
                }
                if (wheelAxis > 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().RadiusAutoCircle -= radiusFaktorWheel;
                }
                // Set Text
                uiAutomationText.transform.position = uiCursorPosition.GetComponent<Transform>().position;
                uiAutomationText.transform.LookAt(this.transform);
                uiAutomationText.GetComponentInChildren<Text>().text = "Radius: " + uiCursorPosition.GetComponent<OperatorState>().RadiusAutoCircle.ToString("0.00") + " m";
                break;
            case 2:
                if (wheelAxis < 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position += new Vector3(0, moveYFaktorWheel, 0f);
                }
                if (wheelAxis > 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position -= new Vector3(0, moveYFaktorWheel, 0f);
                }
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                   autoCircleState = 3;
                }

                uiAutomationText.transform.position = uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position;
                uiCursorPosition.GetComponent<OperatorState>().StateChanged = true;
                uiAutomationText.transform.LookAt(this.transform);
                uiAutomationText.GetComponentInChildren<Text>().text = "Höhe: " + uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position.y.ToString("0.00") + " m";
                break;
            case 3:
               //operatorState.OperatorPose.position = uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position;
               // operatorState.RadiusAutoCircle = uiCursorPosition.GetComponent<OperatorState>().RadiusAutoCircle;
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                uiCursorPosition.SetActive(false);
                autoCircleState = 0;

                operatorState.Command = TORCommand.VehicleAutoCircle(uiCursorPosition.GetComponent<OperatorState>().RadiusAutoCircle, uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position);
                break;

        }

        // Handle Waypoints
        switch (autoWaypointsState)
        {
            case 1:
                Vector3 direction = this.transform.TransformDirection(Vector3.forward);
                float factor = -this.transform.position.y / direction.y;
                uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor * direction + new Vector3(0f, 0f, 0f);
                if(uiCursorPosition.GetComponent<OperatorState>().Waypoints.Count > 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().Waypoints.RemoveAt(uiCursorPosition.GetComponent<OperatorState>().Waypoints.Count - 1);
                }
                uiCursorPosition.GetComponent<OperatorState>().Waypoints.Add(uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position);
                uiCursorPosition.GetComponent<OperatorState>().StateChanged = true;
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position = this.transform.position + factor * direction + new Vector3(0f, 10f, 0f);
                    uiAutomationText.SetActive(true);
                    autoWaypointsState = 2;
                }
                if (Input.GetKeyUp(KeyCode.Mouse1))
                {
                    autoWaypointsState = 3;
                }
                break;
            case 2:
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    uiWaypoints.Add(uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position);
                    uiCursorPosition.GetComponent<OperatorState>().Waypoints.Add(uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position);
                    autoWaypointsState = 1;
                    uiAutomationText.SetActive(false);
                }
                if (wheelAxis < 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position += new Vector3(0, moveYFaktorWheel, 0f);
                }
                if (wheelAxis > 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position -= new Vector3(0, moveYFaktorWheel, 0f);
                }

                // Text
                if (uiCursorPosition.GetComponent<OperatorState>().Waypoints.Count > 0)
                {
                    uiCursorPosition.GetComponent<OperatorState>().Waypoints.RemoveAt(uiCursorPosition.GetComponent<OperatorState>().Waypoints.Count - 1);
                }
                uiCursorPosition.GetComponent<OperatorState>().Waypoints.Add(uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position);
                uiCursorPosition.GetComponent<OperatorState>().StateChanged = true;
                uiAutomationText.transform.LookAt(this.transform);
                uiAutomationText.GetComponentInChildren<Text>().text = "Höhe: " + uiCursorPosition.GetComponent<OperatorState>().OperatorPose.position.y.ToString("0.00") + " m";
                break;
             case 3:
                operatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count-1];
                uiCursorPosition.GetComponent<OperatorState>().Waypoints.Clear();
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                uiCursorPosition.SetActive(false);
                autoWaypointsState = 0;

                operatorState.Command = TORCommand.VehicleAutoWaypoints(uiWaypoints);
                uiWaypoints.Clear();
                break;

        }

        //Get real time form system
        realTime = Time.realtimeSinceStartup;

    }

    private bool isUiAutomatismActive()
    {
        return (autoCircleState > 0 || autoWaypointsState > 0);
    }
}
