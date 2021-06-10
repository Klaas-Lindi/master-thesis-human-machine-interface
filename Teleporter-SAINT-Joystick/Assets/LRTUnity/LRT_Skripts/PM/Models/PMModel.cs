using System;
using System.Collections.Generic;
using UnityEngine;

internal class PMModel
{
    // State of vehicle
    private Pose vehiclePose;
    private UavState.UavOperationState uavState;
    private double vehicleTime; // Timestep

    // State of operator
    private Pose operatorPose;
    private double tOperator; // Timestep
    private bool activeCommads = true;

    // Waypoints
    private List<Waypoint> waypoints;

    // Pose for prediction
    private Pose predictivePose;
    private double tPrediction; // Timestep

    // Latency to consider
    private double forwardLatency;
    private double backwardLatency;

    // Prediction time step
    private float defaultTimeStep;

    //Histories
    private PoseHistory vehiclePoseHistory;
    private PoseHistory operatorPoseHistory;

    // External Parameters
    private double overwritedTime = -1;
    private double realTimeAtOverwrite;

    #region Class Properties
    /// <summary>
    /// The current position of the uav and current rotation of the gimbal which was received
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

    /// <summary>
    /// The command state of the uav
    /// </summary>
    public UavState.UavOperationState UavState
    {
        get
        {
            return uavState;
        }

        set
        {
            uavState = value;
        }
    }

    /// <summary>
    /// Time of last value from uav
    /// </summary>
    public double VehicleTime
    {
        get
        {
            return vehicleTime;
        }

        set
        {
            vehicleTime = value;
        }
    }

    /// <summary>
    /// The position and rotation of the user/operator within the world
    /// </summary>
    public Pose OperatorPose
    {
        get
        {
            return operatorPose;
        }

        set
        {
            operatorPose = value;
        }
    }

    /// <summary>
    /// Time of last value from operator
    /// </summary>
    public double OperatorTime
    {
        get
        {
            return tOperator;
        }

        set
        {
            tOperator = value;
        }
    }

    /// <summary>
    /// The predicted position of the uav and rotation of the gimbal
    /// </summary>
    public Pose PredictivePose
    {
        get
        {
            return predictivePose;
        }

        set
        {
            predictivePose = value;
        }
    }

    /// <summary>
    /// Last predicted time
    /// </summary>
    public double PredictionTime
    {
        get
        {
            return tPrediction;
        }

        set
        {
            tPrediction = value;
        }
    }

    /// <summary>
    /// Estimated forward latency in ms
    /// </summary>
    public double ForwardLatency
    {
        get
        {
            return forwardLatency;
        }

        set
        {
            forwardLatency = value;
        }
    }

    /// <summary>
    /// Estimated backward latency
    /// </summary>
    public double BackwardLatency
    {
        get
        {
            return backwardLatency;
        }

        set
        {
            backwardLatency = value;
        }
    }

    /// <summary>
    /// Waypoints towards the operator pose
    /// </summary>
    public List<Waypoint> Waypoints
    {
        get
        {
            return waypoints;
        }

        set
        {
            waypoints = value;
        }
    }

    /// <summary>
    /// History of the operator
    /// </summary>
    public PoseHistory OperatorPoseHistory
    {
        get
        {
            return operatorPoseHistory;
        }

        set
        {
            operatorPoseHistory = value;
        }
    }

    /// <summary>
    /// History of the vehicle 
    /// </summary>
    public PoseHistory VehiclePoseHistory
    {
        get
        {
            return vehiclePoseHistory;
        }

        set
        {
            vehiclePoseHistory = value;
        }
    }

    /// <summary>
    /// Is operator tactive or automatism is running?
    /// </summary>
    public bool ActiveCommads
    {
        get
        {
            return activeCommads;
        }

        set
        {
            activeCommads = value;
        }
    }
    /// <summary>
    /// Set the DefaultTimeStep in the calulation. ATTENTION: This has great impact on the performance
    /// </summary>
    public float DefaultTimeStep { get => defaultTimeStep; set => defaultTimeStep = value; }
    #endregion

    public PMModel()
    {
        this.PredictivePose = new Pose();
        this.OperatorPose = new Pose();
        this.VehiclePoseHistory = new PoseHistory();
        this.OperatorPoseHistory = new PoseHistory();
        this.Waypoints = new List<Waypoint>();
    }

    /// <summary>
    /// Set the current uav position. Time stamp will be added internally
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    virtual internal void SetCurrentUavPose(Pose pose)
    {
        // Set current poses
        this.VehiclePose = pose;
        this.VehicleTime = this.GetCurrentTime();

        // Set for History
        this.VehiclePoseHistory.Add(this.VehicleTime, this.VehiclePose);
    }

    /// <summary>
    /// Set the current uav state. Time stamp will be added internally
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    internal void SetCurrentUavState(UavState.UavOperationState state)
    {
        this.UavState = state;
    }

