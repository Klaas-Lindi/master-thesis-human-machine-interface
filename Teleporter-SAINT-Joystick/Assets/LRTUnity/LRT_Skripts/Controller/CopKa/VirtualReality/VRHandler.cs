using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if HMI_VR_ACTIVE
public class VRHandler : MonoBehaviour
{

    private OperatorState operatorState;
    public UavState uavState;

    [Header("Controller")]
    [Tooltip("The head mounted display of HTC Vive")]
    public GameObject ControllerHead;

    [Tooltip("The right controller in HTC Vive VR")]
    public GameObject controllerRight;
    private VRLaserpointer laserPointer;
    private SteamVR_TrackedObject controller_right;
    private SteamVR_Controller.Device device_right;

    [Tooltip("The left controller in HTC Vive VR")]
    public GameObject controllerLeft;
    private SteamVR_TrackedObject controller_left;
    private SteamVR_Controller.Device device_left;

    [Header("UI")]
    [Tooltip("UI Element to show the Orientation of the operator")]
    public GameObject uiOperatorOrientation;
    [Tooltip("UI Element to show the Position of the operator")]
    public GameObject uiOperatorPosition;
    public GameObject uiCursorPosition;
    [Tooltip("The UI camera to overlay informations")]
    public GameObject uiCamera;
    public GameObject uiAutomationText;
    [Tooltip("The UI Map camera to show map")]
    public GameObject uiMapCamera;

    [Header("UI Controller Left")]
    [Tooltip("UI Element which hovers over left controller")]
    public GameObject uiLeftController;
    [Tooltip("UI Element which shows state, map and stream")]
    public GameObject uiVrMonitor;
    private bool monitorEnable = true;
    [Tooltip("UI Start Menue")]
    public UIVrMenu4Buttons uiStartMenue;

    [Tooltip("UI TakeOff Menue")]
    public UIVrMenu4Buttons uiTakeOffMenue;

    [Tooltip("UI Landing Menue")]
    public UIVrMenu4Buttons uiLandingMenue;

    [Tooltip("UI Automatism Menue")]
    public UIVRMenu7Buttons uiAutomatismMenue;

    [Tooltip("UI Automatism Circle Menue")]
    public UIVRMenu7Buttons uiAutomatismCircleMenue;

    [Tooltip("UI Automatism Waypoint Menue")]
    public UIVRMenu7Buttons uiAutomatismWaypointMenue;

    [Tooltip("UI Automatism Waypoint Menue")]
    public Image uiMapDirection;

    [Header("UI Controller Right")]
    [Tooltip("UI Element which hovers over right controller")]
    public GameObject uiRightController;

    [Tooltip("UI Automatism Height")]
    public UIVrMenu4Buttons uiAutomatismHeight;
    [Tooltip("UI Automatism Height Text")]
    public GameObject uiAutomatismHeightText;

    [Tooltip("UI Automatism Radius")]
    public UIVrMenu4Buttons uiAutomatismRadius;
    [Tooltip("UI Automatism Height Text")]
    public GameObject uiAutomatismRadiusText;


    [Header("Controller Settings")]
    [Tooltip("Minimum and maximum height which can be set")]
    public Vector2 heightRange;
    [Tooltip("Minimum and maximum radius which can be set")]
    public Vector2 radiusRange;

    public float moveYFaktor = 0.25f;

    public float heightFaktorSlide = 0.25f;
    public float radiusFaktorSlide = 0.25f;

    private Vector2 oldAxisValue;
    private bool contiousSlide = false;

    [Tooltip("Sensitivity to threshold to sense a direction in sensitivity")]
    public float touchPadThresholdFromMiddle = 0.5f;

    private float f_key_timer = 0.0f;
    private float backspace_key_timer = 0.0f;
    private float realTime;

    // Automatism variables
    private int autoMode = 0; // 0: no automatism, 1: rotation allowed, 2: fully automatism
    private int autoCircleState = 0; // 0: not active, 1: set postion, 2: set radius 
    private float cursorHeight = 0f;
    private float wheelAxisValue = 0f;

