
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This script lists all possible commands. TORCommands is a base class which handles all common commands. These commands are equal to the copter commands, because this was the first commands
/// </summary>
public class TORCommand 
{
    // General Commands
    static public string VehicleRequestState     { get => "D:REQUEST_STATE"; }
    static public string VehicleRequestInitPoint { get => "D:REQUEST_INIT_POINT"; }
    static public string SystemReboot            { get => ":REBOOT"; }

    // Guidance Commands
    static public string TMTCStartGuidance  { get => "T:START_GUIDANCE"; }
    static public string TMTCStopGuidance   { get => "T:STOP_GUIDANCE"; }
    static public string VehicleActive      { get => "D:ACTIVE"; }
    static public string VehicleIdle        { get => "D:IDLE"; }
    //static public string VehicleFreeze      { get => "D:FREEZE"; }

    // TakeOff / Landing
    static public string VehicleTakeOff { get => "D:TAKEOFF"; }
    static public string VehicleLand    { get => "D:LAND"; }
    static public string VehicleRTL     { get => "D:RTL"; }

    // Autoscan
    static public string VehicleAutoCircle(float radius, Vector3 center)
    {
        return "D:AUTO:POI:" + radius.ToString("0.00") 
                             + "," + center.z.ToString("0.00") 
                             + "," + center.x.ToString("0.00") 
                             + "," + center.y.ToString("0.00");
    }

    static public string VehicleAutoWaypoints(List<Vector3> waypoints)
    {
        string result = "D:AUTO:WAYPOINTS:";
        foreach (Vector3 waypoint in waypoints)
        {
            result += (waypoint.z.ToString("0.00") + "," + waypoint.x.ToString("0.00") + "," + waypoint.y.ToString("0.00")) + ",";
        }
        return result.Remove(result.Length - 1);      
    }

    public class SAINT
    {
        // Protocol commands
        public const string ACK = "ack:";
        public const string SUCCESS = "success:";

        // General Command
        public const string Reset = "reset";
        public const string Request = "request";

        // Control Commands
        public const string SwitchToManual = "switchManual";
        public const string SwitchToAutonomous = "switchAutonomous";
        public const string OpenGripper = "openGripper";
        public const string CloseGripper = "closeGripper";

        // Autonomous Commands
        public const string CancelAutonomous = "cancel";
        public const string PauseAutonomous = "pause";
        public const string ResumeAutonomous = "resume";
        public const string Grasp = "grasp";
        public const string BoxCycle = "boxCycle";
        public const string PickCycle = "pickCycle";
        public const string SemiAutonomous = "semiAutonomous";
    }
}
