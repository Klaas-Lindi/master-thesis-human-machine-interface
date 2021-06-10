using System;
using System.Collections.Generic;
using UnityEngine;


internal class PMModelAdvPID : PMModel
{
    // PID Properties of the uav
    private PMAdvPIDProperties properties;

    // Default time step to calculate path
    //private float default_timestep;

    // Saved error for differential calculations
    private PosePM pre_error;

    // Saved error for integral calculations
    private PosePM integral_error;

    // Saved error for integral calculations from uav state history
    private PosePM pre_integral_error;

    // Inertal velocity
    private Vector3 inertal_velocity;

    // Index of waypoints to fly on path
    private int waypointIndex = 0;

    // 
    private float waypointRangeOfAcceptance = 1.0f;

    public PMModelAdvPID() : base()
    {
        // Set initial values
        this.properties = new PMAdvPIDProperties();
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
        //base.SetCurrentUavPose(pose);
        this.VehiclePose = pose;
        
        // Get time difference
        float dt = (float)(this.GetCurrentTime() - VehicleTime) / 1000;
        this.VehicleTime = this.GetCurrentTime();
        this.VehiclePoseHistory.Add(this.VehicleTime, this.VehiclePose);

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
        this.inertal_velocity = this.CalculateVelocity(20);

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
            pid = calculateAdvPID(error, time_step);
            delta.position += pid.position;
            delta.rotation = delta.rotation + pid.rotation;
        }

        // Calculate delat for last timestep
        error = getError(t * 1000 + reaction_time, delta);
        pid = calculateAdvPID(error, (float)(relative_time - (t - time_step)));
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
        // PID
        PosePM error = new PosePM();
        PosePM delta = new PosePM();
        PosePM pid;
        this.pre_error = new PosePM();
        this.integral_error = new PosePM() + this.pre_integral_error;