    private int autoWaypointsState = 0; // 0: not active, 1: set postions, 1: set height, 3: end path
    private List<Vector3> uiWaypoints;

    private enum TouchPadSector { TOP, RIGHT, DOWN, LEFT, TopRight, DownRight, DownLeft, TopLeft, TTR, RTR, RDR, DDR, DDL, LDL, LTL, TTL }; // R: RIGHT, T: TOP, L: LEFT, D: DOWN
    private enum MenueLevel { START, TAKEOFF, LANDING, AUTOMATISM, AUTOMATISM_CIRLE, AUTOMATISM_WAYPOINT }

    private MenueLevel menueLevel;

    private OperatorState uiOperatorState;

    private float height = 10;

    // Use this for initialization
    void Start()
    {
        // Controller right
        laserPointer = controllerRight.GetComponent<VRLaserpointer>();
        //laserPointer.init();
        controller_right = controllerRight.GetComponent<SteamVR_TrackedObject>();
        device_right = SteamVR_Controller.Input((int)controller_right.index);

        // Controller left
        controller_left = controllerLeft.GetComponent<SteamVR_TrackedObject>();
        device_left = SteamVR_Controller.Input((int)controller_left.index);

        //uiWaypoints = new List<Vector3>();
        uiOperatorState = uiCursorPosition.GetComponent<OperatorState>();
        uiOperatorState.Waypoints = new List<Vector3>();
        operatorState = this.GetComponent<OperatorState>();
        operatorState.Waypoints = new List<Vector3>();
        uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
        //uiVirtualPosition.GetComponent<OperatorState>() = operatorState;
        //operatorState.Command = "D:REQUEST_STATE";

        //UI controller left
        uiStartMenue.init(true, touchPadThresholdFromMiddle);
        uiTakeOffMenue.init(false, touchPadThresholdFromMiddle);
        uiLandingMenue.init(false, touchPadThresholdFromMiddle);
        uiAutomatismMenue.init(false, touchPadThresholdFromMiddle * 0.5f);
        uiAutomatismCircleMenue.init(false, touchPadThresholdFromMiddle * 0.5f);
        uiAutomatismWaypointMenue.init(false, touchPadThresholdFromMiddle * 0.5f);
        menueLevel = MenueLevel.START;
        monitorEnable = true;
        uiWaypoints = new List<Vector3>();

        //UI controller right
        uiAutomatismHeight.init(false, 1.0f);
        uiAutomatismRadius.init(false, 1.0f);

        // Automatism 
        autoMode = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Update ui camera position
        uiCamera.transform.position = ControllerHead.transform.position; // + this.transform.position;
        uiCamera.transform.rotation = ControllerHead.transform.rotation;// * this.transform.rotation;

        // Update Map
        uiMapDirection.rectTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, -ControllerHead.transform.rotation.eulerAngles.y));

        uiMapCamera.transform.position = new Vector3(ControllerHead.transform.position.x, uiMapCamera.transform.position.y, ControllerHead.transform.position.z);

        // Navigation in the VR-World
        if (device_right.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) && (autoCircleState == 99 || autoCircleState == 0) && (autoWaypointsState != 2))
        {
            laserPointer.changeDistance((device_right.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y + 1) / 2.0f);
            laserPointer.showDistance();
            if (device_right.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
            {
                Vector3 delta = (laserPointer.getPoint() - ControllerHead.transform.position);
                this.transform.position += delta;
                uiMapCamera.transform.position += new Vector3(delta.x, 0, delta.z);
            }
        }
        else
        {
            laserPointer.hideDistance();
        }

        // Set Ui on Controller
        // Set Position
        uiLeftController.transform.position = controllerLeft.transform.position;
        uiLeftController.transform.rotation = controllerLeft.transform.rotation;

        uiRightController.transform.position = controllerRight.transform.position;
        uiRightController.transform.rotation = controllerRight.transform.rotation;

        // Set Start Menue
        switch (menueLevel)
        {
#region START 
            case MenueLevel.START:
                // General buton handling for animation and touchpad             
                handleVRMenue(uiStartMenue);

                // Button allignment
                if (touchPadGetPressDown(TouchPadSector.LEFT))
                {
                    monitorEnable = !monitorEnable;
                    uiVrMonitor.SetActive(monitorEnable);
                }
                if (touchPadGetPressDown(TouchPadSector.RIGHT))
                {
                    switchMenue(uiStartMenue, uiAutomatismMenue);
                    menueLevel = MenueLevel.AUTOMATISM;
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN))
                {
                    if (uavState.OperationState == UavState.UavOperationState.IDLE)
                    {
                        operatorState.Command = TORCommand.VehicleActive;
                    }
                }
                if (touchPadGetPressDown(TouchPadSector.TOP))
                {
                    if (uavState.Condition == UavState.UavCondition.LANDED)
                    {
                        switchMenue(uiStartMenue, uiTakeOffMenue);
                        menueLevel = MenueLevel.TAKEOFF;
                    }
                    else if (uavState.Condition == UavState.UavCondition.FLYING)
                    {
                        switchMenue(uiStartMenue, uiLandingMenue);
                        menueLevel = MenueLevel.LANDING;
                    }
                }
                break;
#endregion

#region TAKEOFF
            case MenueLevel.TAKEOFF:
                // General buton handling
                handleVRMenue(uiTakeOffMenue);

                // Button allignment
                if (touchPadGetPressDown(TouchPadSector.LEFT))
                {
                    operatorState.Command = TORCommand.VehicleTakeOff;
                    switchMenue(uiTakeOffMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN))
                {
                    switchMenue(uiTakeOffMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                break;
#endregion

#region LANDING
            case MenueLevel.LANDING:
                // General buton handling
                handleVRMenue(uiLandingMenue);
                // Button allignment
                if (touchPadGetPressDown(TouchPadSector.LEFT))
                {
                    operatorState.Command = TORCommand.VehicleLand;
                    switchMenue(uiLandingMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                if (touchPadGetPressDown(TouchPadSector.RIGHT))
                {
                    operatorState.Command = TORCommand.VehicleRTL;
                    switchMenue(uiLandingMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN))
                {
                    switchMenue(uiLandingMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                break;
#endregion

#region AUTOMATISM
            case MenueLevel.AUTOMATISM:
                // General buton handling
                handleVRMenue(uiAutomatismMenue);

                // Button Alligment
                if (touchPadGetPressDown(TouchPadSector.RTR)) // AUTOMATISM CIRCLE BUTTON
                {
                    // Set Menu
                    switchMenue(uiAutomatismMenue, uiAutomatismCircleMenue);
                    menueLevel = MenueLevel.AUTOMATISM_CIRLE;

                    //
                    //!isUiAutomatismActive()
                    // Start autoCircleRoutine
                    autoCircleState = 1;
                    uiCursorPosition.SetActive(true);

                    // Postition of groundplane intersection
                    Vector3 direction = controllerRight.transform.TransformDirection(Vector3.forward);
                    float factor = -controllerRight.transform.position.y / direction.y;
                    uiOperatorState.OperatorPose.position = controllerRight.transform.position + factor * direction + new Vector3(0, 0f, 0f);

                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.AUTOSCAN_CIRCLE);

                    // Show UI on Right Controller
                    uiAutomatismHeight.gameObject.SetActive(true);
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                    uiAutomatismRadius.gameObject.SetActive(true);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                }
                if (touchPadGetPressDown(TouchPadSector.RDR))// AUTOMATISM WAYPOINT BUTTON
                {
                    // Set Menu
                    switchMenue(uiAutomatismMenue, uiAutomatismWaypointMenue);
                    menueLevel = MenueLevel.AUTOMATISM_WAYPOINT;

                    // Set settings for automatism waypoint

                    autoWaypointsState = 1;
                    uiCursorPosition.SetActive(true);

                    // Postition of groundplane intersection
                    Vector3 direction = controllerRight.transform.TransformDirection(Vector3.forward);
                    float factor = -controllerRight.transform.position.y / direction.y;
                    uiOperatorState.OperatorPose.position = controllerRight.transform.position + factor * direction + new Vector3(0, 0f, 0f);

                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.WAYPOINT);

                    // Show UI on Right Controller
                    // uiAutomatismHeight.gameObject.SetActive(true);
                    // uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN)) // BACK BUTTON
                {
                    switchMenue(uiAutomatismMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                break;
#endregion

#region AUTOMATISM_CIRCLE
            case MenueLevel.AUTOMATISM_CIRLE:
                // General buton handling
                handleVRMenue(uiAutomatismCircleMenue);
                handleAutoCircleRoutine();
                // button allignmet
                if (touchPadGetPressDown(TouchPadSector.LTL)) // CONFIRM SETTINGS BUTTON
                {
                    // Set Operation back
                    //operatorState.OperatorPose.position = uiOperatorState.OperatorPose.position;
                    //operatorState.RadiusAutoCircle = uiOperatorState.RadiusAutoCircle;
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    uiCursorPosition.SetActive(false);
                    autoCircleState = 0;
                    autoMode = 1;

                    // Set command
                    operatorState.Command = TORCommand.VehicleAutoCircle(uiOperatorState.RadiusAutoCircle, uiOperatorState.OperatorPose.position);

                    // Change UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    switchMenue(uiAutomatismCircleMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                if (touchPadGetPressDown(TouchPadSector.TTR)) // SET POSITION
                {
                    // Set handler state
                    autoCircleState = 2;

                    // Change UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }
                if (touchPadGetPressDown(TouchPadSector.RTR)) // SET HEIGHT
                {
                    // Set handler state
                    autoCircleState = 3;

                    // Change UI
                    uiAutomatismHeight.gameObject.SetActive(true);
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }
                if (touchPadGetPressDown(TouchPadSector.RDR)) // SET RADIUS
                {
                    // Set handler state
                    autoCircleState = 4;

                    // Change UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    uiAutomatismRadius.gameObject.SetActive(true);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN)) // BACK
                {
                    // Reset State
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    uiCursorPosition.SetActive(false);
                    autoCircleState = 0;

                    // Change UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    switchMenue(uiAutomatismCircleMenue, uiAutomatismMenue);
                    menueLevel = MenueLevel.AUTOMATISM;
                }
                break;
#endregion

#region AUTOMATISM_WAYPOINT
            case MenueLevel.AUTOMATISM_WAYPOINT:
                // General buton handling
                handleVRMenue(uiAutomatismWaypointMenue);
                handleAutoWaypointRoutine();

                // Button alligment
                if (touchPadGetPressDown(TouchPadSector.LTL)) // CONFIRM SETTINGS
                {
                    // Command absetzen
                    if (uiOperatorState.Waypoints.Count > 0)
                    {
                        operatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1];

                        // Set command for UAV
                        operatorState.Command = TORCommand.VehicleAutoWaypoints(uiWaypoints);
                        uiWaypoints.Clear();
                        autoMode = 1;
                    }

                    // Reset State
                    uiOperatorState.Waypoints.Clear();
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    uiCursorPosition.SetActive(false);
                    autoWaypointsState = 0;
                    
                    // Set UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    switchMenue(uiAutomatismWaypointMenue, uiStartMenue);
                    menueLevel = MenueLevel.START;
                }
                if (touchPadGetPressDown(TouchPadSector.TTL)) // DELETE LAST WAYPOINT
                {
                    // Remove last point

                    if (uiWaypoints.Count > 0)
                    {
                        uiWaypoints.RemoveAt(uiWaypoints.Count - 1);
                        uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                        if (uiWaypoints.Count == 0)
                        {
                            autoWaypointsState = 1;
                        }
                        else
                        {
                            autoWaypointsState = 99;
                            uiOperatorState.OperatorPose.position = uiWaypoints[uiWaypoints.Count - 1];
                        }
                    }
                    
                    // Set UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }
                if (touchPadGetPressDown(TouchPadSector.TTR)) // SET NEW WAYPOINT
                {
                    autoWaypointsState = 1;
                    // Set UI
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }
                if (touchPadGetPressDown(TouchPadSector.RTR)) // SET HEIGHT OF WAYPOINT
                {
                    if(uiOperatorState.Waypoints.Count > 0)
                        uiOperatorState.OperatorPose.position = uiOperatorState.Waypoints[uiOperatorState.Waypoints.Count - 1];
                    autoWaypointsState = 2;
                    uiAutomatismHeight.gameObject.SetActive(true);
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
                }
                if (touchPadGetPressDown(TouchPadSector.DOWN)) // BACK
                {
                    // Reset State
                    uiOperatorState.Waypoints.Clear();
                    uiCursorPosition.GetComponent<UiCursor>().SetUICursorMode(UiCursor.UICursorMode.NONE);
                    uiCursorPosition.SetActive(false);
                    autoWaypointsState = 0;

                    // Set menu
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    switchMenue(uiAutomatismWaypointMenue, uiAutomatismMenue);
                    menueLevel = MenueLevel.AUTOMATISM;
                }
                break;
#endregion
        }

        // Handle idle/pause mode
        if (touchPadGetPress(TouchPadSector.DOWN))
        {
            if (this.f_key_timer > 2)
            {
                autoMode = 0;
                operatorState.Command = TORCommand.VehicleIdle;
                this.f_key_timer = -100;
            }
            else
            {
                this.f_key_timer += (Time.realtimeSinceStartup - realTime);
            }
        }
        if (touchPadGetPressUp(TouchPadSector.DOWN))
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
                        uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
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

#region Keyboard Inputs
        // RTL
        if (Input.GetKeyDown(KeyCode.R))
        {
            operatorState.Command = TORCommand.VehicleRTL;
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
#endregion


        // Handle direct control
        if (!this.GetComponent<FlyingScript>().SkriptEnabled && !operatorState.ReadFromLog && !isUiAutomatismActive()) // Handle Script
        {
            // Get pose from camera
            if (device_right.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
            {
                // Get allowed changes
                Pose tmp = new Pose();
                if (autoMode < 1)
                {
                    tmp.position = ControllerHead.transform.position;
                }
                else
                {
                    tmp.position = operatorState.OperatorPose.position;
                }
                if (autoMode < 2)
                {
                    tmp.rotation = ControllerHead.transform.rotation;
                }
                else
                {
                    tmp.rotation = operatorState.OperatorPose.rotation;
                }

                // Set parameters into operator state
                operatorState.OperatorPose = tmp;
                operatorState.VehicleActive = true;
            }
        }

        //Get real time form system
        realTime = Time.realtimeSinceStartup;
        //print(operatorState.Command);

    }

    private bool isUiAutomatismActive()
    {
        return (autoCircleState > 0 || autoWaypointsState > 0);
    }


#region Controller functions
    private bool touchPadGetPressDown(TouchPadSector sector)
    {
        if (device_left.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            return touchPadGetSektor(sector);
        }

        return false;
    }

    private bool touchPadGetPress(TouchPadSector sector)
    {
        if (device_left.GetPress(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            return touchPadGetSektor(sector);
        }

        return false;
    }

    private bool touchPadGetPressUp(TouchPadSector sector)
    {
        if (device_left.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            return touchPadGetSektor(sector);
        }

        return false;
    }

    private bool touchPadGetSektor(TouchPadSector sector)
    {
        Vector2 position = device_left.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);

        if (position.magnitude >= touchPadThresholdFromMiddle)
        {
            float phi = ((Mathf.Rad2Deg * Mathf.Atan2(position.y, position.x)) + 360) % 360;

            // Return state
            switch (sector)
            {
                // 4 normal sectors
                case TouchPadSector.TOP: return ((phi <= 135) && (phi >= 45));
                case TouchPadSector.RIGHT: return ((phi <= 45) && (phi >= 0)) || ((phi <= 360) && (phi >= 315));
                case TouchPadSector.DOWN: return ((phi <= 315) && (phi >= 225));
                case TouchPadSector.LEFT: return ((phi <= 225) && (phi >= 135));

                // 45-degreed shift sectors
                case TouchPadSector.TopRight: return ((phi <= 90) && (phi >= 0));
                case TouchPadSector.TopLeft: return ((phi <= 180) && (phi >= 90));
                case TouchPadSector.DownLeft: return ((phi <= 270) && (phi >= 180));
                case TouchPadSector.DownRight: return ((phi <= 360) && (phi >= 270));

                // 8 normal sectors
                case TouchPadSector.RTR: return ((phi <= 45) && (phi >= 0));
                case TouchPadSector.TTR: return ((phi <= 90) && (phi >= 45));
                case TouchPadSector.TTL: return ((phi <= 135) && (phi >= 90));
                case TouchPadSector.LTL: return ((phi <= 180) && (phi >= 135));
                case TouchPadSector.LDL: return ((phi <= 225) && (phi >= 180));
                case TouchPadSector.DDL: return ((phi <= 270) && (phi >= 225));
                case TouchPadSector.DDR: return ((phi <= 315) && (phi >= 270));
                case TouchPadSector.RDR: return ((phi <= 360) && (phi >= 315));
            }
        }
        return false;
    }

    private float touchPadSlider(float abs_value, float delta, Vector2 range, float SlideFactor)
    {
        contiousSlide = true;

        float new_value = abs_value - delta * SlideFactor;
        if (new_value < range.x)
            return range.x;
        if (new_value > range.y)
            return range.y;

        return new_value;
    }
#endregion


    private void switchMenue(UIVrMenuButtons from, UIVrMenuButtons to)
    {
        to.gameObject.SetActive(true);
        //from.gameObject.SetActive(false);
        to.GetComponent<UiVrMenuAnimation>().setAnimationActive(true);
        from.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
    }

#region Handler functions
    private void handleVRMenue(UIVrMenuButtons uiMenue4Buttons)
    {
        if (device_left.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
        {
            uiMenue4Buttons.setTouchTouched(true);
            uiMenue4Buttons.setTouchPosition(device_left.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad));
        }
        else
        {
            uiMenue4Buttons.setTouchTouched(false);
        }
    }

    private void handleAutoCircleRoutine()
    {
        Vector2 touchAxis = device_right.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);

        if (touchAxis == Vector2.zero || !contiousSlide)
        {
            oldAxisValue = touchAxis;
            contiousSlide = false;
        }


        Vector3 direction;
        float factor;
        switch (autoCircleState)
        {

            case 1:
                direction = controllerRight.transform.TransformDirection(Vector3.forward);
                factor = -controllerRight.transform.position.y / direction.y;
                uiOperatorState.OperatorPose.position = controllerRight.transform.position + factor * direction;
                uiOperatorState.OperatorPose.position += new Vector3(0, height, 0f);
                uiOperatorState.StateChanged = true;

                if (device_right.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    // Set Radius
                    uiOperatorState.RadiusAutoCircle = touchPadSlider(uiOperatorState.RadiusAutoCircle, oldAxisValue.x - touchAxis.x, radiusRange, radiusFaktorSlide);

                    // Set Height 
                    height = touchPadSlider(height, oldAxisValue.y - touchAxis.y, heightRange, heightFaktorSlide);

                    uiAutomatismHeightText.GetComponent<UiTextAnimation>().startDeclineAnimation();
                    uiAutomatismRadiusText.GetComponent<UiTextAnimation>().startDeclineAnimation();
                }

                goto case 99;

            case 2:
                direction = controllerRight.transform.TransformDirection(Vector3.forward);
                factor = -controllerRight.transform.position.y / direction.y;
                uiOperatorState.OperatorPose.position = controllerRight.transform.position + factor * direction;
                uiOperatorState.OperatorPose.position += new Vector3(0, height, 0f);
                uiOperatorState.StateChanged = true;

                goto case 99;
            case 3:
                if (device_right.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    // Set Height 
                    height = touchPadSlider(height, oldAxisValue.y - touchAxis.y, heightRange, heightFaktorSlide);
                    uiOperatorState.OperatorPose.position.y = height;
                    uiOperatorState.StateChanged = true;
                    uiAutomatismHeightText.GetComponent<UiTextAnimation>().startDeclineAnimation();
                }
                goto case 99;
            case 4:
                if (device_right.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    // Set Radius
                    uiOperatorState.RadiusAutoCircle = touchPadSlider(uiOperatorState.RadiusAutoCircle, oldAxisValue.x - touchAxis.x, radiusRange, radiusFaktorSlide);
                    uiOperatorState.StateChanged = true;
                    uiAutomatismRadiusText.GetComponent<UiTextAnimation>().startDeclineAnimation();

                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }
                goto case 99;
            case 99:

                if (device_right.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    uiAutomatismRadius.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                    autoCircleState = 99;
                }
                // Set Text
                uiAutomatismRadiusText.GetComponentInChildren<Text>().text =
                    "Radius: " + uiOperatorState.RadiusAutoCircle.ToString("0.00") + " m";
                uiAutomatismHeightText.GetComponentInChildren<Text>().text =
                    "Höhe  : " + uiOperatorState.OperatorPose.position.y.ToString("0.00") + " m";
                break;
        }

        oldAxisValue = touchAxis;
    }

    private void handleAutoWaypointRoutine()
    {
        Vector2 touchAxis = device_right.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);

        if (touchAxis == Vector2.zero || !contiousSlide)
        {
            oldAxisValue = touchAxis;
            contiousSlide = false;
        }


        Vector3 direction;
        float factor;

        // Handle Waypoints
        switch (autoWaypointsState)
        {
            case 1: // Normal routine
                direction = controllerRight.transform.TransformDirection(Vector3.forward);
                factor = -controllerRight.transform.position.y / direction.y;
                uiOperatorState.OperatorPose.position = controllerRight.transform.position + factor * direction + new Vector3(0f, 0f, 0f);

                uiOperatorState.OperatorPose.position += new Vector3(0, height, 0f);
                if (uiOperatorState.Waypoints.Count > 0)
                {
                    uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                }
                uiOperatorState.Waypoints.Add(uiOperatorState.OperatorPose.position);
                uiOperatorState.StateChanged = true;

                if (device_right.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    uiWaypoints.Add(uiOperatorState.OperatorPose.position);
                    uiOperatorState.Waypoints.Add(uiOperatorState.OperatorPose.position);
                }

                break;
            case 2: // Set Height
                if (device_right.GetTouch(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))
                {
                    // Set Height 
                    height = touchPadSlider(height, oldAxisValue.y - touchAxis.y, heightRange, heightFaktorSlide);

                    for (int i = 0; i < uiWaypoints.Count; i++)
                    {
                        uiOperatorState.Waypoints[i] = new Vector3(uiOperatorState.Waypoints[i].x, height, uiOperatorState.Waypoints[i].z);
                        uiWaypoints[i] = new Vector3(uiWaypoints[i].x, height, uiWaypoints[i].z);
                    }
                    uiOperatorState.OperatorPose.position = new Vector3(uiOperatorState.OperatorPose.position.x, height, uiOperatorState.OperatorPose.position.z);

                    if (uiOperatorState.Waypoints.Count > 0)
                    {
                        uiOperatorState.Waypoints.RemoveAt(uiOperatorState.Waypoints.Count - 1);
                    }
                    uiOperatorState.Waypoints.Add(uiOperatorState.OperatorPose.position);
                    uiOperatorState.StateChanged = true;

                    uiAutomatismHeightText.GetComponent<UiTextAnimation>().startDeclineAnimation();
                }

                if (device_right.GetPressDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
                {
                    autoWaypointsState = 99;
                    uiAutomatismHeight.GetComponent<UiVrMenuAnimation>().setAnimationActive(false);
                }

                uiAutomatismHeightText.GetComponentInChildren<Text>().text =
                    "Höhe  : " + uiOperatorState.OperatorPose.position.y.ToString("0.00") + " m";
                break;

            case 99:
                
                break;

               
        }
        oldAxisValue = touchAxis;
    }
#endregion
}
#endif