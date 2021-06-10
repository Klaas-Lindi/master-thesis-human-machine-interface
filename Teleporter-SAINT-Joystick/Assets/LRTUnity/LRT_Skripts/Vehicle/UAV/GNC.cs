using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

public class GNC : MonoBehaviour {

    // Current state of the operator
    public OperatorState operatorState;

    [Header("Classes to share state or command")]
    [Tooltip("extPointCloudLoader needs the gps coordinates of the uav")]
    public LoadPointCloud extPointCloud;


    // Current state of current uav
    private UavState uavState;

    // pose of the GNC to commad
    private Pose gncPose = new Pose();

    // pose of idle 
    private Pose idlePose = new Pose();

    /// <summary>
    /// Set or get the GNC pose to command
    /// </summary>
    public Pose GNCPose
    {
        get
        {
            return gncPose;
        }

        set
        {
            gncPose = value;
        }
    }

    // Meta commands from gnc to command
    private string command = "";
    /// <summary>
    /// Set or get the operational commands of gnc
    /// </summary>
    public string Command
    {
        get
        {
            return command;
        }

        set
        {
            command = value;
        }
    }

    // Meta msg from the teleoperartor
    private string msg = "";
    /// <summary>
    /// Set or get the operational commands of gnc
    /// </summary>
    public string Msg
    {
        get
        {
            return msg;
        }

        set
        {
            msg = value;
        }
    }

    // Latency measurement variables
    // flag for indicating running latency measurements
    private bool measurementRunning = false;
    private uint latency_counter = 1;
    private double startLatencyTime;
    private float measuredLatency;
    private double latency_timeout = 10000; // 10 s 



    // Use this for initialization
    void Start () {
        // Get current uav state
        uavState = this.GetComponent<UavState>();
        idlePose.position = new Vector3(0, 10, 0);
        idlePose.rotation = new Quaternion();

        operatorState.ActiveAutomatism = OperatorState.Automatism.NONE;

        // Set String format globaly
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
    }
	
