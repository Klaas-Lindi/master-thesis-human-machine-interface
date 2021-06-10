using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script is only a data container for the uav state to use this as Component
/// </summary>
public class UavState : MonoBehaviour, VehicleState { 

    public enum UavMode { WAITING, GUIDANCE_ACTIVE, GUIDANCE_STOP, REBOOT };
    public enum UavCondition { LANDED, FLYING, IDLE, ERROR };
    public enum UavOperationState { TAKEOFF, LANDING, IDLE, DIRECT, RTL, AUTO };
    public enum UavControlState { TELEOPERATION, MANUAL };

    // pose of uav and camera
    private Pose cameraPose = new Pose();
    private Pose vehiclePose = new Pose();
    // States and condition of the uav

    // offset off drone from head
    private Vector3 offset = new Vector3(0, -0.15f, 0);

    // Connection States
    private bool isConnected = false;

    private uint latency_counter = 0;
    private float tmtc_latency = 0.0f;


    public (float newEstimatedLatency, uint number) TMTCLatency
    {
        get
        {
            return (tmtc_latency, latency_counter);
        }

        set
        {
            (tmtc_latency, latency_counter) = (value);
        }
    }

    /// <summary>
    /// Set or Get the current camera pose
    /// </summary>
    public Pose CameraPose
    {
        get
        {
            return cameraPose;
        }

        set
        {
            cameraPose = value;
        }
    }
    /// <summary>
    /// Set or Get the current uav pose
    /// </summary>
    public Pose VehiclePose
    {
        get
        {
            return vehiclePose;
        }

        set
        {
            vehiclePose = value;
        }
    }



    // texture class
    private Texture2D currentFrame;
    /// <summary>
    /// Set or get the screen texture of current frame
    /// </summary>
    public Texture2D CurrentFrame
    {
        get
        {
            return currentFrame;
        }

        set
        {
            currentFrame = value;
        }
    }

    // Current Condition of the UAV
    private UavCondition condition = new UavCondition();
    /// <summary>
    /// Set or get the condition of the UAV
    /// </summary>
    public UavCondition Condition
    {
        get
        {
            return condition;
        }

        set
        {
            condition = value;
        }
    }

    // Current operation state of the uav
    private UavOperationState operationState = new UavOperationState();
    /// <summary>
    /// Set or get the operational state of the uav
    /// </summary>
    public UavOperationState OperationState
    {
        get
        {
            return operationState;
        }

        set
        {
            operationState = value;
        }
    }

    // Meta commands from teleoperator
    private string msg = "";
    /// <summary>
    /// Set or get the operational commands of teleoperator
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

    // State of the guidance 
    private UavMode mode = UavMode.WAITING;
    /// <summary>
    /// Get or set the guidanceActive State
    /// </summary>
    public UavMode Mode
    {
        get
        {
            return mode;
        }

        set
        {
            mode = value;
        }
    }

    // State of the control mode
    private UavControlState controlState = UavControlState.MANUAL;
    /// <summary>
    /// Get or set the control state
    /// </summary>
    public UavControlState ControlState
    {
        get
        {
            return controlState;
        }

        set
        {
            controlState = value;
        }
    }

    // Battery Status of the UAV
    private float battery = 0;
    /// <summary>
    /// Get or set the uav battery capacity
    /// </summary>
    public float Battery
    {
        get
        {
            return battery;
        }

        set
        {
            battery = value;
        }
    }



    // GPS
    private GPS gps = new GPS();
    /// <summary>
    /// Get or set the coordinates of the gps
    /// </summary>
    public GPS Coordinates
    {
        get
        {
            return gps;
        }

        set
        {
            gps = value;
        }
    }

    // GPS
    private GPS initialCoordinate = new GPS();
    /// <summary>
    /// Get or set the coordinates of the gps
    /// </summary>
    public GPS InitialCoordinate
    {
        get
        {
            return initialCoordinate;
        }

        set
        {
            initialCoordinate = value;
        }
    }

    /// <summary>
    /// This Property gives the offset from camera to uav
    /// </summary>
    public Vector3 OffsetCameraUav
    {
        get
        {
            return offset;
        }
    }

    /// <summary>
    /// Set or get the connection state
    /// </summary>
    public bool IsConnected
    {
        get
        {
            return isConnected;
        }

        set
        {
            isConnected = value;
        }
    }


    /// <summary>
    /// Set the position of the vehicle and also automatically updates the camera position
    /// </summary>
    /// <param name="postion">the currenct position of the vehicle</param>
    public void SetVehiclePosition(Vector3 postion)
    {
        this.VehiclePose.position = postion;
        this.CameraPose.position = postion + offset;
    }

    /// <summary>
    /// Set the position of the camera and also automatically updates the vehicle position
    /// </summary>
    /// <param name="postion">the currenct position of the camera</param>
    public void SetCameraPosition(Vector3 postion)
    {
        this.VehiclePose.position = postion - offset;
        this.CameraPose.position = postion;
    }

    /// <summary>
    /// Set the pose of the camera and also automatically updates the uav position
    /// </summary>
    /// <param name="postion">the currenct position of the camera</param>
    public void SetCameraPose(Pose pose)
    {
        this.VehiclePose.position = pose.position - offset;
        this.CameraPose = pose;
    }

}
