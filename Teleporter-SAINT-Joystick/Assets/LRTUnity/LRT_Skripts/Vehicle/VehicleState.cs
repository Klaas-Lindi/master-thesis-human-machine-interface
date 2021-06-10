using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script is only a data container for the uav state to use this as Component
/// </summary>
public interface VehicleState
{
   
    Pose CameraPose{get;set;}
    /// <summary>
    /// Set or Get the current uav pose
    /// </summary>
    Pose VehiclePose { get; set; }

    // texture class
    /// <summary>
    /// Set or get the screen texture of current frame
    /// </summary>
    Texture2D CurrentFrame { get; set; }


    // Meta commands from teleoperator
    /// <summary>
    /// Set or get the operational commands of teleoperator
    /// </summary>
    string Msg { get; set; }

    // Battery Status of the UAV
    /// <summary>
    /// Get or set the uav battery capacity
    /// </summary>
    float Battery { get; set; }

    // GPS
    /// <summary>
    /// Get or set the coordinates of the gps
    /// </summary>
    GPS Coordinates { get; set; }

    // GPS
    /// <summary>
    /// Get or set the coordinates of the gps
    /// </summary>
    GPS InitialCoordinate { get; set; }

    /// <summary>
    /// This Property gives the offset from camera to uav
    /// </summary>
    Vector3 OffsetCameraUav { get; }

    /// <summary>
    /// Set or get the connection state
    /// </summary>
    bool IsConnected { get; set; }


    /// <summary>
    /// Set the position of the vehicle and also automatically updates the camera position
    /// </summary>
    /// <param name="postion">the currenct position of the vehicle</param>
    void SetVehiclePosition(Vector3 postion);

    /// <summary>
    /// Set the position of the camera and also automatically updates the vehicle position
    /// </summary>
    /// <param name="postion">the currenct position of the camera</param>
    void SetCameraPosition(Vector3 postion);

    /// <summary>
    /// Set the pose of the camera and also automatically updates the uav position
    /// </summary>
    /// <param name="postion">the currenct position of the camera</param>
    void SetCameraPose(Pose pose);
}

public class Pose
{
    public int state;
    public Vector3 position;
    public Quaternion rotation;

    public Pose()
    {
        state = new int();
        position = new Vector3();
        rotation = new Quaternion();
        rotation = Quaternion.identity;
    }

    public Pose Clone()
    {
        Pose res = new Pose();
        res.state = this.state;
        res.position = this.position;
        res.rotation = this.rotation;
        return res;
    }
}

public class GPS
{
    public float latitude;
    public float longitude;
    public float height;

    public GPS()
    {
        latitude = 0;
        longitude = 0;
        height = 0;
    }
}