	// Update is called once per frame
	void Update () {
        bool accept = false;
        if(operatorState.Command.Length == 0 && !this.measurementRunning)
        {
            if(((DateTime.Now.TimeOfDay.TotalMilliseconds - this.startLatencyTime) > 30) && uavState.IsConnected) // request tmtc latency every 30 ms when possible
                this.Command = startTMTCLatencyMeasurement();
        }
        foreach (string cmd in operatorState.Command.Split('\n'))
        {
            switch (cmd)
            {
                case "D:IDLE":
                    uavState.OperationState = UavState.UavOperationState.IDLE;
                    operatorState.ActiveAutomatism = OperatorState.Automatism.NONE;
                    idlePose.position = uavState.CameraPose.position;
                    idlePose.rotation = uavState.CameraPose.rotation;
                    operatorState.StateChanged = true;
                    goto case "accept";
                case "D:ACTIVE":
                    uavState.OperationState = UavState.UavOperationState.DIRECT;
                    operatorState.StateChanged = true;
                    operatorState.ActiveAutomatism = OperatorState.Automatism.NONE;
                    goto case "accept";
                case "D:TAKEOFF":
                    operatorState.ActiveAutomatism = OperatorState.Automatism.UAV_TAKEOFF;
                    goto case "accept";
                case "D:LAND":
                    operatorState.ActiveAutomatism = OperatorState.Automatism.UAV_LANDING;
                    operatorState.StateChanged = true;
                    goto case "accept";
                case "T:START_GUIDANCE":
                    goto case "accept";
                case "T:STOP_GUIDANCE":
                    goto case "accept";
                case "D:AUTO:POI":
                    operatorState.ActiveAutomatism = OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE;
                    operatorState.StateChanged = true;
                    goto case "accept";
                case "D:RTL":
                    operatorState.ActiveAutomatism = OperatorState.Automatism.UAV_RTL;
                    operatorState.StateChanged = true;
                    goto case "accept";
                case "D:REQUEST_STATE":
                    goto case "accept";
                case "D:REQUEST_INIT_POINT":
                    goto case "accept";
                case ":REBOOT": //Not working at the moment
                    goto case "accept";
                /*case "D:FREEZE":
                    uavState.OperationState = UavState.UavOperationState.IDLE;
                    operatorState.ActiveAutomatism = OperatorState.Automatism.NONE;
                    operatorState.StateChanged = true;
                    idlePose.position = uavState.CameraPose.position;
                    idlePose.rotation = uavState.CameraPose.rotation;
                    goto case "accept";*/
                case "accept":
                    this.Command = operatorState.Command;
                    operatorState.Command = "";
                    accept = true;
                    break;
            }
            if (!accept && cmd.Length > 0 && cmd.Contains(":"))
            {
                string identifier = cmd.Substring(0, cmd.LastIndexOf(":"));
                string[] values = cmd.Substring(cmd.LastIndexOf(':') + 1).Split(',');
                switch (identifier)
                {
                    // HANDLE STATES MESSAGE
                    case "D:AUTO:POI":
                        operatorState.ActiveAutomatism = OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE_SET_POINT;
                        operatorState.StateChanged = true;
                        print(values.Length);
                        if(values.Length == 4)
                        {
                            operatorState.OperatorPose.position = new Vector3(float.Parse(values[2]), float.Parse(values[3]), float.Parse(values[1]));
                            operatorState.RadiusAutoCircle = float.Parse(values[0]);
                        }

                        goto case "accept";
                    case "D:AUTO:WAYPOINTS":
                        operatorState.ActiveAutomatism = OperatorState.Automatism.WAYPOINTS;
                        operatorState.Waypoints.Clear();
                        for (int i = 0; i < values.Length; i+=3)
                        {
                            operatorState.Waypoints.Add(new Vector3(float.Parse(values[i+1]), float.Parse(values[i + 2]), float.Parse(values[i])));
                        }
                        operatorState.StateChanged = true;
                        goto case "accept";
                    case "accept":
                        this.Command = operatorState.Command;
                        operatorState.Command = "";
                        accept = true;
                        break;
                }
            }
        }

        // Handle msg from teleoperator cotrol
        accept = false;

        foreach (string msg in uavState.Msg.Split('\n'))
        {
            accept = false;
            switch (msg)
            {
                // CONTROL MESSAGES
                case ":LIFE":
                    this.Command = "O:LIFE";
                    goto case "accept";

                // OPERATION CYCLES
                // --TAKEOFF
                case "T:ACK:TAKEOFF":
                    uavState.OperationState = UavState.UavOperationState.TAKEOFF;
                    goto case "accept";
                case "T:SUCCESS:TAKEOFF":
                    //print("Works! " + uavState.Msg);
                    uavState.Condition = UavState.UavCondition.FLYING;
                    uavState.OperationState = UavState.UavOperationState.DIRECT;
                    operatorState.ActiveAutomatism = OperatorState.Automatism.NONE;
                    goto case "accept";
                // --LAND
                case "T:ACK:LAND":
                    //uavState.OperationState = UavState.UavOperationState.LANDING;
                    goto case "accept";
                case "T:SUCCESS:LAND":
                    //uavState.Condition = UavState.UavCondition.LANDED;
                    uavState.OperationState = UavState.UavOperationState.LANDING;
                    goto case "accept";
                // --GUIDANCE
                case "T:SUCCESS:START_GUIDANCE":
                    uavState.Mode = UavState.UavMode.GUIDANCE_ACTIVE;
                    this.GetComponent<TelemetryAndTelecommand>().initializeGuidanceConnection();
                    goto case "accept";
                case "T:ACK:STOP_GUIDANCE":
                    uavState.Mode = UavState.UavMode.GUIDANCE_STOP;
                    this.GetComponent<TelemetryAndTelecommand>().closeGuidanceConnection();
                    goto case "accept";
                case "T:SUCCESS:STOP_GUIDANCE":
                    uavState.Mode = UavState.UavMode.WAITING;
                    goto case "accept";
                case "T:SUCCESS:IDLE":
                    uavState.Condition = UavState.UavCondition.IDLE;
                    goto case "accept";
                // --DIRECT CONTROL
                case "T:SUCCESS:DIRECT":
                    uavState.Condition = UavState.UavCondition.FLYING;
                    goto case "accept";
                case "T:FAIL:DIRECT":
                    uavState.OperationState = UavState.UavOperationState.IDLE;
                    goto case "accept";
                // --AUTOSCAN
                case "T:ACK:AUTO":
                    uavState.OperationState = UavState.UavOperationState.AUTO; //Redundancy to success 
                    goto case "accept";
                case "T:SUCCESS:AUTO":
                    uavState.OperationState = UavState.UavOperationState.AUTO; 
                    goto case "accept";
                case "T:FAIL:AUTO":
                    idlePose.position = uavState.CameraPose.position;
                    idlePose.rotation = uavState.CameraPose.rotation;
                    uavState.OperationState = UavState.UavOperationState.IDLE;
                    goto case "accept";
                // --RTL
                case "T:ACK:RTL":
                    uavState.OperationState = UavState.UavOperationState.RTL;
                    goto case "accept";
                case "T:SUCCESS:RTL":
                    //idlePose.position = uavState.CameraPose.position;
                    //idlePose.rotation = uavState.CameraPose.rotation;
                    uavState.OperationState = UavState.UavOperationState.LANDING;
                    //uavState.OperationState = UavState.UavOperationState.IDLE;
                    goto case "accept";

                // REBOOT
                case "T:ACK:REBOOT":
                    if (uavState.Mode != UavState.UavMode.REBOOT)
                    {
                        uavState.Mode = UavState.UavMode.REBOOT;
                        StartCoroutine(RebootRoutine());
                    } 
                    goto case "accept";

                case "accept":
                    accept=true;
                    break;
            }

            if (!accept && msg.Length > 0 && msg.Contains(":"))
            {
                string identifier = msg.Substring(0, msg.LastIndexOf(":"));
                string[] values;
                switch (identifier)
                {
                    // HANDLE STATES MESSAGE
                    case "T:STATE":
                        values = msg.Substring(msg.LastIndexOf(':')+1).Split(',');
                        switch (values[0])
                        {
                            case "AUTO":
                                uavState.Condition = UavState.UavCondition.FLYING;
                                uavState.OperationState = UavState.UavOperationState.AUTO;
                                break;
                            case "IDLE":
                                uavState.Condition = UavState.UavCondition.IDLE;
                                uavState.OperationState = UavState.UavOperationState.IDLE;
                                break;
                            case "LANDED":
                                uavState.Condition = UavState.UavCondition.LANDED;
                                uavState.OperationState = UavState.UavOperationState.IDLE;
                                break;
                            case "DIRECT":
                                uavState.Condition = UavState.UavCondition.FLYING;
                                uavState.OperationState = UavState.UavOperationState.DIRECT;
                                break;
                        }
                        switch (values[1])
                        {

                            case "MANUAL":
                                uavState.ControlState = UavState.UavControlState.MANUAL;
                                break;
                            case "TELEOP":
                                uavState.ControlState = UavState.UavControlState.TELEOPERATION;
                                break;
                        }
                        uavState.Battery = float.Parse(values[2]);
                        uavState.Coordinates.latitude = float.Parse(values[3]);
                        uavState.Coordinates.longitude = float.Parse(values[4]);
                        uavState.Coordinates.height = float.Parse(values[5]);

                        // Share Parameters with other classes
                        //if(extPointCloud.enabled == true && uavState.Condition == UavState.UavCondition.LANDED)
                        //    extPointCloud.Reload(uavState.Coordinates);

                        goto case "accept";
                    case "T:INIT_POINT":
                        values = msg.Substring(msg.LastIndexOf(':') + 1).Split(',');

                        uavState.InitialCoordinate.latitude = float.Parse(values[0]);
                        uavState.InitialCoordinate.longitude = float.Parse(values[1]);
                        uavState.InitialCoordinate.height = float.Parse(values[2]);

                        // Share Parameters with other classes
                        if (extPointCloud.enabled == true)
                            extPointCloud.Reload(uavState.InitialCoordinate);

                        goto case "accept";

                    case "T:LM": // Result of latency Measurement
                        endTMTCLatencyMeasurement(msg.Substring(msg.LastIndexOf(':') + 1));
                        goto case "accept";

                    case "accept":
                        accept = true;
                        break;
                }
            }
        }

        uavState.Msg = "";

        // Handle guidance mode
        if (uavState.Mode == UavState.UavMode.GUIDANCE_ACTIVE)
        {
            if (uavState.OperationState == UavState.UavOperationState.IDLE)
            {
                this.GNCPose.position = idlePose.position - uavState.OffsetCameraUav;
                this.GNCPose.rotation = idlePose.rotation;
            }
            else if (uavState.OperationState == UavState.UavOperationState.DIRECT)
            {
                this.GNCPose.position = operatorState.OperatorPose.position - uavState.OffsetCameraUav;
                this.GNCPose.rotation = operatorState.OperatorPose.rotation;
            }
            else
            {
                this.GNCPose.position = uavState.CameraPose.position - uavState.OffsetCameraUav; 
                this.GNCPose.rotation = uavState.CameraPose.rotation;
            }
        }

        // Handle timeout of TMTC latency measurement
        if((DateTime.Now.TimeOfDay.TotalMilliseconds - this.startLatencyTime) > this.latency_timeout)
        {
            print("TMTC Latency Measurement Timeout! Nr. " + (this.latency_counter-1));
            this.measurementRunning = false;
        }
    }

