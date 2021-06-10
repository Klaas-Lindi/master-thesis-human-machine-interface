
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if HMI_SM_ACTIVE
using SpaceNavigatorDriver;


public class SMHandler : MonoBehaviour
{

    private OperatorState operatorState;
    public UavState uavState;
    public GameObject screen;

    public GameObject uiOperatorOrientation;
    public GameObject uiOperatorPosition;
    public GameObject uiCursorPosition;
    public GameObject uiCamera;
    public GameObject uiAutomationText;
    public GameObject uiVirtualPath;
    public UiStreamScreen uiStreamScreen;
    public UiMapInput uiMapInput;

    // Get Buttons
    public UIButtonAnimation buttonTL;
    public UIButtonAnimation buttonLanding;
    public UIButtonAnimation buttonRTL;
    public UIButtonAnimation buttonAC;
    public UIButtonAnimation buttonACSetWaypoint;
    public UIButtonAnimation buttonACConfirm;
    public UISlider sliderACHeight;
    public UISlider sliderACRadius;
    public UIButtonAnimation buttonWP;
    public UIButtonAnimation buttonWPSetWaypoint;
    public UISlider sliderWPHeight;
    public UIButtonAnimation buttonWPRemoveWaypoint;
    public UIButtonAnimation buttonWPConfirm;

    public float moveYFaktor = 0.25f;

    public float moveYFaktorWheel = 0.25f;
    public float radiusFaktorWheel = 0.25f;

    public float spaceMouseRotationSenistivity = 0.25f;
    public float spaceMouseTranslationSensitivity = 0.25f;
    public float spaceMouseDeadZone = 0.1f;

    private float f_key_timer = 0.0f;
    private float backspace_key_timer = 0.0f;
    private float realTime;

    private float yaw = 0;
    private float pitch = 0;

    // Automatism variables
    private int autoMode = 0; // 0: no automatism, 1: rotation allowede, 2: fully automatism

    private int autoCircleState = 0; // 0: not active, 1: set postion, 2: set radius 
    private float cursorHeight = 0f;
    private float wheelAxisValue = 0f;

    private int autoWaypointsState = 0; // 0: not active, 1: set postions, 1: set height, 3: end path
    private List<Vector3> uiWaypoints;

    private Pose tmp = new Pose();
    private OperatorState uiOperatorState;

    // Use this for initialization
    void Start()
    {
        screen.GetComponent<Renderer>().enabled = false;
        uiWaypoints = new List<Vector3>();
        uiCursorPosition.GetComponent<OperatorState>().Waypoints = new List<Vector3>();
        operatorState = this.GetComponent<OperatorState>();
        operatorState.Waypoints = new List<Vector3>();
        uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);

