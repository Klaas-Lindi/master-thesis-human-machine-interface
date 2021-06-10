using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class PMVehicleCopter : PMVehicle
{
    // Vehicle Settings
    private PMVehicleCopterSettings pmVehicleSettings;

    // variables
    private double timeLastStateChange = 0;
    private bool operatorReacted = false;
    private int automatismWaypointIndex = 0;

    private Pose automatismEndPose;
    private Pose automatismStartPose;
    private float automatismPhi;
    private int automatismPassedWaypointsCount;
    private int automatismWaypointCount;

    public PMVehicleCopter() : base()
    {
        this.currentUavState = null;
        this.operatorState = null;
        this.pmModelWrapper = null;

        // Set default Value of automatism
        automatismEndPose = new Pose();
        automatismStartPose = new Pose();
        automatismPhi = 0;
        automatismWaypointCount = 0;
        automatismPassedWaypointsCount = 0;
    }

    public PMVehicleCopter(UavState vehicleState, OperatorState operatorState, PMModel pmModelWrapper)
    {
        this.currentUavState = vehicleState;
        this.operatorState = operatorState;
        this.pmModelWrapper = pmModelWrapper;
        this.pmVehicleSettings = new PMVehicleCopterSettings();

        // Set default Value of automatism  
        automatismEndPose = new Pose();
        automatismStartPose = new Pose();
        automatismPhi = 0;
        automatismWaypointCount = 0;
        automatismPassedWaypointsCount = 0;
    }

    /// <summary>
    /// Set the properties of the curent vehile
    /// </summary>
    /// <param name="obj">obj containing the parameter variables</param>
    override public void SetVehicleProperties(object obj)
    {
        if (obj == typeof(PMVehicleCopterSettings))
        {
            this.pmVehicleSettings = (PMVehicleCopterSettings)obj;
        }
    }

    /// <summary>
    /// Get the properties of the curent vehile
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    override public object GetVehicleProperties()
    {
        if (pmVehicleSettings != null)
        {
            return pmVehicleSettings;
        }
        return null;
    }

    /// <summary>
    /// This function handles all automatism of the uav and set the current operator pose
    /// </summary>
    override public void HandleAutomatism()
    {
        // Only consider automatism in teleoperation state
        if (currentUavState.ControlState == UavState.UavControlState.TELEOPERATION)
        {
            // Destinguis between Condition
            switch (currentUavState.Condition)
            {
                // Most automatism is handled in flying mode
                case UavState.UavCondition.FLYING:
                    // If no automatism is on we only consider tactive mode or idle
                    if (operatorState.ActiveAutomatism == OperatorState.Automatism.NONE)
                    {
                        switch (currentUavState.OperationState)
                        {
                            case UavState.UavOperationState.DIRECT:
                                if (operatorState.StateChanged)
                                {
                                    // Reset previous settings and set flag for direct commanding
                                    pmModelWrapper.ActiveCommads = true;
                                    pmModelWrapper.Waypoints.Clear();
                                    operatorState.StateChanged = false;
                                    pmModelWrapper.SetModelProperties(OperatorState.Automatism.NONE);
                                }
                                //Set Operator Pose
                                pmModelWrapper.SetCurrentOperatorPose(operatorState.OperatorPose);
                                break;
                            case UavState.UavOperationState.IDLE:
                                if (operatorState.StateChanged)
                                {
                                    pmModelWrapper.Waypoints.Clear();
                                    // Set time from last change
                                    operatorState.StateChanged = false;
                                    timeLastStateChange = pmModelWrapper.GetCurrentTime();
                                }

                                // Handle behaviour through reaction time
                                pmModelWrapper.ActiveCommads = false;
                                pmModelWrapper.SetCurrentOperatorPose(handleCommandChange());
                                break;
                        }
                    }

                    // Handle Automatism in flying mode
                    switch (operatorState.ActiveAutomatism)
                    {
                        case OperatorState.Automatism.UAV_RTL:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                // Set flags and time of change
                                pmModelWrapper.ActiveCommads = true;
                                operatorState.StateChanged = false;
                                operatorReacted = false;
                                timeLastStateChange = pmModelWrapper.GetCurrentTime();

                                // Set the initial Waypoints
                                pmModelWrapper.Waypoints.Clear();

                                Vector3 high_point = pmModelWrapper.PredictivePose.position;
                                if (high_point.y < 10)
                                    high_point.y = 10;

                                pmModelWrapper.Waypoints.Add(new Waypoint(high_point, pmModelWrapper.GetCurrentTime()));
                                pmModelWrapper.Waypoints.Add(new Waypoint(new Vector3(0.0f, high_point.y, 0.0f), pmModelWrapper.GetCurrentTime()));
                                pmModelWrapper.Waypoints.Add(new Waypoint(new Vector3(0.0f, 0.0f, 0.0f), pmModelWrapper.GetCurrentTime()));
                                Pose landing_pose = new Pose();
                                landing_pose.position = new Vector3(0.0f, 0.0f, 0.0f);
                                pmModelWrapper.SetCurrentOperatorPose(landing_pose);

                                pmModelWrapper.SetModelProperties(OperatorState.Automatism.UAV_RTL);
                            }

                            // Handle Updates 
                            if (pmModelWrapper.Waypoints.Count == 3 && !this.operatorReacted)
                            {
                                Vector3 high_position = handleCommandChange().position;

                                // Update initial waypoint
                                if (high_position.y < 10)
                                {
                                    high_position.y = 10;
                                }
                                pmModelWrapper.Waypoints[0].Position = high_position;
                            }
                            // Handle success if necessary
                            break;
                        case OperatorState.Automatism.UAV_LANDING:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                // Set flags and last reaction time
                                pmModelWrapper.ActiveCommads = false;
                                operatorState.StateChanged = false;
                                this.operatorReacted = false;
                                timeLastStateChange = pmModelWrapper.GetCurrentTime();
                                pmModelWrapper.Waypoints.Clear();
                                automatismWaypointIndex = 0;
                                pmModelWrapper.SetModelProperties(OperatorState.Automatism.UAV_LANDING);
                            }

                            // Handle Updates
                            if (!this.operatorReacted)
                            {
                                Vector3 landing_position = handleCommandChange().position;

                                // Set current landing position
                                landing_position.y = 0;
                                Pose landing_pose = new Pose();
                                automatismEndPose.position = landing_position;
                            }
                            pmModelWrapper.SetCurrentOperatorPose(automatismEndPose);
                            // Handle success if necessary
                            break;

                        case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                // Set flags, clear waypoints and set last time of change
                                pmModelWrapper.ActiveCommads = false;
                                operatorState.StateChanged = false;
                                operatorReacted = false;
                                timeLastStateChange = pmModelWrapper.GetCurrentTime();
                                pmModelWrapper.Waypoints.Clear();
                                pmModelWrapper.SetModelProperties(OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE);
                            }

                            // Handle Updates
                            if (!operatorReacted)
                            {
                                pmModelWrapper.Waypoints.Clear();
                                automatismStartPose.position = handleCommandChange().position; //+ new Vector3(0.0f, 0.0f, -this.operatorState.RadiusAutoCircle);

                                // Calculate shortest distance to circle
                                Vector3 delta = currentUavState.CameraPose.position - automatismStartPose.position;
                                automatismPhi = Mathf.Atan2(-delta.x, delta.z) - 1 * Mathf.PI;
                                // Calculate limit to rotate one full circle
                                float limit = automatismPhi - 2 * Mathf.PI;

                                // Set starting point
                                //automatismStartPose.position = operatorState.OperatorPose.position; //+ new Vector3(0.0f, 0.0f, this.operatorState.RadiusAutoCircle);

                                // Set Radius
                                float radius = this.operatorState.RadiusAutoCircle;
                                Vector3 circle_point;

                                // Calculate circle
                                for (; automatismPhi > limit; automatismPhi -= Mathf.PI / 18)
                                {
                                    circle_point = new Vector3(radius * Mathf.Sin(automatismPhi), 0, -radius * Mathf.Cos(automatismPhi));
                                    circle_point += (automatismStartPose.position);// - new Vector3(0.0f, 0.0f, -this.operatorState.RadiusAutoCircle));
                                    pmModelWrapper.Waypoints.Add(new Waypoint(circle_point, pmModelWrapper.GetCurrentTime()));
                                }
                                automatismWaypointCount = pmModelWrapper.Waypoints.Count;
                                automatismPhi += Mathf.PI / 18;
                            }

                            // Handle constrant updates
                            // Set end position acording to current uav position
                            automatismEndPose = new Pose();
                            //circle_point = new Vector3(radius * Mathf.Cos(2 * Mathf.PI), 0, radius * Mathf.Sin(2 * Mathf.PI));
                            //circle_point += starting_position;

                            automatismEndPose.position = this.currentUavState.CameraPose.position;
                            pmModelWrapper.SetCurrentOperatorPose(automatismEndPose);

                            if (automatismWaypointCount > pmModelWrapper.Waypoints.Count)
                            {
                                // automatismPhi -= Mathf.PI / 18;
                                Vector3 circle_point = new Vector3(this.operatorState.RadiusAutoCircle * Mathf.Sin(automatismPhi), 0, -this.operatorState.RadiusAutoCircle * Mathf.Cos(automatismPhi));// - this.operatorState.RadiusAutoCircle);
                                pmModelWrapper.Waypoints.Add(new Waypoint(automatismStartPose.position + circle_point, pmModelWrapper.GetCurrentTime()));
                                automatismPhi -= Mathf.PI / 18;
                            }
                            // Handle success if necessary
                            break;

                        case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE_SET_POINT:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                // Set flags, clear waypoints and set last time of change
                                pmModelWrapper.ActiveCommads = false;
                                operatorState.StateChanged = false;
                                operatorReacted = false;
                                timeLastStateChange = pmModelWrapper.GetCurrentTime();
                                pmModelWrapper.Waypoints.Clear();
                                pmModelWrapper.SetModelProperties(OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE);

                                // Calculate shortest distance to circle
                                Vector3 delta = currentUavState.CameraPose.position - operatorState.OperatorPose.position;
                                automatismPhi = Mathf.Atan2(-delta.x, delta.z) - 1 * Mathf.PI;
                                // Calculate limit to rotate one full circle
                                float limit = automatismPhi - 2 * Mathf.PI;

                                // Set Radius
                                float radius = this.operatorState.RadiusAutoCircle;
                                Vector3 circle_point;

                                // Set starting point
                                automatismStartPose.position = operatorState.OperatorPose.position; //+ new Vector3(0.0f, 0.0f, this.operatorState.RadiusAutoCircle);

                                // Set first point to go into height
                                if (currentUavState.CameraPose.position.y < automatismStartPose.position.y)
                                    pmModelWrapper.Waypoints.Add(new Waypoint(
                                        new Vector3(currentUavState.CameraPose.position.x, automatismStartPose.position.y, currentUavState.CameraPose.position.z),
                                        pmModelWrapper.GetCurrentTime()));
                                else
                                    pmModelWrapper.Waypoints.Add(new Waypoint(
                                        new Vector3(automatismStartPose.position.x, currentUavState.CameraPose.position.y, automatismStartPose.position.z) +
                                        new Vector3(radius * Mathf.Sin(automatismPhi), 0, -radius * Mathf.Cos(automatismPhi)),
                                        pmModelWrapper.GetCurrentTime()));

                                // Calculate circle
                                for (; automatismPhi > limit; automatismPhi -= Mathf.PI / 18)
                                {
                                    circle_point = new Vector3(radius * Mathf.Sin(automatismPhi), 0, -radius * Mathf.Cos(automatismPhi));
                                    circle_point += (automatismStartPose.position);// - new Vector3(0.0f, 0.0f, this.operatorState.RadiusAutoCircle));
                                    pmModelWrapper.Waypoints.Add(new Waypoint(circle_point, pmModelWrapper.GetCurrentTime()));
                                }
                                automatismPhi += Mathf.PI / 18;
                                automatismWaypointCount = pmModelWrapper.Waypoints.Count;

                                automatismEndPose = new Pose();
                                automatismEndPose.position = pmModelWrapper.Waypoints[pmModelWrapper.Waypoints.Count - 1].Position;
                                pmModelWrapper.SetCurrentOperatorPose(automatismEndPose);
                                automatismPassedWaypointsCount = 0;
                            }

                            // Handle constrant updates
                            // Set end position acording to current uav position


                            // Handle Updates until operator reacts
                            if (!operatorReacted)
                            {
                                if ((pmModelWrapper.Waypoints.Count > 0) && automatismPassedWaypointsCount == 0)
                                {
                                    float radius = this.operatorState.RadiusAutoCircle;
                                    // Set first point to go into height
                                    if (currentUavState.CameraPose.position.y < automatismStartPose.position.y)
                                        pmModelWrapper.Waypoints[0].Position =
                                            new Vector3(currentUavState.CameraPose.position.x, automatismStartPose.position.y, currentUavState.CameraPose.position.z);
                                    else
                                        pmModelWrapper.Waypoints[0].Position =
                                            new Vector3(automatismStartPose.position.x, currentUavState.CameraPose.position.y, automatismStartPose.position.z) +
                                            new Vector3(radius * Mathf.Sin(automatismPhi), 0, -radius * Mathf.Cos(automatismPhi));
                                }
                                //pmModelWrapper.Waypoints[0].Position = new Vector3(currentUavState.CameraPose.position.x, automatismStartPose.position.y, currentUavState.CameraPose.position.z);
                            }

                            if (automatismWaypointCount > pmModelWrapper.Waypoints.Count)
                            {
                                automatismPassedWaypointsCount++;
                                if (automatismPassedWaypointsCount != 1)
                                {
                                    Vector3 circle_point = new Vector3(this.operatorState.RadiusAutoCircle * Mathf.Sin(automatismPhi), 0, -this.operatorState.RadiusAutoCircle * Mathf.Cos(automatismPhi));// - this.operatorState.RadiusAutoCircle);
                                    pmModelWrapper.Waypoints.Add(new Waypoint(automatismStartPose.position + circle_point, pmModelWrapper.GetCurrentTime()));
                                    automatismPhi -= Mathf.PI / 18;
                                    automatismEndPose = new Pose();
                                    automatismEndPose.position = pmModelWrapper.Waypoints[pmModelWrapper.Waypoints.Count - 1].Position;
                                    pmModelWrapper.SetCurrentOperatorPose(automatismEndPose);
                                }
                            }

                            // Handle success if necessary
                            break;

                    case OperatorState.Automatism.WAYPOINTS:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                // Set flags, clear waypoints and set last time of change
                                pmModelWrapper.ActiveCommads = false;
                                operatorState.StateChanged = false;
                                operatorReacted = false;
                                pmModelWrapper.Waypoints.Clear();
                                timeLastStateChange = pmModelWrapper.GetCurrentTime();

                                pmModelWrapper.Waypoints.Clear();
                                pmModelWrapper.SetModelProperties(OperatorState.Automatism.WAYPOINTS);

                                // Set first point to get the correct height
                                if(currentUavState.CameraPose.position.y < operatorState.Waypoints[0].y)
                                    pmModelWrapper.Waypoints.Add(new Waypoint(new Vector3(currentUavState.CameraPose.position.x, operatorState.Waypoints[0].y, currentUavState.CameraPose.position.z), pmModelWrapper.GetCurrentTime()));
                                else
                                    pmModelWrapper.Waypoints.Add(new Waypoint(new Vector3(operatorState.Waypoints[0].x, currentUavState.CameraPose.position.y, operatorState.Waypoints[0].z), pmModelWrapper.GetCurrentTime()));

                                foreach (Vector3 waypoint in operatorState.Waypoints)
                                {
                                    pmModelWrapper.Waypoints.Add(new Waypoint(waypoint, pmModelWrapper.GetCurrentTime()));
                                }

                                automatismEndPose = new Pose();
                                automatismEndPose.position = pmModelWrapper.Waypoints[pmModelWrapper.Waypoints.Count - 1].Position;
                                pmModelWrapper.SetCurrentOperatorPose(automatismEndPose);
                                automatismWaypointCount = pmModelWrapper.Waypoints.Count;
 
                            }

                            // Handle Updates until operator reacts
                            if (!operatorReacted )
                            {
                                if (automatismWaypointCount == pmModelWrapper.Waypoints.Count)
                                {
                                    if (currentUavState.CameraPose.position.y < operatorState.Waypoints[0].y)
                                        pmModelWrapper.Waypoints[0].Position = new Vector3(currentUavState.CameraPose.position.x, operatorState.Waypoints[0].y, currentUavState.CameraPose.position.z);
                                    else
                                        pmModelWrapper.Waypoints[0].Position = new Vector3(operatorState.Waypoints[0].x, currentUavState.CameraPose.position.y, operatorState.Waypoints[0].z);
                                }
                            }

                            // Handle success if necessary
                            break;
                    }
                    break;

                // Handle Automatism when landed
                case UavState.UavCondition.LANDED:
                    switch (operatorState.ActiveAutomatism)
                    {
                        case OperatorState.Automatism.UAV_TAKEOFF:
                            // Handle State Change and Initialization
                            if (operatorState.StateChanged)
                            {
                                pmModelWrapper.ActiveCommads = true;
                                operatorState.StateChanged = false;
                                pmModelWrapper.Waypoints.Clear();
                                automatismWaypointIndex = 0;
                                pmModelWrapper.Waypoints.Add(new Waypoint(currentUavState.CameraPose.position + new Vector3(0.0f, 1.25f, 0.0f), pmModelWrapper.GetCurrentTime())); // Hier evtl. event handling wenn waypoint erfüllt wurde
                            }

                            // Handle Updates
                            pmModelWrapper.SetCurrentOperatorPose(operatorState.OperatorPose);

                            // Handle success if necessary

                            break;
                        default:
                            pmModelWrapper.SetCurrentOperatorPose(currentUavState.CameraPose);
                            break;
                    }
                    break;
                default:
                    pmModelWrapper.ActiveCommads = false;
                    pmModelWrapper.SetCurrentOperatorPose(handleCommandChange());
                    break;
            }
        }
        else  // Manual control is activated
        {
            // Get current status of the operator (pose)
            pmModelWrapper.ActiveCommads = false;
            pmModelWrapper.SetCurrentOperatorPose(handleCommandChange());
        }

        //Handle Waypoints
        if (pmModelWrapper.Waypoints.Count != 0)
        {
            Vector3 error_waypoint = new Vector3();
            // calculate error to waypoint
            error_waypoint = (pmModelWrapper.Waypoints[0].Position - currentUavState.CameraPose.position);
            // Increase waypoint index when error is smaller the range of acceptance
            if (error_waypoint.magnitude < this.pmVehicleSettings.waypointAcceptanceRange)
            {
                pmModelWrapper.Waypoints.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// This methods handles the pose of uav, when there was a change of state and we have to predict the behavour until the uav reacts to the new command.
    /// with timeLastStateChange the time of last change will be set
    /// operatorReacted flag will be set to true, if the timeLastStateChange was reached.
    /// </summary>
    /// <returns>The estimated pose</returns>
    private Pose handleCommandChange()
    {
        Pose result = new Pose();
        double current_time = pmModelWrapper.GetCurrentTime();
        if (current_time < (timeLastStateChange + pmModelWrapper.ForwardLatency + pmModelWrapper.BackwardLatency))
        {
            result = pmModelWrapper.GetPredictivePoseAt(pmModelWrapper.BackwardLatency + pmModelWrapper.ForwardLatency - (current_time - timeLastStateChange));
        }
        else
        {
            result = currentUavState.CameraPose;
            this.operatorReacted = true;
        }
        return result;
    }

    /// <summary>
    /// Reset the vehicle settings and preknowledge
    /// </summary>
    override public void Reset()
    {
        this.pmVehicleSettings = new PMVehicleCopterSettings();

        // Set default Value of automatism  
        automatismEndPose = new Pose();
        automatismStartPose = new Pose();
        automatismPhi = 0;
        automatismWaypointCount = 0;
        automatismPassedWaypointsCount = 0;
        timeLastStateChange = 0;
        operatorReacted = false;
    }
}

public class PMVehicleCopterSettings
{
    public float waypointAcceptanceRange;

    public PMVehicleCopterSettings()
    {
        this.waypointAcceptanceRange = 1;
    }

    public PMVehicleCopterSettings(float waypointAcceptanceRange)
    {
        this.waypointAcceptanceRange = waypointAcceptanceRange;
    }
}


