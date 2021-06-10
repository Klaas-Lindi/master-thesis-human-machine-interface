using System;
using System.Collections.Generic;
using UnityEngine;


internal class PMModelAPG : PMModel
{
    // PID Properties of the uav
    private PMAPGProperties properties;

    // Default time step to calculate path
    //private float default_timestep;

    // Inertal velocity
    private Vector3 inertal_velocity;

    // Index of waypoints to fly on path
    private int waypointIndex = 0;

    // 
    private float waypointRangeOfAcceptance = 1.0f;

    public PMModelAPG() : base()
    {
        // Set initial values
        this.properties = new PMAPGProperties();

        this.waypointRangeOfAcceptance = 1.0f;
    }

    /// <summary>
    /// Set the current uav position. Time stamp will be added internally
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    internal override void SetCurrentUavPose(Pose pose)
    {

        // Set Pose 
         this.VehiclePose = pose;
        
        // Get time difference
        float dt = (float)(this.GetCurrentTime() - VehicleTime) / 1000;
        this.VehicleTime = this.GetCurrentTime();
        this.VehiclePoseHistory.Add(this.VehicleTime, this.VehiclePose);

        // dt limit of one second, otherwise something is wrong
        if (dt > 1.0)
            return;

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
        double reaction_time = this.GetCurrentTime() - this.ForwardLatency - this.BackwardLatency + 1000*this.DefaultTimeStep;

        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);

        // calculate time delay of reaction
        double relative_time = (time / 1000);

        // Calculate good time step
        float time_step = this.DefaultTimeStep;
        if (time_step > relative_time)
        {
            time_step = (float)relative_time / 10;
        }


        // Handle negative time
        if (relative_time < 0)
            return this.VehiclePose;

        // Initialize position variables
        // PID
        PosePM error = new PosePM();
        PosePM x_start = new PosePM();
        PosePM delta = new PosePM();
        PosePM overflow = new PosePM();
        PosePM apg = new PosePM();

        // Dynamics
        this.inertal_velocity = this.CalculateVelocity(4);

        // Logical
        this.waypointIndex = 0;

        // Timing Loop
        double t;
        double t_start = 0.0;
        int index_point = 0;
        for (t = 0.0f; t < relative_time; t += time_step)
        {
            // Calculate error to destinaction
            error = getError(t * 1000 + reaction_time, delta);

            if (error.position != x_start.position)
            {
                overflow.position += apg.position;
                x_start = error;
                t_start = t - time_step;
            }

            // Calculate next step in time
            apg = calculateAPG(x_start, t - t_start);
            delta.position = overflow.position + apg.position; // * time_step;
        }

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
        double reaction_time = this.GetCurrentTime() - this.ForwardLatency - this.BackwardLatency + start_time;

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
        // PID
        PosePM error = new PosePM();
        PosePM x_start = new PosePM();
        PosePM delta = new PosePM();
        PosePM overflow = new PosePM();
        PosePM apg = new PosePM();

        // Dynamics
        this.inertal_velocity = this.CalculateVelocity(4);

        // Logical
        this.waypointIndex = 0;

        // Calculate good time step
        float time_step = this.DefaultTimeStep;
        if (time_step > relative_time)
        {
            time_step = (float)steps_size / 10;
        }