    private string startTMTCLatencyMeasurement()
    {

        string lmCommand = "O:LM:" + this.latency_counter;// + ",";
                 

        // Save time
        this.startLatencyTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

        //lmCommand += this.startLatencyTime.ToString();

        // print("TMTC Latency Measurement: " + lmCommand);

        this.measurementRunning = true;
        this.latency_counter++;

        return lmCommand;
    }

    private void endTMTCLatencyMeasurement(string lmMessage)
    {

        string[] values = lmMessage.Split(',');

        if (int.Parse(values[0]) != (this.latency_counter - 1))
        {
            print("TMTC Latency Measurement: Wrong counter number! Nr. " + values[0]);
            return;
        }

        // Save latency
        this.measuredLatency = (float)(DateTime.Now.TimeOfDay.TotalMilliseconds - this.startLatencyTime);
                
        //print("TMTC Latency Measurement (TCP) Nr. " + (this.latency_counter-1) + ": " + this.measuredLatency);

        uavState.TMTCLatency = (this.measuredLatency/2, (this.latency_counter - 1));

        this.measurementRunning = false;
    }

    // every 2 seconds perform the print()
    private IEnumerator RebootRoutine()
    {
        yield return new WaitForSecondsRealtime(5);

        this.GetComponent<TelemetryAndTelecommand>().closeGuidanceConnection();
        this.GetComponent<TelemetryAndTelecommand>().closeTeleoperator();
        this.GetComponent<TelemetryAndTelecommand>().connectTeleoperator();

        uavState.Mode = UavState.UavMode.WAITING;
    }
}
