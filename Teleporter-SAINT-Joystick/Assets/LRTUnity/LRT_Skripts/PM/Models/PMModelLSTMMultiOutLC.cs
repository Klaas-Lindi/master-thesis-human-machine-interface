using System;
using System.Collections.Generic;
using UnityEngine;
using CNTK;
using System.Linq;

internal class PMModelLSTMMultiOutLC : PMModel
{
    // Model
    private Function modelFunc;
    private DeviceDescriptor device;
    // PID Properties of the uav
    private PMLSTMMultiProperties properties;

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

    public PMModelLSTMMultiOutLC() : base()
    {
        // Set initial values
        this.properties = new PMLSTMMultiProperties();

        // Load model
        this.device = DeviceDescriptor.UseDefaultDevice();
        foreach (DeviceDescriptor dev in DeviceDescriptor.AllDevices())
        {
            MonoBehaviour.print("Avaible devices: " + dev.AsString());
            if (dev.Type == DeviceKind.GPU)
                this.device = DeviceDescriptor.CPUDevice;
        }

        MonoBehaviour.print($"Hello from CNTK for {device.Type} only!");

        //string model_path = "C:\\Users\\ga72zif\\Documents\\develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "C:\\Users\\Max\\Documents\\Develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200302_cntkLSTM_v42_best.onnx";

        this.modelFunc = Function.Load(this.properties.Model_path, device, ModelFormat.CNTKv2);
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
        float dt = (float)(this.GetCurrentTime(false) - VehicleTime) / 1000;
        this.VehicleTime = this.GetCurrentTime(false);
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
        double reaction_time = this.GetCurrentTime(false) - this.ForwardLatency - this.BackwardLatency + 1000*this.DefaultTimeStep;

        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);

        // calculate time delay of reaction
        double relative_time = (time / 1000);

        // Take time step
        float time_step = this.properties.Trained_time_step;

        // Handle negative time and to less data
        if (relative_time <= 0 || this.VehiclePoseHistory.Count < (this.properties.Model_samples))
        {
            return (this.VehiclePose);
        }

        // Initialize position variables
        PosePM[] result;
        PosePM lstm = new PosePM();

        // Logical
        this.waypointIndex = 0;