    internal void SetPredictionTimeStep(float predictionTimeStep)
    {
        this.DefaultTimeStep = predictionTimeStep;
    }

    /// <summary>
    /// Set the current operator position. Time stamp will be added internally and Pose History will be updated
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    virtual internal void SetCurrentOperatorPose(Pose pose)
    {

        //MonoBehaviour.print(OperatorPose.position + " <-> " + pose.position);
        //if ((OperatorPose.position != pose.position) || (OperatorPose.rotation != pose.rotation))
        //{
            // Store pose if pose will be send to teleoperator
            if(!this.ActiveCommads)
            {
                this.OperatorPoseHistory.RemoveLast();
            }
            else
            {
                this.OperatorTime = this.GetCurrentTime();
            }

            // Update pose with new values
            this.OperatorPose = new Pose();
            this.OperatorPose.position = pose.position;
            this.OperatorPose.rotation = pose.rotation;
            this.OperatorPose.state = pose.state;

            // Save old pose with valid value time
            this.OperatorPoseHistory.Add(this.OperatorTime, this.OperatorPose);  
        //}
    }

    /// <summary>
    /// Set the forward latency, which means the delay from the operator towards teleoperator
    /// </summary>
    /// <param name="latency">The current forward latency in seconds</param>
    internal void SetForwardLatency(double latency)
    {
        ForwardLatency = latency * 1000; ;
    }

    /// <summary>
    /// Set the backward latency, which means the delay from the teleoperator towards operator
    /// </summary>
    /// <param name="latency">The current backward latency in seconds</param>
    internal void SetBackwardsLatency(double latency)
    {
        BackwardLatency = latency*1000;
    }

    /// <summary>
    /// Get the predicted pose at the time where the uav reacts to the input
    /// </summary>
    /// <returns>Returns the pose of the prediction</returns>
    internal Pose GetPredictivePose()
    {
        PredictionTime = this.GetCurrentTime();
        this.PredictivePose = GetPredictivePoseAt(ForwardLatency+BackwardLatency);
        return PredictivePose;
    }

    /// <summary>
    /// Get the predicted pose at the time relative from now
    /// </summary>
    /// <param name="time">the relative time from know to know state</param>
    /// <returns>Returns the pose of the prediction at given time</returns>
    virtual internal Pose GetPredictivePoseAt(double time)
    {
        PredictivePose.position = OperatorPose.position;
        PredictivePose.rotation = OperatorPose.rotation;
        
        return PredictivePose;
    }

    virtual internal List<Vector3> GetPathOptimized(double start_time, double end_time, double steps_size)
    {
        return null;
    }
    
