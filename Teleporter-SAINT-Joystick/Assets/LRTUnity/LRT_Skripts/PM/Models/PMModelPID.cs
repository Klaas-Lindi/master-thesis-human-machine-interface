using System;
using System.Collections.Generic;
using UnityEngine;

internal class PMModelPID : PMModel
{
    // PID Properties of the uav
    private PMPIDProperties properties;

    // Default time step to calculate path
    //private float default_timestep;

    // Saved error for differential calculations
    private PosePM pre_error;

    // Saved error for integral calculations
    private PosePM integral_error;

    // Saved error for integral calculations from uav state history
    private PosePM pre_integral_error;

    // Index of waypoints to fly on path
    private int waypointIndex = 0;

    // 
    private float waypointRangeOfAcceptance = 1.0f;

    public PMModelPID() : base()
    {
        // Set initial values
        this.properties = new PMPIDProperties();
        this.pre_integral_error = new PosePM();

        this.waypointRangeOfAcceptance = 1.0f;
    }

    /// <summary>
    /// Set the current uav position. Time stamp will be added internally
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    internal override void SetCurrentUavPose(Pose pose)
    {
        // Set Pose 
        VehiclePose = pose;
        float dt = (float)(this.GetCurrentTime() - VehicleTime) / 1000;
        VehicleTime = this.GetCurrentTime();

        // dt limit of one second, otherwise something is wrong
        if (dt > 1.0)
            return;

        // Calculate pre_integral_error
        PosePM error = new PosePM();
        Pose operatorPose = this.OperatorPoseHistory.GetNextValidPose(VehicleTime - this.ForwardLatency - this.BackwardLatency + this.properties.Delay);

        // Calculate error
        error.position = operatorPose.position - this.VehiclePose.position;
        error.rotation = new Vector3(
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.x), operatorPose.rotation.eulerAngles.x)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.y), operatorPose.rotation.eulerAngles.y)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.z), operatorPose.rotation.eulerAngles.z)));

        // Accumulate error 
        this.pre_integral_error.position.x += error.position.x * dt;
        this.pre_integral_error.position.y += error.position.y * dt;
        this.pre_integral_error.position.z += error.position.z * dt;
        this.pre_integral_error.rotation.x += error.rotation.x * dt;
        this.pre_integral_error.rotation.y += error.rotation.y * dt;
        this.pre_integral_error.rotation.z += error.rotation.z * dt;
    }

    /// <summary>
    /// Get the predicted pose at the time relative from now
    /// </summary>
    /// <param name="time">the relative time from know to know state in ms</param>
    /// <returns>Returns the pose of the prediction at given time</returns>
    internal override Pose GetPredictivePoseAt(double time)
    {
        // Set time related values 
        // Time until uav will reacting
        double reaction_time = this.GetCurrentTime() - this.ForwardLatency - this.BackwardLatency + this.properties.Delay;

        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);
        
        // calculate time delay of reaction
        double relative_time = (time / 1000);

        // Handle negative time
        if (relative_time <= 0)
            return this.VehiclePose;

        // Initialize position variables
        PosePM error = new PosePM();
        PosePM delta = new PosePM();
        PosePM pid;
        this.pre_error = new PosePM();
        this.integral_error = new PosePM() + this.pre_integral_error;

        this.waypointIndex = 0;

        // Calculate good time step
        float time_step = this.DefaultTimeStep;
        if (time_step > relative_time)
        {
            time_step = (float)relative_time / 10;
        }
        
        // Timing Loop
        double t;
        for (t = 0.0f; t < relative_time; t += time_step)
        {
            // Calculate error to destinaction
            error = getError(t * 1000 + reaction_time, delta);
            if (t == 0.0f)
            {
                this.pre_error = error;
            }

            // Calculate next step in time
            pid = calculatePID(error, time_step);
            delta.position += pid.position;
            delta.rotation = delta.rotation + pid.rotation;
        }

        // Calculate delat for last timestep
        error = getError(t * 1000 + reaction_time, delta);
        pid = calculatePID(error, (float)(relative_time - (t-time_step)));
        delta.position += pid.position;
        delta.rotation = delta.rotation + pid.rotation;

        // Initialize, Update and return predictied pose
        Pose prediciton = new Pose();
        prediciton.position = VehiclePose.position + delta.position;
        //prediciton.rotation = VehiclePose.rotation * delta.rotation;
        prediciton.rotation.eulerAngles = new Vector3(VehiclePose.rotation.eulerAngles.x + delta.rotation.x,
                                                      VehiclePose.rotation.eulerAngles.y + delta.rotation.y,
                                                      VehiclePose.rotation.eulerAngles.z + delta.rotation.z);
        return prediciton;
 
    }

    /// <summary>
    /// Get the Path in a optimized way
    /// </summary>
    /// <param name="start_time">Beginning time</param>
    /// <param name="end_time">End_time of predicition</param>
    /// <param name="steps_size">Stepsize to know points</param>
    /// <returns>A list of all points on path</returns>
    internal override List<Vector3> GetPathOptimized(double start_time, double end_time, double steps_size)
    {
        List<Vector3> result = new List<Vector3>();
        // Set time related values 
        // Time until uav will reacting
        double reaction_time = this.GetCurrentTime() - this.ForwardLatency - this.BackwardLatency + start_time + this.properties.Delay;
        
        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);

        // calculate time delay of reaction TBD not coorectly implemented
        double relative_time = (double)(end_time);

        // Handle negative time
        if (relative_time <= 0)
        {
            result.Add(this.VehiclePose.position);
            return result;
        }

        // Initialize position variables
        PosePM error = new PosePM();
        PosePM delta = new PosePM();
        PosePM pid;
        this.pre_error = new PosePM();
        this.integral_error = new PosePM() + this.pre_integral_error;
        this.waypointIndex = 0;

        // Calculate good time step
        float time_step = this.DefaultTimeStep;
        if (time_step > relative_time)
        {
            time_step = (float) steps_size / 10;
        }

        // Timing Loop
        double t;
        int index_point = 0;
        for (t = 0.0f; t < relative_time; t += time_step)
        {
            // Calculate error to destinaction
            error = getError(t * 1000 + reaction_time, delta);
            if(t == 0.0f)
            {
                this.pre_error = error;
            }

            // Calculate next step in time
            pid = calculatePID(error, time_step);
            delta.position += pid.position; // * time_step;

            if (t >= (index_point * steps_size))
            {
                result.Add(this.VehiclePose.position + delta.position);
                index_point++;
            }
        }

        return result;

    }

    /// <summary>
    /// This funciton calculate the errors depending of waypoint and position of uav
    /// </summary>
    /// <param name="time">The time when error should be calculated</param>
    /// <param name="delta_position">The current delta from the uav</param>
    /// <returns>Returns the error to destination</returns>
    private PosePM getError(double time, PosePM delta)
    {
        // Initialize Vector
        PosePM result = new PosePM();

        // Calculate rotation which is independet from waypoints     
        result.rotation = new Vector3(
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.x + delta.rotation.x), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.x)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.y + delta.rotation.y), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.y)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.z + delta.rotation.z), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.z)));

        /* result.rotation.eulerAngles = new Vector3(
                 (this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.x - this.VehiclePose.rotation.eulerAngles.x - delta.rotation.eulerAngles.x),
                 (this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.y - this.VehiclePose.rotation.eulerAngles.y - delta.rotation.eulerAngles.y),
                 (this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.z - this.VehiclePose.rotation.eulerAngles.z - delta.rotation.eulerAngles.z));
     */
        // Check if waypoint is still available
        if (this.Waypoints.Count != 0 && this.waypointIndex < this.Waypoints.Count)
        {
            // If Waypoint is older than current time calculate it
            if (this.Waypoints[this.waypointIndex].TimetStamp < time)
            {
                // calculate error to waypoint
                result.position = (this.Waypoints[this.waypointIndex].Position - this.VehiclePose.position - delta.position);
                // Increase waypoint index when error is smaller the range of acceptance
                if (result.position.magnitude < this.properties.WaypointRangeOfAcceptance)
                {
                    this.waypointIndex++;
                }
                return result;
            }
        }
        // Calculate error to operator
        result.position = (this.OperatorPoseHistory.GetNextValidPose(time).position - this.VehiclePose.position - delta.position);
        return result;
    }

    /// <summary>
    /// Calculate P without checking max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateP(PosePM error)
    {
        PosePM result = new PosePM();
        // Calculat Position
        result.position.x = this.properties.Position.X.P * error.position.x;
        result.position.y = this.properties.Position.Y.P * error.position.y;
        result.position.z = this.properties.Position.Z.P * error.position.z;

        // Calculate Rotation
        result.rotation = new Vector3(
            this.properties.Rotation.X.P * error.rotation.x,
            this.properties.Rotation.Y.P * error.rotation.y,
            this.properties.Rotation.Z.P * error.rotation.z);

        return result;
    }

    /// <summary>
    /// Calculate I without checking max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateI(PosePM error, float dt)
    {
        PosePM result = new PosePM();

        this.integral_error.position.x += error.position.x * dt;
        this.integral_error.position.y += error.position.y * dt;
        this.integral_error.position.z += error.position.z * dt;
        result.position.x = this.properties.Position.X.I * this.integral_error.position.x;
        result.position.y = this.properties.Position.Y.I * this.integral_error.position.y;
        result.position.z = this.properties.Position.Z.I * this.integral_error.position.z;

        // Calculate Rotation
        this.integral_error.rotation.x += error.rotation.x * dt;
        this.integral_error.rotation.y += error.rotation.y * dt;
        this.integral_error.rotation.z += error.rotation.z * dt;

        result.rotation = new Vector3(
           this.properties.Rotation.X.I * this.integral_error.rotation.x,
           this.properties.Rotation.Y.I * this.integral_error.rotation.y,
           this.properties.Rotation.Z.I * this.integral_error.rotation.z);

        return result;
    }

    /// <summary>
    /// Calculate D without checking max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateD(PosePM error, float dt)
    {
        PosePM result = new PosePM();
        result.position.x = this.properties.Position.X.D * (error.position.x - pre_error.position.x) / dt;
        result.position.y = this.properties.Position.Y.D * (error.position.y - pre_error.position.y) / dt;
        result.position.z = this.properties.Position.Z.D * (error.position.z - pre_error.position.z) / dt;

        // Calculate Rotation
        result.rotation = new Vector3(
            this.properties.Rotation.X.D * (error.rotation.x - pre_error.rotation.x) / dt,
            this.properties.Rotation.Y.D * (error.rotation.y - pre_error.rotation.y) / dt,
            this.properties.Rotation.Z.D * (error.rotation.z - pre_error.rotation.z) / dt);

        pre_error = error;
        return result;
    }

    /// <summary>
    /// Calculate PID and check max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculatePID(PosePM error, float dt)
    {
        PosePM result = new PosePM();
        if (dt <= 0)
            return result;

        // Calculate P
        PosePM calc_step = calculateP(error);
        result.position = calc_step.position;
        result.rotation = calc_step.rotation;

        // Calculate I
        calc_step = calculateI(error,dt);
        result.position += calc_step.position;
        result.rotation += calc_step.rotation;

        // Calculate D
        calc_step = calculateD(error, dt);
        result.position += calc_step.position;
        result.rotation += calc_step.rotation;


        // Check max velocity
        // Translation
        if (Mathf.Abs(result.position.y / dt) > this.properties.MaxVelocityHeight)
        {
            result.position.y = Mathf.Sign(result.position.y) * this.properties.MaxVelocityHeight * dt;
        }
        if ((Mathf.Sqrt(Mathf.Pow(result.position.x,2)+ Mathf.Pow(result.position.z, 2))/dt) > this.properties.MaxVelocityPlane)
        {
            float x_ratio = Mathf.Abs(result.position.x) / (Mathf.Abs(result.position.x) + Mathf.Abs(result.position.z));
            float z_ratio = Mathf.Abs(result.position.z) / (Mathf.Abs(result.position.x) + Mathf.Abs(result.position.z));
            result.position.x = Mathf.Sign(result.position.x) * this.properties.MaxVelocityPlane * dt * x_ratio;
            result.position.z = Mathf.Sign(result.position.z) * this.properties.MaxVelocityPlane * dt * z_ratio;
        }
        // Rotation
        if (Mathf.Abs(result.rotation.x / dt) > this.properties.MaxVelocityRotation.x)
        {
            result.rotation.x = Mathf.Sign(result.rotation.x) * this.properties.MaxVelocityRotation.x * dt;
        }
        if (Mathf.Abs(result.rotation.y / dt) > this.properties.MaxVelocityRotation.y)
        {
            result.rotation.y = Mathf.Sign(result.rotation.y) * this.properties.MaxVelocityRotation.y * dt;
        }
        if (Mathf.Abs(result.rotation.z / dt) > this.properties.MaxVelocityRotation.z)
        {
            result.rotation.z = Mathf.Sign(result.rotation.z) * this.properties.MaxVelocityRotation.z * dt;
        }

        return result;
    }

    internal override void SetModelProperties(object obj)
    {
        if (obj is PMPIDProperties)
        {
            this.properties = (PMPIDProperties)obj;
        }
        if( obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    float factor = 3.00f;
                    this.properties = new PMPIDProperties(
                    // X: 0,2073312 mit P: 0,01949999 I: 0 D: 0,0024 vMax: 1,5
                    // Y: 0,179443 mit P: 0,0185 I: 0 D: 0,0009999999 vMax: 1,6
                    // Z:  0,09915202 mit P: 0,01999999 I: 0 D: 0,0005 vMax: 1,7
                    new PIDProperties(0.0194999f, 0.00000f, 0.0024f* factor), //new PIDProperties(0.099f, 0.00f, 0.0836f), //new PIDProperties(0.049508f, 0.000287f, 0.080751f),
                    new PIDProperties(0.0185f, 0.0000f, 0.00099999f* factor),  //new PIDProperties(0.080f, 0.00f, 0.0836f), //new PIDProperties(0.092340f, 0.000842f, 0.079446f),
                    new PIDProperties(0.02f, 0.00000f, 0.0005f* factor),  //new PIDProperties(0.103f, 0.00f, 0.0836f), //new PIDProperties(0.03392f, 0.00f, 0.026693f),    //new PIDProperties(0.0783f, 0.0015f, 0.0390f * factor),
                    new PIDProperties(1f, 0.0f, 0.0f),
                    new PIDProperties(1f, 0.0f, 0.0f),
                    new PIDProperties(1f, 0.0f, 0.0f),
                    1.7f,//2.46f, //1.74980f, //Max velocity in height translation in m/s //1.57f, //1.74980f, //Max velocity in height translation in m/s
                    1.6f,//2.46f, //1.06117f,//Max velocity in planar translation in m/s //1.57f, //1.06117f,//Max velocity in planar translation in m/s
                    new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                    0.0f, // Set delay of reaction 
                    0.1f); // Set Range of Acceptance for waypoint 
                    break;
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMPIDProperties(
                        new PIDProperties(0.35f, 0.00f, 0.00f),
                        new PIDProperties(0.05f, 0.0f, 0.0f),
                        new PIDProperties(0.35f, 0.00f, 0.00f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        3.0f, //Max velocity in height translation in m/s
                        5.0f,//Max velocity in planar translation in m/s
                        new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                        0.0f, // Set delay of reaction 
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMPIDProperties(
                        new PIDProperties(0.03f, 0.00f, 0.00f),
                        new PIDProperties(0.05f, 0.0f, 0.0f),
                        new PIDProperties(0.03f, 0.00f, 0.00f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        3.0f, //Max velocity in height translation in m/s
                        5.0f,//Max velocity in planar translation in m/s
                        new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                        0.0f, // Set delay of reaction 
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
            }
        }
    }

    internal override object GetModelProperties()
    {
        return this.properties;
    }

    override public void Reset()
    {
        base.Reset();
        this.properties = new PMPIDProperties();
        this.pre_integral_error = new PosePM();
        this.integral_error = new PosePM();
        //MonoBehaviour.print(this.integral_error.position);
        //MonoBehaviour.print(this.pre_integral_error.position);
    }
}

public class PosePM
{
    public int state;
    public Vector3 position;
    public Vector3 rotation;

    public PosePM()
    {
        state = new int();
        position = new Vector3();
        rotation = new Vector3();
    }

    // overload operator +
    public static PosePM operator +(PosePM a, PosePM b)
    {
        PosePM result = new PosePM();
        result.state = a.state + b.state;
        result.position = a.position + b.position;
        result.rotation = a.rotation + b.rotation;

        return result;
    }
}



/// <summary>
/// This class stores all 
/// </summary>
[Serializable]
public class PMPIDProperties : ICloneable
{
    [SerializeField]
    private PIDPositionProperties position = new PIDPositionProperties();
    [SerializeField]
    private PIDRotationProperties rotation = new PIDRotationProperties();
    [SerializeField]
    private float maxVelocityHeight;
    [SerializeField]
    private float maxVelocityPlane;
    public Vector3 maxVelocityRotation;
    [SerializeField]
    private float delay;
    [SerializeField]
    private float waypointRangeOfAcceptance;

    /// <summary>
    /// Returns a formatted table with all properties as a String.
    /// </summary>
    /// <returns>The properties as string-table.</returns>
    public string ToStringTable()
    {
        string floatFormat = "0.000000";
        return "\t\tP\t\tI\t\tD\n" +
               "\nPos: \tX\t" + position.X.P.ToString(floatFormat) + "\t\t" + position.X.I.ToString(floatFormat) + "\t\t" + position.X.D.ToString(floatFormat) +
                    "\n\tY\t" + position.Y.P.ToString(floatFormat) + "\t\t" + position.Y.I.ToString(floatFormat) + "\t\t" + position.Y.D.ToString(floatFormat) +
                    "\n\tZ\t" + position.Z.P.ToString(floatFormat) + "\t\t" + position.Z.I.ToString(floatFormat) + "\t\t" + position.Z.D.ToString(floatFormat) + "\n" +
               "\nRot: \tX\t" + rotation.X.P.ToString(floatFormat) + "\t\t" + rotation.X.I.ToString(floatFormat) + "\t\t" + rotation.X.D.ToString(floatFormat) +
                    "\n\tY\t" + rotation.Y.P.ToString(floatFormat) + "\t\t" + rotation.Y.I.ToString(floatFormat) + "\t\t" + rotation.Y.D.ToString(floatFormat) +
                    "\n\tZ\t" + rotation.Z.P.ToString(floatFormat) + "\t\t" + rotation.Z.I.ToString(floatFormat) + "\t\t" + rotation.Z.D.ToString(floatFormat) + "\n" +
                                       "\nMaxVelHeight [m/s]: \t" + maxVelocityHeight +
                                       "\nMaxVelPlane [m/s]: \t\t" + maxVelocityPlane +
                                       "\nMaxVelRot [°/s]: \t\t" + maxVelocityRotation.ToString() +
                                       "\nDelay [s]: \t\t\t" + delay +
                                       "\nMaxWaypointAcceptance [m]: \t" + waypointRangeOfAcceptance +
                                       "\n\n" +
                                    "new PMSimpleProperties( new PIDProperties(" + position.X.P.ToString(floatFormat) + "f," + position.X.I.ToString(floatFormat) + "f," + position.X.D.ToString(floatFormat) + "f)," +
                                                            "new PIDProperties(" + position.Y.P.ToString(floatFormat) + "f," + position.Y.I.ToString(floatFormat) + "f," + position.Y.D.ToString(floatFormat) + "f)," +
                                                            "new PIDProperties(" + position.Z.P.ToString(floatFormat) + "f," + position.Z.I.ToString(floatFormat) + "f," + position.Z.D.ToString(floatFormat) + "f)," +
                                    "new PIDProperties(0.00f, 0.00f, 0.00f)," +
                                    "new PIDProperties(0.00f, 0.00f, 0.00f)," +
                                    "new PIDProperties(0.00f, 0.00f, 0.00f)," +
                                    maxVelocityHeight + "f," + maxVelocityPlane + "f," + "new Vector3(180, 180, 180), 0.0f, 0.1f)" +
                                        "\n\n";
    }

    /// <summary>
    /// Returns a formatted string with all properties for the comment in file.
    /// </summary>
    /// <returns>The properties as string-table.</returns>
    public virtual string getCommentString()
    {
        string floatFormat = "0.000000";
        return " PID_Pos: ( Px: " + position.X.P.ToString(floatFormat) + ", Ix: " + position.X.I.ToString(floatFormat) + ", Dx: " + position.X.D.ToString(floatFormat) +
                          " Py: " + position.Y.P.ToString(floatFormat) + ", Iy: " + position.Y.I.ToString(floatFormat) + ", Dy: " + position.Y.D.ToString(floatFormat) +
                          " Pz: " + position.Z.P.ToString(floatFormat) + ", Iz: " + position.Z.I.ToString(floatFormat) + ", Dz: " + position.Z.D.ToString(floatFormat) + " )," +
               " PID_Rost ( Px: " + rotation.X.P.ToString(floatFormat) + ", Ix: " + rotation.X.I.ToString(floatFormat) + ", Dx: " + rotation.X.D.ToString(floatFormat) +
                          " Py: " + rotation.Y.P.ToString(floatFormat) + ", Iy: " + rotation.Y.I.ToString(floatFormat) + ", Dy: " + rotation.Y.D.ToString(floatFormat) +
                          " Pz: " + rotation.Z.P.ToString(floatFormat) + ", Iz: " + rotation.Z.I.ToString(floatFormat) + ", Dz: " + rotation.Z.D.ToString(floatFormat) + " )," +
               " MaxVelHeight [m/s]: " + maxVelocityHeight + " ," +
               " MaxVelPlane [m/s]: " + maxVelocityPlane + " ," +
               " MaxVelRot [°/s]: " + maxVelocityRotation.ToString() + " ," +
               " Delay [s]: " + delay + " ," +
               " MaxWaypointAcceptance [m]: " + waypointRangeOfAcceptance;
    }

    public object Clone()
    {
        return new PMPIDProperties(this);
    }

    /// <summary>

    /// <summary>
    /// PID Properties for the position axis of uav
    /// </summary>
    public PIDPositionProperties Position
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
    /// PID Properties for the rotation axis of gimbal
    /// </summary>
    public PIDRotationProperties Rotation
    {
        get
        {
            return rotation;
        }

        set
        {
            rotation = value;
        }
    }

    /// <summary>
    /// Max. possible velocity in high directions
    /// </summary>
    public float MaxVelocityHeight
    {
        get
        {
            return maxVelocityHeight;
        }

        set
        {
            maxVelocityHeight = value;
        }
    }

    /// <summary>
    /// Max. possible plane translation velocity
    /// </summary>
    public float MaxVelocityPlane
    {
        get
        {
            return maxVelocityPlane;
        }

        set
        {
            maxVelocityPlane = value;
        }
    }

    /// <summary>
    /// Delay of reaction in s
    /// </summary>
    public float Delay
    {
        get
        {
            return delay;
        }

        set
        {
            delay = value;
        }
    }

    /// <summary>
    /// Get or set the range when the waypoint is classified as reached
    /// </summary>
    public float WaypointRangeOfAcceptance
    {
        get
        {
            return waypointRangeOfAcceptance;
        }

        set
        {
            waypointRangeOfAcceptance = value;
        }
    }

    /// <summary>
    /// Max. possible rotation velocity in degree/s
    /// </summary>
    public Vector3 MaxVelocityRotation
    {
        get
        {
            return maxVelocityRotation;
        }

        set
        {
            maxVelocityRotation = value;
        }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public PMPIDProperties()
    {
        // Set PID position // 0.006, 0.16 // 0.025, 0,1 // 0.02
        Vector3 factor = new Vector3(1.00f, 1.00f , 1.00f);
                          // Real Flight parameters                                                              // new Real Flight Parameters                         // I dont know                                 // Test parameters
        this.Position.X = new PIDProperties(0.041f * factor.x, 0.0001f * factor.y, 0.0883f * factor.z); //new PIDProperties(0.049508f * factor.x, 0.000287f * factor.y, 0.080751f * factor.z); //new PIDProperties(0.136187f, 0.0000052f, 0.037565f);// new PIDProperties(0.125002f, 0.0f, 0.09072f);//new PIDProperties(0.07f, 0.01f, 0.08f); ////new PIDProperties(0.01f, 0.00f, 0.00f);
        this.Position.Y = new PIDProperties(0.055f * factor.x, 0.0001f * factor.y, 0.1f * factor.z); //new PIDProperties(0.049508f * factor.x, 0.000287f * factor.y, 0.080751f * factor.z); //new PIDProperties(0.092340f * factor.x, 0.000842f * factor.y, 0.079446f * factor.z);//new PIDProperties(0.136187f, 0.0000052f, 0.037565f);//new PIDProperties(0.125002f, 0.0f, 0.091997f);//new PIDProperties(0.07f, 0.01f, 0.08f); //// new PIDProperties(0.03f, 0.0f, 0.0f);
        this.Position.Z = new PIDProperties(0.040f * factor.x, 0.0001f * factor.y, 0.074f * factor.z); //new PIDProperties(0.03392f * factor.x, 0.00f * factor.y, 0.026693f * factor.z);// new PIDProperties(0.136187f, 0.0000052f, 0.037565f);// new PIDProperties(0.085771f, 0.0f, 0.053101f);//new PIDProperties(0.07f, 0.01f, 0.08f); //     // new PIDProperties(0.01f, 0.00f, 0.00f);

        // Set PID rotation
        this.Rotation.X = new PIDProperties(1f, 0.0f, 0.0f);
        this.Rotation.Y = new PIDProperties(1f, 0.0f, 0.0f);
        this.Rotation.Z = new PIDProperties(1f, 0.0f, 0.0f);

        // Set limits
        this.MaxVelocityHeight = 4.00f;//1.68f;//Max velocity in height translation in m/s
        this.MaxVelocityPlane = 2.19f;//Max velocity in planar translation in m/s
        this.MaxVelocityRotation = new Vector3(180, 180, 180);//Max velocity in rotation in degree/s 

        // Set delay of reaction 
        this.Delay = 0.0f; // TBD Not correctly implemented

        // Set Range of Acceptance for waypoint 
        this.WaypointRangeOfAcceptance = 0.1f;
    }

    /// <summary>
    /// Constructor for all parameters
    /// </summary>
    /// <param name="postion_x">PID values for position x</param>
    /// <param name="postion_y">PID values for position y</param>
    /// <param name="postion_z">PID values for position z</param>
    /// <param name="rotation_x">PID values for rotation x</param>
    /// <param name="rotation_y">PID values for rotation y</param>
    /// <param name="rotation_z">PID values for rotation z</param>
    /// <param name="max_velocity_height">max. limit of velocity in height in m/s</param>
    /// <param name="max_velocity_plane">max. limit of velocity in plane direction in m/s</param>
    /// <param name="max_velocity_rotation">max. limit of velocity in rotation in degree/s</param>
    /// <param name="delay">Reaction delay of UAV</param>
    /// <param name="waypointRangeOfAcceptance">Range Of Acceptance for a waypoint</param>
    public PMPIDProperties(PIDProperties postion_x, PIDProperties postion_y, PIDProperties postion_z, PIDProperties rotation_x, PIDProperties rotation_y, PIDProperties rotation_z, float max_velocity_height, float max_velocity_plane, Vector3 max_velocity_rotation, float delay, float waypointRangeOfAcceptance)
    {
        // Set PID position
        this.Position.X = postion_x;
        this.Position.Y = postion_y;
        this.Position.Z = postion_z;

        // Set PID rotation
        this.Rotation.X = rotation_x;
        this.Rotation.Y = rotation_y;
        this.Rotation.Z = rotation_z;

        // Set limits
        this.MaxVelocityHeight = max_velocity_height;
        this.MaxVelocityPlane = max_velocity_plane;
        this.MaxVelocityRotation = max_velocity_rotation;

        // Set delays
        this.Delay = delay;

        // Set waypoint property
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;
    }

    /// <summary>
    /// Constructor for cloning existing PMSimpleProperties objects
    /// </summary>
    public PMPIDProperties(PMPIDProperties properties)
    {
        this.position.X = new PIDProperties(properties.Position.X.P, properties.Position.X.I, properties.Position.X.D);
        this.position.Y = new PIDProperties(properties.Position.Y.P, properties.Position.Y.I, properties.Position.Y.D);
        this.position.Z = new PIDProperties(properties.Position.Z.P, properties.Position.Z.I, properties.Position.Z.D);

        this.rotation.X = new PIDProperties(properties.Rotation.X.P, properties.Rotation.X.I, properties.Rotation.X.D);
        this.rotation.Y = new PIDProperties(properties.Rotation.Y.P, properties.Rotation.Y.I, properties.Rotation.Y.D);
        this.rotation.Z = new PIDProperties(properties.Rotation.Z.P, properties.Rotation.Z.I, properties.Rotation.Z.D);

        this.maxVelocityHeight = properties.maxVelocityHeight;
        this.maxVelocityPlane = properties.maxVelocityPlane;
        this.maxVelocityRotation = new Vector3(properties.maxVelocityRotation.x, properties.maxVelocityRotation.y, properties.maxVelocityRotation.z);

        this.Delay = properties.Delay;
        this.WaypointRangeOfAcceptance = properties.WaypointRangeOfAcceptance;
    }

    // Class to store PID postion Properties
    [Serializable]
    public class PIDPositionProperties
    {
        [SerializeField]
        private PIDProperties x;
        [SerializeField]
        private PIDProperties y;
        [SerializeField]
        private PIDProperties z;

        public PIDProperties X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public PIDProperties Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public PIDProperties Z
        {
            get
            {
                return z;
            }

            set
            {
                z = value;
            }
        }

    }
    [Serializable]
    public class PIDRotationProperties
    {
        [SerializeField]
        private PIDProperties x;
        [SerializeField]
        private PIDProperties y;
        [SerializeField]
        private PIDProperties z;

        public PIDProperties X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public PIDProperties Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public PIDProperties Z
        {
            get
            {
                return z;
            }

            set
            {
                z = value;
            }
        }
    }
}

[Serializable]
public class PIDProperties
{
    [SerializeField]
    private float p;
    [SerializeField]
    private float i;
    [SerializeField]
    private float d;

    public PIDProperties()
    {
        P = 0;
        I = 0;
        D = 0;
    }

    public PIDProperties(float p, float i, float d)
    {
        this.P = p;
        this.I = i;
        this.D = d;
    }

    /// <summary>
    /// The proportional factor of PID control
    /// </summary>
    public float P
    {
        get
        {
            return p;
        }

        set
        {
            p = value;
        }
    }

    /// <summary>
    /// The integration factor of PID control
    /// </summary>
    public float I
    {
        get
        {
            return i;
        }

        set
        {
            i = value;
        }
    }

    /// <summary>
    /// The differential factor of PID control
    /// </summary>
    public float D
    {
        get
        {
            return d;
        }

        set
        {
            d = value;
        }
    }

}