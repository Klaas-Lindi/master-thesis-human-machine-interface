using System;
using System.Collections.Generic;
using UnityEngine;
using CNTK;
using System.Linq;

internal class PMModelLSTMSinOutLC : PMModel
{
    // Model
    private Function modelFunc;
    private DeviceDescriptor cpu;
    // PID Properties of the uav
    private PMLSTMv2Properties properties;

    // Default time step to calculate path
    //private float default_timestep;

    // Inertal velocity
    private Vector3 inertal_velocity;

    // Index of waypoints to fly on path
    private int waypointIndex = 0;

    // 
    private float waypointRangeOfAcceptance = 1.0f;

    // Model data
    List<float> inputSequences = new List<float>();
    List<List<float>> inputa = new List<List<float>>();
    Variable inputVar;
    Dictionary<Variable, Value> inputDic = new Dictionary<Variable, Value>();
    Variable outputVar;
    Dictionary<Variable, Value> outputDic = new Dictionary<Variable, Value>();

    public PMModelLSTMSinOutLC() : base()
    {
        // Set initial values
        this.properties = new PMLSTMv2Properties();

        // Load model
        this.cpu = DeviceDescriptor.UseDefaultDevice();
        MonoBehaviour.print($"Hello from CNTK for {cpu.Type} only!");

        //string model_path = "C:\\Users\\ga72zif\\Documents\\develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "C:\\Users\\Max\\Documents\\Develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200302_cntkLSTM_v42_best.onnx";

        this.modelFunc = Function.Load(this.properties.Model_path, cpu, ModelFormat.CNTKv2);
        this.outputVar = modelFunc.Output;
        this.inputVar = modelFunc.Arguments[0];
       
        this.DefaultTimeStep = 0.025f;
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

        // Handle negative time and to less data
        if (relative_time <= 0 || this.VehiclePoseHistory.Count < (this.properties.Model_samples + this.properties.Predicted_step))
        {
            return (this.VehiclePose);
        }

        // Initialize position variables
        List<PosePM> error = new List<PosePM>();
        PosePM delta = new PosePM();
        PosePM lstm = new PosePM();

        // Logical
        this.waypointIndex = 0;

        // Timing Loop
        double t;

        (double time, Pose pose)[] tmpVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step - 1).ToArray();
        Queue<(double time, Pose pose)> modelVehicleList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);
        Queue<(double time, Pose pose)> modelOperatorList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);
        Queue<PosePM> predList = new Queue<PosePM>(this.properties.Predicted_step);

        for (int j = 0; j < this.properties.Model_samples; j++)
        {
            modelVehicleList.Enqueue(tmpVehicleList[j]);
            lstm.position = tmpVehicleList[j].pose.position;
            lstm.rotation = tmpVehicleList[j].pose.rotation.eulerAngles;
            modelOperatorList.Enqueue(getOperatorPose(reaction_time - time_step * (this.properties.Model_samples - j), lstm));
        }

        int i = 0;
        for (t = 0.0; t < relative_time; t += time_step)
        {
            lstm = calculateLSTM(modelVehicleList.ToList(), modelOperatorList.ToList());    
            
            // Dequeue full list for new items
            modelVehicleList.Dequeue();
            modelOperatorList.Dequeue();
            
            // Enqueue list with new data
            i++;
            if (i < this.properties.Predicted_step)
            {
                modelVehicleList.Enqueue(tmpVehicleList[this.properties.Model_samples + i - 1]);

            }
            else
            {
                Pose newEntry = new Pose();
                newEntry.position = predList.Dequeue().position;
                modelVehicleList.Enqueue((reaction_time + t, newEntry));
            }

            predList.Enqueue(lstm);
            modelOperatorList.Enqueue(getOperatorPose(reaction_time + t * 1000, lstm));
        }

        // Initialize, Update and return predictied pose
        Pose prediciton = new Pose();
        prediciton.position = lstm.position;
        //prediciton.rotation = VehiclePose.rotation * delta.rotation;
        prediciton.rotation.eulerAngles = new Vector3(VehiclePose.rotation.eulerAngles.x + lstm.rotation.x,
                                                      VehiclePose.rotation.eulerAngles.y + lstm.rotation.y,
                                                      VehiclePose.rotation.eulerAngles.z + lstm.rotation.z);
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
        if (relative_time <= 0 || this.VehiclePoseHistory.Count < (this.properties.Model_samples + this.properties.Predicted_step))
        {
            result.Add(this.VehiclePose.position);
            return result;
        }

        // Initialize position variables
        PosePM predPose = new PosePM();

        // Calculate good time step
        float time_step = this.DefaultTimeStep;
        if (time_step > relative_time)
        {
            time_step = (float)steps_size / 10;
        }

        // Timing Loop
        double t;

        (double time, Pose pose)[] tmpVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step - 1).ToArray();

        Queue<(double time, Pose pose)> modelVehicleList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);
        Queue<(double time, Pose pose)> modelOperatorList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);

        for (int j = 0; j < this.properties.Model_samples; j++)
        {
            modelVehicleList.Enqueue(tmpVehicleList[j]);
            predPose.position = tmpVehicleList[j].pose.position;
            predPose.rotation = tmpVehicleList[j].pose.rotation.eulerAngles;
            modelOperatorList.Enqueue(getOperatorPose(reaction_time-time_step*(this.properties.Model_samples-j), predPose));
        }


        int i = 0;
        for (t = 0.0; t < relative_time; t += time_step)
        {
            predPose = calculateLSTM(modelVehicleList.ToList(), modelOperatorList.ToList());

            result.Add(predPose.position);

            i++;

            modelVehicleList.Dequeue();
            modelOperatorList.Dequeue();

            if (result.Count < this.properties.Predicted_step)
            {
                modelVehicleList.Enqueue(tmpVehicleList[this.properties.Model_samples + result.Count - 1]);
            }
            else
            {
                Pose newEntry = new Pose();
                newEntry.position = result[result.Count - this.properties.Predicted_step];
                modelVehicleList.Enqueue((reaction_time + t, newEntry));
            }

            modelOperatorList.Enqueue(getOperatorPose(reaction_time+t*1000, predPose));
        }
        //MonoBehaviour.print(modelVehicleList.Count);

        //result.Add(this.VehiclePose.position);
        return result;
    }

    /// <summary>
    /// This funciton calculate the errors depending of waypoint and position of uav
    /// </summary>
    /// <param name="time">The time when error should be calculated</param>
    /// <param name="delta_position">The current delta from the uav</param>
    /// <returns>Returns the error to destination</returns>
    private (double time, Pose pose) getOperatorPose(double time, PosePM pose)
    {
        // Initialize Vector
        Pose result = new Pose();

        
        // Calculate rotation which is independet from waypoints     
        result.rotation = Quaternion.Euler(new Vector3(
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.x), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.x)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.y), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.y)),
            (Mathf.DeltaAngle((this.VehiclePose.rotation.eulerAngles.z), this.OperatorPoseHistory.GetNextValidPose(time).rotation.eulerAngles.z))));

        // Check if waypoint is still available
        if (this.Waypoints.Count != 0 && this.waypointIndex < this.Waypoints.Count)
        {
            // If Waypoint is older than current time calculate it
            if (this.Waypoints[this.waypointIndex].TimetStamp < time)
            {
                // Increase waypoint index when error is smaller the range of acceptance
                if ((this.Waypoints[this.waypointIndex].Position - pose.position).magnitude < this.properties.WaypointRangeOfAcceptance)
                {
                    this.waypointIndex++;
                }
                result.position = (this.Waypoints[this.waypointIndex].Position + this.properties.Limit_offset);
                return (time,result);
            }
        }
        // Calculate error to operator
        result.position = (this.OperatorPoseHistory.GetNextValidPose(time).position + properties.Limit_offset);

        return (time, result);
    }

     
    /// <summary>
    /// Calculate PID and check max velocities of translation
    /// </summary>
    /// <param name="error">the position error between start and destination in m</param>
    /// <param name="dt">time step in ms</param>
    /// <returns>New position after the time step</returns>
    private PosePM calculateLSTM(List<(double time, Pose pose)> lastVehiclePoses, List<(double time, Pose pose)> lastOperatorPoses)
    {
        PosePM result = new PosePM();

        //MonoBehaviour.print(modelFunc.Arguments[0].Shape[0]);
        //MonoBehaviour.print("Last vehicle count: " + lastVehiclePoses.Count + " last operator count:" + lastOperatorPoses.Count);
        // Get input data
        inputSequences.Clear();
        inputa.Clear();
        inputDic.Clear();
        outputDic.Clear();
        for (int i = 0; i < lastVehiclePoses.Count; i++)
        {
            inputSequences.Add(lastVehiclePoses[i].pose.position.x);
            inputSequences.Add(lastVehiclePoses[i].pose.position.y);
            inputSequences.Add(lastVehiclePoses[i].pose.position.z);
            inputSequences.Add(lastOperatorPoses[i].pose.position.x);
            inputSequences.Add(lastOperatorPoses[i].pose.position.y);
            inputSequences.Add(lastOperatorPoses[i].pose.position.z);
        }

        // Design input sequence
        inputa.Add(inputSequences);

        // Design input dictonary for model
        Value inputVal = Value.CreateBatchOfSequences<float>(inputVar.Shape, inputa, cpu);
        inputDic.Add(inputVar, inputVal);

        // Design output dictonary for model
        outputDic.Add(outputVar, null);

        //MonoBehaviour.print("First: " + tmpPose.position.ToString());
        // Apply model
        modelFunc.Evaluate(inputDic, outputDic, cpu);
        //MonoBehaviour.print("Second: " + tmpPose.position.ToString());
        //if(tmpPose.position != lastVehiclePoses[lastVehiclePoses.Count-1].pose.position)
        //    MonoBehaviour.print("First: " + tmpPose.position.ToString() + " Second: "+ lastVehiclePoses[lastVehiclePoses.Count - 1].pose.position);

        // Print results
        //MonoBehaviour.print("OutputVarLength: " + outputDic[outputVar].GetDenseData<float>(outputVar).Count + " VarValuesLength" + outputDic[outputVar].GetDenseData<float>(outputVar)[0].Count);
        result.position = new Vector3(outputDic[outputVar].GetDenseData<float>(outputVar)[0][0],
                                      outputDic[outputVar].GetDenseData<float>(outputVar)[0][1],
                                      outputDic[outputVar].GetDenseData<float>(outputVar)[0][2]);
        //MonoBehaviour.print(outputDic[outputVar].GetDenseData<float>(outputVar)[0][1]);
        //MonoBehaviour.print(outputDic[outputVar].GetDenseData<float>(outputVar)[0][2]);

        return result;
    }

    internal override void SetModelProperties(object obj)
    {
        if (obj is PMLSTMv2Properties)
        {
            this.properties = (PMLSTMv2Properties)obj;
        }
        if (obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    this.properties = new PMLSTMv2Properties(
                        new Vector3(0.0f,0.0f,0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMLSTMv2Properties(
                        new Vector3(0.0f, -0.0f, 0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMLSTMv2Properties(
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
        this.properties = new PMLSTMv2Properties();
    }
}

/// <summary>
/// This class stores all 
/// </summary>
public class PMLSTMv2Properties
{
    // base parameters
    private Vector3 limit_offset;
    private float waypointRangeOfAcceptance;
    private string model_path;
    private int model_samples;
    private int predicted_step;

    // help parameters
    private string std_model = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200211_cntkLSTM_v43.cntkv2";

    // Properties
    public Vector3 Limit_offset { get => limit_offset; set => limit_offset = value; }

    /// <summary>
    /// Get or set the range when the waypoint is classified as reached
    /// </summary>
    public float WaypointRangeOfAcceptance { get => waypointRangeOfAcceptance; set => waypointRangeOfAcceptance = value; }
    /// <summary>
    /// Get or set the path where the model should be loaded
    /// </summary>
    public string Model_path { get => model_path; set => model_path = value; }
    /// <summary>
    /// Get or set the number of sample points which will be loaded for the model 
    /// </summary>
    public int Model_samples { get => model_samples; set => model_samples = value; }
    /// <summary>
    /// Get or set the number how far the predicted step will be predicted in the future. This information comes from the model
    /// </summary>
    public int Predicted_step { get => predicted_step; set => predicted_step = value; }

    public PMLSTMv2Properties()
    {
        this.Limit_offset = new Vector3();
        this.waypointRangeOfAcceptance = 0.1f;
        this.Model_path = this.std_model;
        this.Model_samples = 5;
        this.Predicted_step = 5;
    }

    public PMLSTMv2Properties(Vector3 offset, float waypointRangeOfAcceptance)
    {
        this.Limit_offset = offset;
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;
        this.Model_path = this.std_model;
        this.Model_samples = 5;
        this.Predicted_step = 5;
    }

}