        uiOperatorState = uiCursorPosition.GetComponent<OperatorState>();
        autoMode = 0;
        //uiVirtualPosition.GetComponent<OperatorState>() = operatorState;
        //operatorState.Command = "D:REQUEST_STATE";
    }

    // Update is called once per frame
    void Update()
    {
        // Update camera position
        uiCamera.transform.position = this.transform.position;
        uiCamera.transform.rotation = this.transform.rotation;

        Vector3 mapWorldMousePosition = uiMapInput.GetWorldMousePosition();
        Vector3 mapScreenMousePosition = uiMapInput.GetScreenMousePosition();
        Vector3 automatismTextPosition = mapScreenMousePosition + new Vector3(0f, 50f, 0);

        // Guidance mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (uavState.OperationState == UavState.UavOperationState.IDLE)
            {
                autoMode = 0;
                operatorState.Command = TORCommand.VehicleActive;
            }
            else
            {
                autoMode = 0;
                autoCircleState = -99;
                autoWaypointsState = -99;
                operatorState.Command = TORCommand.VehicleIdle;
            }
        }


        // Handle Takeoff and landing
        if (Input.GetKey(KeyCode.F))
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
        if(buttonTL.GetButtonDown())
        {
            if (uavState.Condition == UavState.UavCondition.LANDED)
            {
                operatorState.Command = TORCommand.VehicleTakeOff;
            }
            autoCircleState = -99;
            autoWaypointsState = -99;
        }
        if(buttonLanding.GetButtonDown())
        {
            if (uavState.Condition == UavState.UavCondition.FLYING)
            {
                operatorState.Command = TORCommand.VehicleLand;
            }
        }
        // RTL
        if (Input.GetKeyDown(KeyCode.R) || buttonRTL.GetButtonDown())
        {
            operatorState.Command = TORCommand.VehicleRTL;
        }

        // Escape sequences
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Waypoints
            if (autoWaypointsState != 0)
            {
                if (uiWaypoints.Count > 0)
                {
                    uiWaypoints.RemoveAt(uiWaypoints.Count - 1);
                    uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                }
                else
                {
                    autoWaypointsState = -99;
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    buttonWP.GetComponentInParent<UIWaypointHandler>().wpButtonOnClick(false);
                    buttonWP.GetComponentInChildren<UIChangeImage>().setImageOnClick(false);
                }

            }
            // Autoscan
            autoCircleState -= 1;
            if (autoCircleState == 0)
            {
                autoCircleState = -99; // Abort autocirc
                buttonAC.GetComponentInParent<UIAutocircleHandler>().acButtonOnClick(false);
                buttonAC.GetComponentInChildren<UIChangeImage>().setImageOnClick(false);
            }
        }

        // AUTOSCAN
        if (Input.GetKeyDown(KeyCode.C))
        {
            buttonAC.GetComponentInParent<Button>().onClick.Invoke();
        }
        if (buttonAC.GetButtonDown())
        {

            if (autoCircleState <= 0)
            {
                autoCircleState = 1;
                uiCursorPosition.SetActive(true);
                uiOperatorState.OperatorPose.position = mapWorldMousePosition;
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.AUTOSCAN_CIRCLE);
                autoWaypointsState = -99;
            }
            else
            {
                autoCircleState = -99;
            }
        }
        // Waypoints
        if (Input.GetKeyDown(KeyCode.V))
        {
            buttonWP.GetComponentInParent<Button>().onClick.Invoke();
        }
        if (buttonWP.GetButtonDown())
        {
            
            if (autoWaypointsState <= 0)
            {
                autoWaypointsState = 1;
                uiCursorPosition.SetActive(true);
                uiOperatorState.OperatorPose.position = mapWorldMousePosition;
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.WAYPOINT);
                autoCircleState = -99;   
            }
            else
            {
                autoWaypointsState = -99;
            }
        }




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

        if (Input.GetKey(KeyCode.L))
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


        if (!this.GetComponent<FlyingScript>().SkriptEnabled && !operatorState.ReadFromLog) // && !isUiAutomatismActive()) // Handle Script
        {
            SpaceNavigator.SetRotationSensitivity(spaceMouseRotationSenistivity);
            SpaceNavigator.SetTranslationSensitivity(spaceMouseTranslationSensitivity);
           
            // Space Mouse movement
            this.transform.Translate(SpaceNavigator.Translation, Space.Self);
            // This method keeps the horizon horizontal at all times.
            // Perform azimuth in world coordinates.
            yaw = SpaceNavigator.Rotation.Yaw();
            pitch = SpaceNavigator.Rotation.Pitch();
            this.transform.Rotate(Vector3.up, yaw * Mathf.Rad2Deg, Space.World);
            // Perform pitch in local coordinates.
            this.transform.Rotate(Vector3.right, pitch * Mathf.Rad2Deg, Space.Self);

            if ((Vector3.Distance(tmp.position, this.transform.position) > spaceMouseDeadZone) || (Quaternion.Angle(tmp.rotation, this.transform.rotation) > spaceMouseDeadZone))
            {
                if (autoMode < 1)
                {
                    tmp.position = this.transform.position;
                }
                else
                {
                    tmp.position = operatorState.OperatorPose.position;
                }
                if (autoMode < 2)
                {
                    tmp.rotation = this.transform.rotation;
                }
                else
                {
                    tmp.rotation = operatorState.OperatorPose.rotation;
                }

                
                // Set parameters into operator state
                uiStreamScreen.EnableTransparat = true;
                operatorState.OperatorPose = tmp;
                operatorState.VehicleActive = true;
            }
            else
            {
                uiStreamScreen.EnableTransparat = false;
            }        
        }

        // Handle autocircleroutine
        float wheelAxis = Input.GetAxis("Mouse ScrollWheel");
        switch (autoCircleState)
        {

            case 1:
                if (!buttonAC.GetMouseOverButton()
                    && !buttonACConfirm.GetMouseOverButton()
                    && !buttonACSetWaypoint.GetMouseOverButton()
                    && !sliderACHeight.GetComponentInParent<UIButtonAnimation>().GetMouseOverButton()
                    && !sliderACRadius.GetComponentInParent<UIButtonAnimation>().GetMouseOverButton())
                {
                    uiOperatorState.OperatorPose.position = mapWorldMousePosition;
                }
                else
                {
                    uiOperatorState.OperatorPose.position = new Vector3();
                }
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    autoCircleState = 2;
                }
                goto case 99;
            case 2:               
                if (buttonACConfirm.GetButtonDown())
                {
                    autoCircleState = 3;
                }
                if (buttonACSetWaypoint.GetButtonDown())
                {
                    autoCircleState = 1;
                }
                goto case 99; 
            case 3:
                //operatorState.OperatorPose.position = uiOperatorState.OperatorPose.position;
                //operatorState.RadiusAutoCircle = uiOperatorState.RadiusAutoCircle;
                uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                uiCursorPosition.SetActive(false);
                autoCircleState = 4;
                autoMode = 1;
                operatorState.Command = TORCommand.VehicleAutoCircle(uiOperatorState.RadiusAutoCircle, uiOperatorState.OperatorPose.position);
                break;
            case 4:
                if((uiOperatorState.RadiusAutoCircle != sliderACRadius.GetValue()) || (uiOperatorState.OperatorPose.position.y !=  sliderACHeight.GetValue()))
                {
                    uiCursorPosition.SetActive(true);
                    uiOperatorState.OperatorPose.position = uiOperatorState.OperatorPose.position;
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.AUTOSCAN_CIRCLE);
                    autoCircleState = 5;
                    goto case 99;
                }
                if(buttonACSetWaypoint.GetButtonDown())
                {
                    uiCursorPosition.SetActive(true);
                    uiOperatorState.OperatorPose.position = mapWorldMousePosition;
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.AUTOSCAN_CIRCLE);
                    autoCircleState = 1;
                }
                break;
            case 5:
                if (buttonACConfirm.GetButtonDown())
                {
                    autoCircleState = 3;
                }
                goto case 99;
            case 99:
                // Radius
                uiOperatorState.RadiusAutoCircle = sliderACRadius.GetValue();
                
                // Height
                uiOperatorState.OperatorPose.position = new Vector3(uiOperatorState.OperatorPose.position.x, sliderACHeight.GetValue(), uiOperatorState.OperatorPose.position.z);
                uiOperatorState.StateChanged = true;
                break;
            case -99:
                if (autoWaypointsState <= 0)
                {
                    uiCursorPosition.SetActive(false);
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                }
                //uiAutomationText.SetActive(false);

                autoCircleState = 0;
                break;
        }

        // Handle Waypoints
        switch (autoWaypointsState)
        {
            case 1:
                if(!buttonWP.GetMouseOverButton()
                    && !buttonWPConfirm.GetMouseOverButton()
                    && !buttonWPRemoveWaypoint.GetMouseOverButton()
                    && !buttonWPSetWaypoint.GetMouseOverButton()
                    && !sliderWPHeight.GetComponentInParent<UIButtonAnimation>().GetMouseOverButton())
                {

                    uiOperatorState.OperatorPose.position = mapWorldMousePosition + new Vector3(0f, sliderWPHeight.GetValue(), 0f);
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        autoWaypointsState = 2;
                    }
                }
                else
                {
                    if(uiWaypoints.Count > 0)
                        uiOperatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1] + new Vector3(0f, sliderWPHeight.GetValue(), 0f);
                    else
                        uiOperatorState.OperatorPose.position = new Vector3(0f, sliderWPHeight.GetValue(), 0f);
                }
                goto case 4;
            case 2:
                uiWaypoints.Add(uiOperatorState.OperatorPose.position);
                uiOperatorState.Waypoints.Add(uiOperatorState.OperatorPose.position);
                autoWaypointsState = 1;
                break;
            case 3:
                operatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1];
                autoWaypointsState = 5;
                uiVirtualPath.SetActive(false);
                uiCursorPosition.SetActive(false);

                operatorState.Command = TORCommand.VehicleAutoWaypoints(uiWaypoints);
                uiWaypoints.Clear();

                break;
            case 4:
                if (buttonWPConfirm.GetButtonDown() && uiWaypoints.Count > 0)
                {
                    autoWaypointsState = 3;
                }
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    if (autoWaypointsState == 1)
                    {
                        uiOperatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1];
                        autoWaypointsState = 4;
                    }
                }
                if (buttonWPRemoveWaypoint.GetButtonDown())
                {
                    uiCursorPosition.SetActive(true);
                    uiVirtualPath.SetActive(true);
                    if (uiWaypoints.Count > 0)
                    {
                        uiWaypoints.RemoveAt(uiWaypoints.Count - 1);
                        uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                        uiOperatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1];
                    }
                }
                if (buttonWPSetWaypoint.GetButtonDown())
                {
                    uiCursorPosition.SetActive(true);
                    uiVirtualPath.SetActive(true);
                    autoWaypointsState = 1;
                }
                goto case 99;
            case 5:
                goto case 4;
            case 99:
                if (uiOperatorState.OperatorPose.position.y != sliderWPHeight.GetValue())
                {
                    uiCursorPosition.SetActive(true);
                    uiVirtualPath.SetActive(true);
                    for (int i = 0; i < uiWaypoints.Count; i++)
                    {
                        uiOperatorState.Waypoints[i] = new Vector3(uiOperatorState.Waypoints[i].x, sliderWPHeight.GetValue(), uiOperatorState.Waypoints[i].z);
                        uiWaypoints[i] = new Vector3(uiWaypoints[i].x, sliderWPHeight.GetValue(), uiWaypoints[i].z);
                    }
                    uiOperatorState.OperatorPose.position = new Vector3(uiOperatorState.OperatorPose.position.x, sliderWPHeight.GetValue(), uiOperatorState.OperatorPose.position.z);
                    if (uiOperatorState.Waypoints.Count > 0)
                    {
                        uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                    }
                    uiOperatorState.Waypoints.Add(uiOperatorState.OperatorPose.position);
                    uiOperatorState.StateChanged = true;
                }
                break;
            case -99:
                uiOperatorState.Waypoints.Clear();
                uiWaypoints.Clear();
                uiVirtualPath.SetActive(true);
                if (autoCircleState <= 0)
                {
                    uiCursorPosition.SetActive(false);
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                }
                autoWaypointsState = 0;
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
#endif