        // Timing Loop
        double t;
        double t_start = 0.0;
        int index_point = 0;
        for (t = 0.0; t < relative_time; t += time_step)
        {

            // Calculate error to destinaction
            error = getError(t * 1000 + reaction_time, delta);

            if(error.position != x_start.position)
            {
                overflow.position += apg.position;
                x_start = error;
                t_start = t- time_step;
            }
            
            // Calculate next step in time
            apg = calculateAPG(x_start, t - t_start);
            delta.position = overflow.position + apg.position; // * time_step;

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
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.x), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.x)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.y), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.y)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.z), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.z)));

        // Check if waypoint is still available
        if (this.Waypoints.Count != 0 && this.waypointIndex < this.Waypoints.Count)
        {
            // If Waypoint is older than current time calculate it
            if (this.Waypoints[this.waypointIndex].TimetStamp < time)
            {
                // Increase waypoint index when error is smaller the range of acceptance
                if ((this.Waypoints[this.waypointIndex].Position - this.VehiclePose.position - delta.position).magnitude < this.properties.WaypointRangeOfAcceptance)
                {
                    this.waypointIndex++;
                }
                result.position = (this.Waypoints[this.waypointIndex].Position - this.VehiclePose.position + this.properties.Limit_offset);
                return result;
            }
        }
        // Calculate error to operator
        result.position = (this.OperatorPoseHistory.GetNextValidPose(time).position - this.VehiclePose.position + properties.Limit_offset);

        if (Mathf.Abs(result.position.x) < 0.001)
            result.position.x = 0.0f;
        if (Mathf.Abs(result.position.y) < 0.001)
            result.position.y = 0.0f;
        if (Mathf.Abs(result.position.y) < 0.001)
            result.position.y = 0.0f;

        return result;
    }

     
    /// <summary>
    /// Calculate PID and check max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateAPG(PosePM x_start, double t)
    {
        PosePM result = new PosePM();
        if (t < 0)
            return result;

        // case analysis
        if (this.properties.W_d.x == System.Numerics.Complex.Zero)
        {
            PMAPGVector3Complex T_1 = ((this.properties.Di * x_start.position - this.inertal_velocity) * t + x_start.position);

            PMAPGVector3Complex T_2 = PMAPGVector3Complex.Exp(-this.properties.Di * t);

            result.position = x_start.position - (T_1 * T_2).Real;

        }
        else
        {
            PMAPGVector3Complex T_1 = PMAPGVector3Complex.Exp(-this.properties.Di * t);

            PMAPGVector3Complex c = ((-this.inertal_velocity + this.properties.Di * x_start.position) / this.properties.W_d);
            PMAPGVector3Complex T_2 = c * PMAPGVector3Complex.Sin(this.properties.W_d * t);
            PMAPGVector3Complex T_3 = x_start.position * PMAPGVector3Complex.Cos(this.properties.W_d * t);

            result.position = x_start.position - (T_1 * (T_2 + T_3)).Real;
        }

        //MonoBehaviour.print(result.position);
       
        return result;
    }

    internal override void SetModelProperties(object obj)
    {
        if (obj is PMAPGProperties)
        {
            this.properties = (PMAPGProperties)obj;
        }
        if (obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    float factor = 1.00f;
                    // X: 0,07641819 mit M: 32,4 k: 34,39997 d: 82,39992 Offset: -0,3
                    // Y: 0,04671165 mit M: 18 k: 38,39999 d: 83,99997 Offset: -0,35
                    // Z: 0,05774673 mit M: 10,1 k: 37 d: 88 Offset: -0,1 //0,0815995 mit M: 10 k: 17 d: 39 Offset: -0,2
                    // Z: 0,06046231 mit M: 9,250001 k: 25,10001 d: 60,19997 Offset: -0,09999997
                    // X: 0,07961977 mit M: 27 k: 36 d: 83 Offset: -0,3 "" 0,08343512 mit M: 25,2 k: 36,39995 d: 80 Offset: -0,35
                    // 0,07959433 mit M: 26,80001 k: 35,19997 d: 81,19998 Offset: -0,3
                    this.properties = new PMAPGProperties(
                        new Vector3(26.8f,18.000f, 9.25f), // mass of the system
                        new Vector3(35.19997f,38.399f, 25.1f), // spring constant 
                        new Vector3(81.1998f,83.999f, 60.19f), // damping vactors for each axis
                        new Vector3(-0.2f, -0.12f, -0.15f), //new Vector3(-0.3f,-0.35f,-0.09997f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMAPGProperties(
                        new Vector3(1.0f, 1.0f, 1.0f), // mass of the system
                        new Vector3(5.0f, 5.0f, 5.0f), // spring constant 
                        new Vector3(10.0f, 10.0f, 10.0f), // damping vactors for each axis
                        new Vector3(0.0f, -0.0f, 0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMAPGProperties(
                        new Vector3(1.0f, 1.0f, 1.0f), // mass of the system
                        new Vector3(5.0f, 5.0f, 5.0f), // spring constant 
                        new Vector3(10.0f, 10.0f, 10.0f), // damping vactors for each axis
                        new Vector3(0.0f, -0.0f, 0.0f), // offset of position limit to calculate error
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
        this.properties = new PMAPGProperties();
    }
}

/// <summary>
/// This class stores all 
/// </summary>
[Serializable]
public class PMAPGProperties : ICloneable
{
    // base parameters
    [SerializeField]
    private Vector3 m;
    [SerializeField]
    private Vector3 k;
    [SerializeField]
    private Vector3 d;
    [SerializeField]
    private Vector3 limit_offset;
    [SerializeField]
    private float waypointRangeOfAcceptance;

    // help parameters
    private PMAPGVector3Complex w_0 = new PMAPGVector3Complex();
    private PMAPGVector3Complex w_d = new PMAPGVector3Complex();
    private PMAPGVector3Complex di = new PMAPGVector3Complex();

    // Properties
    public Vector3 M { get => m; set{ m = value; updateParameters(); } }
    public Vector3 K { get => k; set { k = value; updateParameters(); }  }
    public Vector3 D { get => d; set { d = value; updateParameters(); }  }
    public Vector3 Limit_offset { get => limit_offset; set => limit_offset = value; }
    public PMAPGVector3Complex W_0 { get => w_0; }
    public PMAPGVector3Complex W_d { get => w_d; }
    public PMAPGVector3Complex Di { get => di; }
    /// <summary>
    /// Get or set the range when the waypoint is classified as reached
    /// </summary>
    public float WaypointRangeOfAcceptance { get => waypointRangeOfAcceptance; set => waypointRangeOfAcceptance = value; }


    public PMAPGProperties()
    {
        this.m = new Vector3(1.0f, 1.0f, 1.0f);
        this.k = new Vector3(5.0f, 5.0f, 5.0f);
        this.d = new Vector3(10.0f, 10.0f, 10.0f);
        this.Limit_offset = new Vector3();
        this.waypointRangeOfAcceptance = 0.1f;

        updateParameters();
    }

    public PMAPGProperties(Vector3 m, Vector3 k, Vector3 d, Vector3 offset, float waypointRangeOfAcceptance)
    {
        this.m = m;
        this.k = k;
        this.d = d;
        this.Limit_offset = offset;
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;

        updateParameters();
    }

    public PMAPGProperties(PMAPGProperties properties)
    {
        this.m = new Vector3(properties.M.x, properties.M.y, properties.M.z);
        this.k = new Vector3(properties.K.x, properties.K.y, properties.K.z);
        this.d = new Vector3(properties.D.x, properties.D.y, properties.D.z);
        this.Limit_offset = new Vector3(properties.Limit_offset.x, properties.Limit_offset.y, properties.Limit_offset.z);
        this.WaypointRangeOfAcceptance = new float();
        this.WaypointRangeOfAcceptance = properties.WaypointRangeOfAcceptance;

        updateParameters();
    }

    private void updateParameters()
    {
        this.w_0.x = System.Numerics.Complex.Sqrt(this.k.x / this.m.x);
        this.w_0.y = System.Numerics.Complex.Sqrt(this.k.y / this.m.y);
        this.w_0.z = System.Numerics.Complex.Sqrt(this.k.z / this.m.z);

        this.di.x = this.d.x / (2 * this.m.x);
        this.di.y = this.d.y / (2 * this.m.y);
        this.di.z = this.d.z / (2 * this.m.z);

        this.w_d.x = System.Numerics.Complex.Sqrt(System.Numerics.Complex.Pow(this.w_0.x, 2) - System.Numerics.Complex.Pow(this.di.x, 2));
        this.w_d.y = System.Numerics.Complex.Sqrt(System.Numerics.Complex.Pow(this.w_0.y, 2) - System.Numerics.Complex.Pow(this.di.y, 2));
        this.w_d.z = System.Numerics.Complex.Sqrt(System.Numerics.Complex.Pow(this.w_0.z, 2) - System.Numerics.Complex.Pow(this.di.z, 2));
    }

    /// <summary>
    /// Returns a formatted string with all properties for the comment in file.
    /// </summary>
    /// <returns>The properties as string-table.</returns>
    public string getCommentString()
    {
        string floatFormat = "0.000000";
        return " Mass: ( X: " + M.x.ToString(floatFormat) + ", Y: " + M.y.ToString(floatFormat) + ", Z: " + M.z.ToString(floatFormat) + " )," +
               " Spring Constant: ( X: " + K.x.ToString(floatFormat) + ", Y: " + K.y.ToString(floatFormat) + ", Z: " + K.z.ToString(floatFormat) + " )," +
               " Damping Constant: ( X: " + D.x.ToString(floatFormat) + ", Y: " + D.y.ToString(floatFormat) + ", Z: " + D.z.ToString(floatFormat) + " )," +
               " Offset: ( X: " + Limit_offset.x.ToString(floatFormat) + ", Y: " + Limit_offset.y.ToString(floatFormat) + ", Z: " + Limit_offset.z.ToString(floatFormat) + " )";
    }

    public object Clone()
    {
        return new PMAPGProperties(this);
    }

}


[Serializable]
public class PMAPGVector3Complex
{
    public System.Numerics.Complex x;
    public System.Numerics.Complex y;
    public System.Numerics.Complex z;

    public PMAPGVector3Complex()
    {
        this.x = System.Numerics.Complex.Zero;
        this.y = System.Numerics.Complex.Zero;
        this.z = System.Numerics.Complex.Zero;
    }

    public PMAPGVector3Complex(System.Numerics.Complex x, System.Numerics.Complex y, System.Numerics.Complex z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    // Member functions
    public Vector3 Real
    {
        get
        {
            Vector3 result = new Vector3();

            result.x = (float)this.x.Real;
            result.y = (float)this.y.Real;
            result.z = (float)this.z.Real;

            return result;
        }
    }

    public Vector3 Imaginary
    {
        get
        {
            Vector3 result = new Vector3();

            result.x = (float)this.x.Imaginary;
            result.y = (float)this.y.Imaginary;
            result.z = (float)this.z.Imaginary;

            return result;
        }
    }

    // Operator
    public static PMAPGVector3Complex operator*(PMAPGVector3Complex a, Vector3 b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x * b.x;
        result.y = a.y * b.y;
        result.z = a.z * b.z;

        return result;
    }

    public static PMAPGVector3Complex operator*(Vector3 a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x * b.x;
        result.y = a.y * b.y;
        result.z = a.z * b.z;
        
        return result;
    }

    public static PMAPGVector3Complex operator *(PMAPGVector3Complex a, double b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x * b;
        result.y = a.y * b;
        result.z = a.z * b;

        return result;
    }

    public static PMAPGVector3Complex operator *(PMAPGVector3Complex a, float b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x * b;
        result.y = a.y * b;
        result.z = a.z * b;

        return result;
    }

    public static PMAPGVector3Complex operator *(PMAPGVector3Complex a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x * b.x;
        result.y = a.y * b.y;
        result.z = a.z * b.z;

        return result;
    }

    public static PMAPGVector3Complex operator /(PMAPGVector3Complex a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        if (b.x == System.Numerics.Complex.Zero ||
           b.y == System.Numerics.Complex.Zero ||
           b.z == System.Numerics.Complex.Zero)
            throw new DivideByZeroException();

        result.x = a.x / b.x;
        result.y = a.y / b.y;
        result.z = a.z / b.z;

        return result;
    }

    public static PMAPGVector3Complex operator +(PMAPGVector3Complex a, Vector3 b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x + b.x;
        result.y = a.y + b.y;
        result.z = a.z + b.z;

        return result;
    }

    public static PMAPGVector3Complex operator +(PMAPGVector3Complex a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x + b.x;
        result.y = a.y + b.y;
        result.z = a.z + b.z;

        return result;
    }

    public static PMAPGVector3Complex operator +(Vector3 a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x + b.x;
        result.y = a.y + b.y;
        result.z = a.z + b.z;

        return result;
    }

    public static PMAPGVector3Complex operator -(PMAPGVector3Complex a)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = -a.x;
        result.y = -a.y;
        result.z = -a.z;

        return result;
    }

    public static PMAPGVector3Complex operator -(PMAPGVector3Complex a, Vector3 b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x - b.x;
        result.y = a.y - b.y;
        result.z = a.z - b.z;

        return result;
    }

    public static PMAPGVector3Complex operator -(Vector3 a, PMAPGVector3Complex b)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = a.x - b.x;
        result.y = a.y - b.y;
        result.z = a.z - b.z;

        return result;
    }

    // Static functions
    internal static PMAPGVector3Complex Exp(PMAPGVector3Complex exponent)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = System.Numerics.Complex.Exp(exponent.x);
        result.y = System.Numerics.Complex.Exp(exponent.y);
        result.z = System.Numerics.Complex.Exp(exponent.z);

        return result;
    }

    internal static PMAPGVector3Complex Cos(PMAPGVector3Complex value)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = System.Numerics.Complex.Cos(value.x);
        result.y = System.Numerics.Complex.Cos(value.y);
        result.z = System.Numerics.Complex.Cos(value.z);

        return result;
    }

    internal static PMAPGVector3Complex Sin(PMAPGVector3Complex a)
    {
        PMAPGVector3Complex result = new PMAPGVector3Complex();

        result.x = System.Numerics.Complex.Sin(a.x);
        result.y = System.Numerics.Complex.Sin(a.y);
        result.z = System.Numerics.Complex.Sin(a.z);

        return result;
    }



}