    internal void UpdateWaypoints()
    {
        // Check if Waypoints are available
        if(this.Waypoints.Count > 0)
        {
            //Calculate Distance an remove if to small
            if (Vector3.Distance(this.Waypoints[0].Position, this.VehiclePose.position) < 0.1)
            {
                // Remove all, also when one was skipt
                this.Waypoints.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Set the given properties to the model
    /// </summary>
    /// <param name="obj">obj containing the parameter variables</param>
    virtual internal void SetModelProperties(object obj)
    {
        if(obj == typeof(PMModel))
        {
            
        }
    }

    /// <summary>
    /// Calculates the current velocity, with a given horizon of samples in the past
    /// </summary>
    /// <param name="sample_horizon">Number of samples from the past for estimating velocity. Minimum 1 is the last vehicle position</param>
    public Vector3 CalculateVelocity(int sample_horizon = 1)
    {
        Vector3 velocity = new Vector3();
        
        // Detect false horizon
        if (sample_horizon <= 0)
            sample_horizon = 1;

        List<(double time, Pose pose)> vehicle_poses = this.VehiclePoseHistory.GetLast(sample_horizon + 1);

        for(int i = 0; i < vehicle_poses.Count-1; i ++)
        {
            //Vector3 delta = (vehicle_poses[i + 1].pose.position - vehicle_poses[i].pose.position)*1000;
            //double dt = (vehicle_poses[i + 1].time - vehicle_poses[i].time);
            velocity += (vehicle_poses[i + 1].pose.position - vehicle_poses[i].pose.position) / (float)((vehicle_poses[i + 1].time - vehicle_poses[i].time)/1000);
            //MonoBehaviour.print((vehicle_poses[i + 1].pose.position - vehicle_poses[i].pose.position)*1000);
        }
        
        if(vehicle_poses.Count != 0)
            velocity /= (float)vehicle_poses.Count;
        

        return velocity;
    }

    /// <summary>
    /// Get the properties of the curent model
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    virtual internal object GetModelProperties()
    {
        return null;
    }

    internal protected double GetCurrentTime(bool enableRealTimeCompensation = true)
    {
        if (this.overwritedTime < 0)
            return DateTime.Now.TimeOfDay.TotalMilliseconds;
        else if (enableRealTimeCompensation)
            return this.overwritedTime + (DateTime.Now.TimeOfDay.TotalMilliseconds - this.realTimeAtOverwrite);
        else
            return this.overwritedTime;
    }

    // External Settings 
    public void OverwriteCurrentTime(double time)
    {
        this.overwritedTime = time;
        this.realTimeAtOverwrite = DateTime.Now.TimeOfDay.TotalMilliseconds;
    }

    virtual public void Reset()
    {
        this.PredictivePose = new Pose();
        this.OperatorPose = new Pose();
        this.OperatorPoseHistory.Clear();
        this.Waypoints.Clear();
        this.VehicleTime = 0;
    }

}



/// <summary>
/// Class to save the history of poses
/// </summary>
public class PoseHistory
{
    private List<Pose> poses;
    private List<double> time_stamp;
    private int indexLatestTime = 0;
    private int count = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public PoseHistory()
    {
        this.poses = new List<Pose>();
        this.time_stamp = new List<double>();
    }

    /// <summary>
    /// Get the count of stored items
    /// </summary>
    public int Count
    {
        get
        {
            return count;
        }
    }

    /// <summary>
    /// Add new pose
    /// </summary>
    /// <param name="time_stamp">time of the given pose</param>
    /// <param name="pose">the pose at the given time</param>
    public void Add(double time_stamp, Pose pose)
    {
        //MonoBehaviour.print(pose.position + " at " + time_stamp);
        this.poses.Add(pose.Clone());
        this.time_stamp.Add(time_stamp);
        count++;
    }

    /// <summary>
    /// Update latest time and so the internal index to get current results
    /// </summary>
    /// <param name="absolute_time">The current absolute time</param>
    /// <returns>The current index of latest change</returns>
    public int UpdateLatestTime(double absolute_time)
    {
        if (count <= 0)
            return 0;

        for (; indexLatestTime < count; indexLatestTime++)
        {
            if (this.time_stamp[indexLatestTime] >= absolute_time)
            {
                if(indexLatestTime != 0)
                    this.indexLatestTime--;
                break;
            }
        }
        if (indexLatestTime == count)
            indexLatestTime--;
        return indexLatestTime;
    }

    /// <summary>
    /// Get the next valid pose for calculateion
    /// </summary>
    /// <param name="absolute_time">The current absolute time</param>
    /// <returns>Pose at latest time</returns>
    public Pose GetNextValidPose(double absolute_time)
    {
        if (count <= 0)
            return new Pose();

        int i = this.indexLatestTime;
        for (i = indexLatestTime; i < this.Count; i++)
        {
            if (this.time_stamp[i] > absolute_time)
            {
                if (i != 0)
                    return this.poses[i-1];
                else
                    return this.poses[0];
            }
        }
        return this.poses[i-1];
    }

    /// <summary>
    /// Remove Last Pose 
    /// </summary>
    internal void RemoveLast()
    {
        if (count > 0)
        {
            count--;
            this.poses.RemoveAt(count);
            this.time_stamp.RemoveAt(count);
        }
    }

    /// <summary>
    /// Get number of last poses
    /// </summary>
    /// <param name="sample_count">number of last index</param>
    /// <returns></returns>
    internal List<(double time, Pose pose)> GetLast(int sample_count)
    {
        List<(double time, Pose pose)> tmp = new List<(double time, Pose pose)>();

        for(int index = count - sample_count; index < count; index++)
        {
            if (index >= 0)
                tmp.Add((this.time_stamp[index], this.poses[index]));
        }

        return tmp;
    }

    public void Clear()
    {
        poses.Clear();
        time_stamp.Clear();
        indexLatestTime = 0;
        count = 0;
    }
}



/// <summary>
/// Waypoint to store currently position and time
/// </summary>
public class Waypoint
{
    private Vector3 position;
    private double time_stamp;

    // Constructor
    public Waypoint()
    {
        this.Position = new Vector3();
        this.TimetStamp = new double();
    }

    /// <summary>
    /// Add waypoint with current TimeStamp from OS Time. NO FAST CALCULATION POSSIBLE
    /// </summary>
    /// <param name="position">The new position as Vector</param>
    /*public Waypoint(Vector3 position)
    {
        this.Position = position;
        this.TimetStamp = DateTime.Now.TimeOfDay.TotalMilliseconds;
    }*/

    /// <summary>
    /// Add waypoint with self setted time stamp
    /// </summary>
    /// <param name="position">position</param>
    /// <param name="time_stamp">Time Stamp</param>
    public Waypoint(Vector3 position, double time_stamp)
    {
        this.Position = position;
        this.TimetStamp = time_stamp;
    }

    /// <summary>
    /// Get the position of the waypoint
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return position;
        }

        set
        {
            position = value;
        }
    }

    /// <summary>
    /// Get the time, when the position was set in ms
    /// </summary>
    public double TimetStamp
    {
        get
        {
            return time_stamp;
        }

        set
        {
            time_stamp = value;
        }
    }
}