        (double time, Pose pose)[] modelVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples).ToArray();
        (double time, Pose pose)[] modelOperatorList = new (double time, Pose pose)[this.properties.Model_samples];
        Queue<PosePM> predList = new Queue<PosePM>(this.properties.Predicted_step);

        for (int j = 0; j < this.properties.Model_samples; j++)
        {
            lstm.position = modelVehicleList[j].pose.position;
            lstm.rotation = modelVehicleList[j].pose.rotation.eulerAngles;
            modelOperatorList[j] = (getOperatorPose(reaction_time - time_step * (this.properties.Model_samples - j), lstm));
        }

        result = calculateLSTM(modelVehicleList.ToList(), modelOperatorList.ToList());

        // Initialize, Update and return predictied pose
        int index = (int)(time / (1000 * time_step));
        if (index >= result.Length)
            index = result.Length - 1;

        lstm = result[index];
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

        //result.Add(this.VehiclePose.position);
        // return result;
        // Set time related values 
        // Time until uav will reacting
        double reaction_time = this.GetCurrentTime(false) - this.ForwardLatency - this.BackwardLatency + start_time;

        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);

        // calculate time delay of reaction TBD not coorectly implemented
        double relative_time = (double)(end_time);

        // Handle negative time
        if (relative_time <= 0 || this.VehiclePoseHistory.Count < (this.properties.Model_samples))
        {
            for (int i = 0; i < this.properties.Predicted_step; i++) result.Add(this.VehiclePose.position);
            return result;
        }

        // Take time step
        float time_step = this.properties.Trained_time_step;

        // Initialize position variables
        PosePM[] result_lstm;
        PosePM lstm = new PosePM();

        // Logical
        this.waypointIndex = 0;

        (double time, Pose pose)[] modelVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples).ToArray();
        (double time, Pose pose)[] modelOperatorList = new (double time, Pose pose)[this.properties.Model_samples];
        Queue<PosePM> predList = new Queue<PosePM>(this.properties.Predicted_step);

        for (int j = 0; j < this.properties.Model_samples; j++)
        {
            lstm.position = modelVehicleList[j].pose.position;
            lstm.rotation = modelVehicleList[j].pose.rotation.eulerAngles;
            modelOperatorList[j] = (getOperatorPose(reaction_time - time_step * (this.properties.Model_samples - j), lstm));
        }

        result_lstm = calculateLSTM(modelVehicleList.ToList(), modelOperatorList.ToList());

        // Initialize, Update and return predictied pose
        foreach(PosePM pmPose in result_lstm)
        {
            result.Add(pmPose.position);
        }

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
    private PosePM[] calculateLSTM(List<(double time, Pose pose)> lastVehiclePoses, List<(double time, Pose pose)> lastOperatorPoses)
    {
        PosePM[] result = new PosePM[this.properties.Predicted_step];
        for (int i = 0; i < result.Length; i++) { result[i] = new PosePM(); }


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
        Value inputVal = Value.CreateBatchOfSequences<float>(inputVar.Shape, inputa, this.device);
        inputDic.Add(inputVar, inputVal);

        // Design output dictonary for model
        outputDic.Add(outputVar, null);

        // Apply model
        modelFunc.Evaluate(inputDic, outputDic, this.device);

        if (this.properties.Model_output_format == PMLSTMMultiProperties.ModelOutput.FORMAT_3M)
        {
            // Get results
            for (int i = 0; 3 * i < outputDic[outputVar].GetDenseData<float>(outputVar)[0].Count; i++)
            {
                //MonoBehaviour.print("Count: " + outputDic[outputVar].GetDenseData<float>(outputVar)[0].Count + " 1: " + i * 3 + " 2: " + (i * 3 + 1) + " 3: " + (i * 3 + 2));
                result[i].position = new Vector3(outputDic[outputVar].GetDenseData<float>(outputVar)[0][i],
                                            outputDic[outputVar].GetDenseData<float>(outputVar)[0][i + 1 * this.properties.Predicted_step],
                                            outputDic[outputVar].GetDenseData<float>(outputVar)[0][i + 2 * this.properties.Predicted_step]);
            }
        }
        if (this.properties.Model_output_format == PMLSTMMultiProperties.ModelOutput.FORMAT_NM3)
        {
            // Get results
            for (int i = this.properties.Model_samples; 3 * i < outputDic[outputVar].GetDenseData<float>(outputVar)[0].Count; i++)
            {
                //MonoBehaviour.print("Count: " + outputDic[outputVar].GetDenseData<float>(outputVar)[0].Count + " 1: " + i * 3 + " 2: " + (i * 3 + 1) + " 3: " + (i * 3 + 2));
                //float x = outputDic[outputVar].GetDenseData<float>(outputVar)[0][i * 3];
                //float y = outputDic[outputVar].GetDenseData<float>(outputVar)[0][i * 3+1];
                //float z = outputDic[outputVar].GetDenseData<float>(outputVar)[0][i * 3+2];

                result[i- this.properties.Model_samples].position = new Vector3(outputDic[outputVar].GetDenseData<float>(outputVar)[0][i*3],
                                            outputDic[outputVar].GetDenseData<float>(outputVar)[0][i*3+1],
                                            outputDic[outputVar].GetDenseData<float>(outputVar)[0][i*3+2]);
            }
        }

        return result;
    }

    internal override void SetModelProperties(object obj)
    {
        if (obj is PMLSTMMultiProperties)
        {
            this.properties = (PMLSTMMultiProperties)obj;
        }
        if (obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    this.properties = new PMLSTMMultiProperties(
                        new Vector3(0.0f,0.0f,0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMLSTMMultiProperties(
                        new Vector3(0.0f, -0.0f, 0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMLSTMMultiProperties(
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
        this.properties = new PMLSTMMultiProperties();
        this.VehiclePoseHistory.Clear();
        this.modelFunc = Function.Load(this.properties.Model_path, device, ModelFormat.CNTKv2);
        this.outputVar = modelFunc.Output;
        this.inputVar = modelFunc.Arguments[0];

    }

    /// <summary>
    /// Set the current operator position. Time stamp will be added internally and Pose History will be updated
    /// </summary>
    /// <param name="pose">The position and rotation of the uav</param>
    internal override void SetCurrentOperatorPose(Pose pose)
    {

        //MonoBehaviour.print(OperatorPose.position + " <-> " + pose.position);
        //if ((OperatorPose.position != pose.position) || (OperatorPose.rotation != pose.rotation))
        //{
        // Store pose if pose will be send to teleoperator
        if (!this.ActiveCommads)
        {
            this.OperatorPoseHistory.RemoveLast();
        }
        else
        {
            this.OperatorTime = this.GetCurrentTime(false);
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
}

/// <summary>
/// This class stores all 
/// </summary>
public class PMLSTMMultiProperties
{
    // base parameters
    private Vector3 limit_offset;
    private float waypointRangeOfAcceptance;
    private string model_path;
    private int model_samples;
    private int predicted_step;
    private float trained_time_step;
    private ModelOutput model_output_format;
    public enum ModelOutput {FORMAT_3M, FORMAT_M3, FORMAT_NM3 };

    // help parameters
    private string std_model = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200320_cntkLSTMMultiv3_N30_M70.cntkv2";//20200225_cntkLSTMMultiv3_v1.0_N15_M20.cntkv2";

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
    /// <summary>
    /// Get or set the trained time step of the model. This information comes from the model
    /// </summary>
    public float Trained_time_step { get => trained_time_step; set => trained_time_step = value; }
    /// <summary>
    /// Get or set the model output format of the model. This information comes from the used model
    /// </summary>
    public ModelOutput Model_output_format { get => model_output_format; set => model_output_format = value; }

    public PMLSTMMultiProperties()
    {
        this.Limit_offset = new Vector3();
        this.waypointRangeOfAcceptance = 0.1f;
        this.Model_path = this.std_model;
        this.Model_samples = 30;
        this.Predicted_step = 70;
        this.Trained_time_step = 0.022f;
        this.Model_output_format = ModelOutput.FORMAT_NM3;
    }

    public PMLSTMMultiProperties(Vector3 offset, float waypointRangeOfAcceptance)
    {
        this.Limit_offset = offset;
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;
        this.Model_path = this.std_model;
        this.Model_samples = 30;
        this.Predicted_step = 70;
        this.Trained_time_step = 0.022f;
        this.Model_output_format = ModelOutput.FORMAT_NM3;
    }

    public PMLSTMMultiProperties(PMLSTMMultiProperties property)
    {
        this.Limit_offset = new Vector3(property.Limit_offset.x, property.Limit_offset.y, property.Limit_offset.z);
        this.WaypointRangeOfAcceptance = property.WaypointRangeOfAcceptance;
        this.Model_path = property.Model_path;
        this.Model_samples = property.Model_samples ;
        this.Predicted_step = property.Predicted_step;
        this.Trained_time_step = property.Trained_time_step;
        this.Model_output_format = property.Model_output_format;
    }

    public string getCommentString()
    {
        string floatFormat = "0.000000";
        return " N: " + model_samples.ToString(floatFormat) + " ," +
               " M: " + predicted_step.ToString(floatFormat) + " ," +
               " Trained Time step: " + Trained_time_step.ToString(floatFormat) + " ," +
               " Model Format: " + Model_output_format.ToString(floatFormat) + " ," +
               " Model: " + model_path;
    }

    public object Clone()
    {
        return new PMLSTMMultiProperties(this);
    }

}