        // Dynamics
        this.inertal_velocity = this.CalculateVelocity(20);

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
        int index_point = 0;
        for (t = 0.0f; t < relative_time; t += time_step)
        {
            // Calculate error to destinaction
            error = getError(t * 1000 + reaction_time, delta);
            if (t == 0.0f)
            {
                this.pre_error = error;
            }

            //if (error.position.x < -2)
              //  MonoBehaviour.print("hallo");

            // Calculate next step in time
            pid = calculateAdvPID(error, time_step);
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
                result.position += this.properties.Limit_offset;
                return result;
            }
        }
        // Calculate error to operator
        result.position = (this.OperatorPoseHistory.GetNextValidPose(time).position - this.VehiclePose.position - delta.position + properties.Limit_offset);

        if (Mathf.Abs(result.position.x) < 0.001)
            result.position.x = 0.0f;
        if (Mathf.Abs(result.position.y) < 0.001)
            result.position.y = 0.0f;
        if (Mathf.Abs(result.position.y) < 0.001)
            result.position.y = 0.0f;

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
        if(error.position.x > 0)
            result.position.x = this.properties.Position.X.P * error.position.x;
        else
            result.position.x = (this.properties.Position.X.P+this.properties.Position_bias.X.P) * error.position.x;

        if (error.position.y > 0)
            result.position.y = this.properties.Position.Y.P * error.position.y;
        else
            result.position.y = (this.properties.Position.Y.P + this.properties.Position_bias.Y.P) * error.position.y;

        if (error.position.z > 0)
            result.position.z = this.properties.Position.Z.P * error.position.z;
        else
            result.position.z = (this.properties.Position.Z.P + this.properties.Position_bias.Z.P) * error.position.z;

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

        if (error.position.x > 0)
            result.position.x = this.properties.Position.X.I * this.integral_error.position.x;
        else
            result.position.x = (this.properties.Position.X.I + this.properties.Position_bias.X.I) * this.integral_error.position.x;

        if (error.position.y > 0)
            result.position.y = this.properties.Position.Y.I * this.integral_error.position.y;
        else
            result.position.y = (this.properties.Position.Y.I + this.properties.Position_bias.Y.I) * this.integral_error.position.y;

        if (error.position.z > 0)
            result.position.z = this.properties.Position.Z.I * this.integral_error.position.z;
        else
            result.position.z = (this.properties.Position.Z.I + this.properties.Position_bias.Y.I) * this.integral_error.position.z;

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
        if (error.position.x > 0)
            result.position.x = this.properties.Position.X.D * (error.position.x - pre_error.position.x) / dt;
        else
            result.position.x = (this.properties.Position.X.D + this.properties.Position_bias.X.D) * (error.position.x - pre_error.position.x) / dt;

        if (error.position.x > 0)
            result.position.y = this.properties.Position.Y.D * (error.position.y - pre_error.position.y) / dt;
        else
            result.position.y = (this.properties.Position.Y.D + this.properties.Position_bias.X.D) * (error.position.y - pre_error.position.y) / dt;

        if (error.position.x > 0)
            result.position.z = this.properties.Position.Z.D * (error.position.z - pre_error.position.z) / dt;
        else
            result.position.z = (this.properties.Position.Z.D + this.properties.Position_bias.X.D) * (error.position.z - pre_error.position.z) / dt;

        // Calculate Rotation
        result.rotation = new Vector3(
            this.properties.Rotation.X.D * (error.rotation.x - pre_error.rotation.x) / dt,
            this.properties.Rotation.Y.D * (error.rotation.y - pre_error.rotation.y) / dt,
            this.properties.Rotation.Z.D * (error.rotation.z - pre_error.rotation.z) / dt);

        pre_error = error;
        return result;
    }

    /// <summary>
    /// Correct the calculated PID step whit the velocity limits
    /// </summary>
    /// <param name="result">the resulted new position from pid calculation in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the limits</returns>
    private PosePM setVelocityLimits(PosePM result, float dt)
    {
        // Check max velocity
        // Translation
        if (Mathf.Abs(result.position.y / dt) > this.properties.MaxVelocityHeight)
        {
            result.position.y = Mathf.Sign(result.position.y) * this.properties.MaxVelocityHeight * dt;
        }
        if ((Mathf.Sqrt(Mathf.Pow(result.position.x, 2) + Mathf.Pow(result.position.z, 2)) / dt) > this.properties.MaxVelocityPlane)
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

    /// <summary>
    /// Correct the calculated with acceleration and PID bias
    /// </summary>
    /// <param name="result">the resulted new position from pid calculation in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the limits</returns>
    private PosePM setMotionLimits(PosePM result, float dt)
    {
        Vector3 acceleration = new Vector3();
        
        acceleration = result.position;

        if (Mathf.Abs(acceleration.x) > this.properties.Acc.x)
            acceleration.x = Mathf.Sign(acceleration.x) * this.properties.Acc.x ;
        if (Mathf.Abs(acceleration.y) > this.properties.Acc.y )
            acceleration.y = Mathf.Sign(acceleration.y) * this.properties.Acc.y ;
        if (Mathf.Abs(acceleration.z) > this.properties.Acc.z )
            acceleration.z = Mathf.Sign(acceleration.z) * this.properties.Acc.z ;
 
        this.inertal_velocity += acceleration * dt;

        // Check max velocity
        // Translation
        if ((this.inertal_velocity.y) > this.properties.MaxVelocityHeight)
            this.inertal_velocity.y = Mathf.Sign(inertal_velocity.y) * this.properties.MaxVelocityHeight;
        if ((this.inertal_velocity.y) < -(this.properties.MaxVelocityHeight + this.properties.MaxVelocityHeight_bias))
            this.inertal_velocity.y =  -(this.properties.MaxVelocityHeight + this.properties.MaxVelocityHeight_bias);
        if ((Mathf.Sqrt(Mathf.Pow(this.inertal_velocity.x, 2) + Mathf.Pow(this.inertal_velocity.z, 2))) > this.properties.MaxVelocityPlane)
        {
            float x_ratio = Mathf.Abs(this.inertal_velocity.x) / (Mathf.Abs(this.inertal_velocity.x) + Mathf.Abs(this.inertal_velocity.z));
            float z_ratio = Mathf.Abs(this.inertal_velocity.z) / (Mathf.Abs(this.inertal_velocity.x) + Mathf.Abs(this.inertal_velocity.z));
            this.inertal_velocity.x = Mathf.Sign(this.inertal_velocity.x) * this.properties.MaxVelocityPlane * x_ratio;
            this.inertal_velocity.z = Mathf.Sign(this.inertal_velocity.z) * this.properties.MaxVelocityPlane * z_ratio;
        }

        result.position = this.inertal_velocity * dt;
      
        return result;
    }
    
    /// <summary>
    /// Calculate PID and check max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateAdvPID(PosePM error, float dt)
    {
        PosePM result = new PosePM();
        if (dt <= 0)
            return result;

        // Calculate P
        PosePM calc_step = calculateP(error);
        result.position = calc_step.position;
        result.rotation = calc_step.rotation;

        // Calculate I
        calc_step = calculateI(error, dt);
        result.position += calc_step.position;
        result.rotation += calc_step.rotation;

        // Calculate D
        calc_step = calculateD(error, dt);
        result.position += calc_step.position;
        result.rotation += calc_step.rotation;

        // Correct velocity limits
        //result = setVelocityLimits(result, dt);

        // Correct acceleration limits
        result = setMotionLimits(result, dt);

        return result;
    }

    internal override void SetModelProperties(object obj)
    {
        if (obj is PMAdvPIDProperties)
        {
            this.properties = (PMAdvPIDProperties)obj;
        }
        if (obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    float factor = 1.00f;
                    this.properties = new PMAdvPIDProperties(
                    new PIDProperties(24.9f, 0.00f, 34f), // P: 21 I: 0 D: 36 vMax: 1, 5 mit P_Bias: 0 I_Bias: 0 D_Bias: 0 vMax_Bias: 0 mit aMax: 2 Offset: 0
                    new PIDProperties(5.671f, 0.01f, 11.7f), 
                    new PIDProperties(24.9f, 0.00f, 34f),  
                    new PIDProperties(1f, 0.0f, 0.0f), //
                    new PIDProperties(1f, 0.0f, 0.0f),
                    new PIDProperties(1f, 0.0f, 0.0f),
                    4f, //Max velocity in height translation in m/s
                    2.5f, //Max velocity in planar translation in m/s
                    new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                    new Vector3(1.3f, 5.73f, 1.3f), // Acc limits
                    new PIDProperties(-0f, 0.00f, 0f), // Position PID bias
                    new PIDProperties(-0.0f, 0.0f, -0.0f),
                    new PIDProperties(-0f, 0.0f, 0f), 
                    0f, // Velocity height bias
                    new Vector3(-0f, -0f, -0f),// offset
                    0.0f, // Set delay of reaction 
                    0.1f); // Set Range of Acceptance for waypoint 
                    break;
                /* Real Flight data
                case OperatorState.Automatism.NONE:
                    float factor = 1.00f;
                    this.properties = new PMAdvPIDProperties(
                    new PIDProperties(7f, 0.00f, 14.5f), //new PIDProperties(57.523f, 0.01f, 83.523f), //new PIDProperties(5.671f, 0.01f, 11.700f),
                    new PIDProperties(5.671f, 0.01f, 11.7f), //new PIDProperties(14.889f, 0.01f, 29.523f), //new PIDProperties(5.671f, 0.01f, 11.700f),
                    new PIDProperties(3.399999f, 0.00f, 7.899998f),  // new PIDProperties(58.074f, 0.0000f, 98.1f), //new PIDProperties(5.671f, 0.01f, 11.700f),
                    //new PIDProperties(0.0783f, 0.0015f, 0.0390f * factor),
                    new PIDProperties(1f, 0.0f, 0.0f), //
                    new PIDProperties(1f, 0.0f, 0.0f),
                    new PIDProperties(1f, 0.0f, 0.0f),
                    4f, //1.74980f, //Max velocity in height translation in m/s
                    2f, //1.06117f,//Max velocity in planar translation in m/s
                    new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                    new Vector3(5f, 5.73f, 5.5f), // Acc limits
                    new PIDProperties(-2.25f, 0.00f, 1.3f), // Position PID bias
                    new PIDProperties(-0.0f, 0.0f, -0.0f),//new PIDProperties(-0.5f, 0.0f, -1.6f), //new PIDProperties(-1f, 0.0f, -3f),  // //: -0, 7000026 I_Bias: 0 D_Bias: -2, 200003 vMax_Bias: -1 mit aMax: 5, 73 Offset: -0, 05
                    new PIDProperties(-1.0f, 0.0f, 0.999998f),
                    -3f, // Velocity height bias
                    new Vector3(-0.3f, -0.4f, -0.1f),//new Vector3(-0.11f, -0.2f, -0.05f),  // 0: new Vector3(-0.3f,-0.4f,-0.1f), // offset of position limit to calculate error
                    0.0f, // Set delay of reaction 
                    0.1f); // Set Range of Acceptance for waypoint 
                    break; */
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMAdvPIDProperties(
                        new PIDProperties(24.9f, 0.00f, 34f), // P: 21 I: 0 D: 36 vMax: 1, 5 mit P_Bias: 0 I_Bias: 0 D_Bias: 0 vMax_Bias: 0 mit aMax: 2 Offset: 0
                        new PIDProperties(5.671f, 0.01f, 11.7f),
                        new PIDProperties(24.9f, 0.00f, 34f),
                        new PIDProperties(1f, 0.0f, 0.0f), //
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        4f, //Max velocity in height translation in m/s
                        2.5f, //Max velocity in planar translation in m/s
                        new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                        new Vector3(1.3f, 5.73f, 1.3f), // Acc limits
                        new PIDProperties(-0f, 0.00f, 0f), // Position PID bias
                        new PIDProperties(-0.0f, 0.0f, -0.0f),
                        new PIDProperties(-0f, 0.0f, 0f),
                        0f, // Velocity height bias
                        new Vector3(-0f, -0f, -0f),// offset
                        0.0f, // Set delay of reaction 
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMAdvPIDProperties(
                        new PIDProperties(24.9f, 0.00f, 34f), // P: 21 I: 0 D: 36 vMax: 1, 5 mit P_Bias: 0 I_Bias: 0 D_Bias: 0 vMax_Bias: 0 mit aMax: 2 Offset: 0
                        new PIDProperties(5.671f, 0.01f, 11.7f),
                        new PIDProperties(24.9f, 0.00f, 34f),
                        new PIDProperties(1f, 0.0f, 0.0f), //
                        new PIDProperties(1f, 0.0f, 0.0f),
                        new PIDProperties(1f, 0.0f, 0.0f),
                        4f, //Max velocity in height translation in m/s
                        2.5f, //Max velocity in planar translation in m/s
                        new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                        new Vector3(1.3f, 5.73f, 1.3f), // Acc limits
                        new PIDProperties(-0f, 0.00f, 0f), // Position PID bias
                        new PIDProperties(-0.0f, 0.0f, -0.0f),
                        new PIDProperties(-0f, 0.0f, 0f),
                        0f, // Velocity height bias
                        new Vector3(-0f, -0f, -0f),// offset
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
        this.properties = new PMAdvPIDProperties();
        this.pre_integral_error = new PosePM();
        this.integral_error = new PosePM();
        MonoBehaviour.print(this.integral_error.position);
        MonoBehaviour.print(this.pre_integral_error.position);
    }
}

/// <summary>
/// This class stores all 
/// </summary>
[Serializable]
public class PMAdvPIDProperties : PMPIDProperties, ICloneable
{
    [SerializeField]
    private Vector3 acc = new Vector3();
    [SerializeField]
    private PIDPositionProperties position_bias = new PIDPositionProperties();
    [SerializeField]
    private float maxVelocityHeight_bias = 0;
    [SerializeField]
    private Vector3 limit_offset = new Vector3();

    public PIDPositionProperties Position_bias { get => position_bias; set => position_bias = value; }
    public float MaxVelocityHeight_bias { get => maxVelocityHeight_bias; set => maxVelocityHeight_bias = value; }
    public Vector3 Acc { get => acc; set => acc = value; }
    public Vector3 Limit_offset { get => limit_offset; set => limit_offset = value; }

    /// <summary>
    /// Default constructor
    /// </summary>
    public PMAdvPIDProperties()
    {
        // Set PID position // 0.006, 0.16 // 0.025, 0,1 // 0.02
        Vector3 factor = new Vector3(1.00f, 1.00f, 1.00f);

        // Real Flight parameters                                                              
        this.Position.X = new PIDProperties(24.9f * factor.x, 0.00f * factor.y, 34f * factor.z); 
        this.Position.Y = new PIDProperties(5.671f * factor.x, 0.01f * factor.y, 11.7f * factor.z); 
        this.Position.Z = new PIDProperties(24.9f * factor.x, 0.00f * factor.y, 34f * factor.z);
        // Set PID rotation
        this.Rotation.X = new PIDProperties(1f, 0.0f, 0.0f);
        this.Rotation.Y = new PIDProperties(1f, 0.0f, 0.0f);
        this.Rotation.Z = new PIDProperties(1f, 0.0f, 0.0f);

        // Set limits
        this.MaxVelocityHeight = 4f;//1.68f;//Max velocity in height translation in m/s
        this.MaxVelocityPlane = 2.5f;//Max velocity in planar translation in m/s
        this.MaxVelocityRotation = new Vector3(180, 180, 180);//Max velocity in rotation in degree/s 

        // Set acceleration limits:
        this.Acc = new Vector3(1.3f, 5.73f, 1.3f);

        // Set bias
        this.Position_bias.X = new PIDProperties(0.0f, 0.0f, 0.0f);
        this.Position_bias.Y = new PIDProperties(0.0f, 0.0f, 0.0f);
        this.Position_bias.Z = new PIDProperties(0.0f, 0.0f, 0.0f);
        this.MaxVelocityHeight_bias = 0.0f;
        this.Limit_offset = new Vector3(0.0f, 0.0f, 0.0f);

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
    /// <param name="acc">max. limit of acceleration in position in m/s^2</param>
    /// <param name="postion_x_bias">PID bias for position x</param>
    /// <param name="postion_y_bias">PID bias for position y</param>
    /// <param name="postion_z_bias">PID bias for position z</param>
    /// <param name="max_velocity_height_bias">max. limit of velocity in height bias in m/s</param>
    /// <param name="limit_offset">offset of postion limit for each axis in m</param>
    /// <param name="delay">Reaction delay of UAV</param>
    /// <param name="waypointRangeOfAcceptance">Range Of Acceptance for a waypoint</param>
    public PMAdvPIDProperties(PIDProperties postion_x, PIDProperties postion_y, PIDProperties postion_z,
                              PIDProperties rotation_x, PIDProperties rotation_y, PIDProperties rotation_z,
                              float max_velocity_height, float max_velocity_plane, Vector3 max_velocity_rotation, Vector3 acc,
                              PIDProperties postion_x_bias, PIDProperties postion_y_bias, PIDProperties postion_z_bias, float max_velocity_height_bias, Vector3 limit_offset,
                              float delay, float waypointRangeOfAcceptance)
    {
        // Set PID position
        this.Position.X = postion_x;
        this.Position.Y = postion_y;
        this.Position.Z = postion_z;

        // Set PID rotation
        this.Rotation.X = rotation_x;
        this.Rotation.Y = rotation_y;
        this.Rotation.Z = rotation_z;

        // Set acceleration limits:
        this.Acc = acc;

        // Set limits
        this.MaxVelocityHeight = max_velocity_height;
        this.MaxVelocityPlane = max_velocity_plane;
        this.MaxVelocityRotation = max_velocity_rotation;

        // Set bias
        this.Position_bias.X = postion_x_bias;
        this.Position_bias.Y = postion_y_bias;
        this.Position_bias.Z = postion_z_bias;
        this.MaxVelocityHeight_bias = max_velocity_height_bias;
        this.Limit_offset = limit_offset;

        // Set delays
        this.Delay = delay;

        // Set waypoint property
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;
    }

    /// <summary>
    /// Constructor for cloning existing PMSimpleProperties objects
    /// </summary>
    public PMAdvPIDProperties(PMAdvPIDProperties properties)
    {
        this.Position.X = new PIDProperties(properties.Position.X.P, properties.Position.X.I, properties.Position.X.D);
        this.Position.Y = new PIDProperties(properties.Position.Y.P, properties.Position.Y.I, properties.Position.Y.D);
        this.Position.Z = new PIDProperties(properties.Position.Z.P, properties.Position.Z.I, properties.Position.Z.D);

        this.Rotation.X = new PIDProperties(properties.Rotation.X.P, properties.Rotation.X.I, properties.Rotation.X.D);
        this.Rotation.Y = new PIDProperties(properties.Rotation.Y.P, properties.Rotation.Y.I, properties.Rotation.Y.D);
        this.Rotation.Z = new PIDProperties(properties.Rotation.Z.P, properties.Rotation.Z.I, properties.Rotation.Z.D);

        this.MaxVelocityHeight = properties.MaxVelocityHeight;
        this.MaxVelocityPlane = properties.MaxVelocityPlane;
        this.MaxVelocityRotation = new Vector3(properties.MaxVelocityRotation.x, properties.MaxVelocityRotation.y, properties.MaxVelocityRotation.z);

        this.Acc = properties.Acc;

        this.Position_bias.X = new PIDProperties(properties.Position_bias.X.P, properties.Position_bias.X.I, properties.Position_bias.X.D);
        this.Position_bias.Y = new PIDProperties(properties.Position_bias.Y.P, properties.Position_bias.Y.I, properties.Position_bias.Y.D);
        this.Position_bias.Z = new PIDProperties(properties.Position_bias.Z.P, properties.Position_bias.Z.I, properties.Position_bias.Z.D);
        this.MaxVelocityHeight_bias = properties.MaxVelocityHeight_bias;
        this.Limit_offset = properties.Limit_offset;

        this.Delay = properties.Delay;
        this.WaypointRangeOfAcceptance = properties.WaypointRangeOfAcceptance;
    }

    /// <summary>
    /// Returns a formatted string with all properties for the comment in file.
    /// </summary>
    /// <returns>The properties as string-table.</returns>
    override public string getCommentString()
    {
        string floatFormat = "0.000000";
        return " PID_Pos: ( Px: " + Position_bias.X.P.ToString(floatFormat) + ", Ix: " + Position_bias.X.I.ToString(floatFormat) + ", Dx: " + Position_bias.X.D.ToString(floatFormat) +
                          " Py: " + Position_bias.Y.P.ToString(floatFormat) + ", Iy: " + Position_bias.Y.I.ToString(floatFormat) + ", Dy: " + Position_bias.Y.D.ToString(floatFormat) +
                          " Pz: " + Position_bias.Z.P.ToString(floatFormat) + ", Iz: " + Position_bias.Z.I.ToString(floatFormat) + ", Dz: " + Position_bias.Z.D.ToString(floatFormat) + " )," +
               " PID_Pos_bias: ( Px: " + Position.X.P.ToString(floatFormat) + ", Ix: " + Position.X.I.ToString(floatFormat) + ", Dx: " + Position.X.D.ToString(floatFormat) +
                               " Py: " + Position.Y.P.ToString(floatFormat) + ", Iy: " + Position.Y.I.ToString(floatFormat) + ", Dy: " + Position.Y.D.ToString(floatFormat) +
                               " Pz: " + Position.Z.P.ToString(floatFormat) + ", Iz: " + Position.Z.I.ToString(floatFormat) + ", Dz: " + Position.Z.D.ToString(floatFormat) + " )," +
               " PID_Rost ( Px: " + Rotation.X.P.ToString(floatFormat) + ", Ix: " + Rotation.X.I.ToString(floatFormat) + ", Dx: " + Rotation.X.D.ToString(floatFormat) +
                          " Py: " + Rotation.Y.P.ToString(floatFormat) + ", Iy: " + Rotation.Y.I.ToString(floatFormat) + ", Dy: " + Rotation.Y.D.ToString(floatFormat) +
                          " Pz: " + Rotation.Z.P.ToString(floatFormat) + ", Iz: " + Rotation.Z.I.ToString(floatFormat) + ", Dz: " + Rotation.Z.D.ToString(floatFormat) + " )," +
               " MaxVelHeight [m/s]: " + MaxVelocityHeight + " ," +
               " MaxVelHeightBias [m/s]: " + MaxVelocityHeight_bias + " ," +
               " MaxVelPlane [m/s]: " + MaxVelocityPlane + " ," +
               " MaxVelRot [°/s]: " + maxVelocityRotation.ToString() + " ," +
               " Delay [s]: " + Delay + " ," +
               " MaxWaypointAcceptance [m]: " + WaypointRangeOfAcceptance + " ," +
               " Acceleration: ( X: " + Acc.x.ToString(floatFormat) + ", Y: " + Acc.y.ToString(floatFormat) + ", Z: " + Acc.z.ToString(floatFormat) + " )," +
               " Offset: ( X: " + Limit_offset.x.ToString(floatFormat) + ", Y: " + Limit_offset.y.ToString(floatFormat) + ", Z: " + Limit_offset.z.ToString(floatFormat) + " ),";
    }

    public new object Clone()
    {
        return new PMAdvPIDProperties(this);
    }

}
