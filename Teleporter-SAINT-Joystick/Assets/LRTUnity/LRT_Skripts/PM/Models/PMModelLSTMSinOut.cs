using System;
using System.Collections.Generic;
using UnityEngine;
using CNTK;
using System.Linq;

internal class PMModelLSTMSinOut : PMModel
{
    // Model
    private Function modelFunc;
    private DeviceDescriptor cpu;
    // PID Properties of the uav
    private PMLSTMProperties properties;

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


    public PMModelLSTMSinOut() : base()
    {
        // Set initial values
        this.properties = new PMLSTMProperties();

        // Load model
        this.cpu = DeviceDescriptor.UseDefaultDevice();
        //MonoBehaviour.print($"Hello from CNTK for {cpu.Type} only!");

        //string model_path = "C:\\Users\\ga72zif\\Documents\\develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "C:\\Users\\Max\\Documents\\Develop\\cntk\\Stored_Models\\20200302_cntkLSTM_v42_best.onnx";
        //string model_path = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200302_cntkLSTM_v42_best.onnx";

        this.modelFunc = Function.Load(this.properties.Model_path, cpu, ModelFormat.CNTKv2);
        this.outputVar = modelFunc.Output;
        this.inputVar = modelFunc.Arguments[0];
        
        this.DefaultTimeStep = 0.022f; 
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
        double reaction_time = this.GetCurrentTime() - this.ForwardLatency - this.BackwardLatency + 1000 * this.DefaultTimeStep; //

        // Update History to latest time
        this.OperatorPoseHistory.UpdateLatestTime(reaction_time);

        // calculate time delay of reaction
        double relative_time = (time / 1000);

        // Handle negative time
        if (relative_time <= 0 || this.VehiclePoseHistory.Count < (this.properties.Model_samples + this.properties.Predicted_step))
        {
            return this.VehiclePose;
        }

        // Calculate good time step
        float time_step = this.DefaultTimeStep;

        // Initialize position variables
        PosePM predPose = new PosePM();

        // Logical
        this.waypointIndex = 0;

        if (this.properties.EnablePropagation)
        {

            double t;

            (double time, Pose pose)[] tmpVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step - 1).ToArray();
            (double time, Pose pose)[] tmpOperatorList = this.OperatorPoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step - 1).ToArray();

            Queue<(double time, Pose pose)> modelVehicleList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);
            Queue<(double time, Pose pose)> modelOperatorList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);

            for (int j = 0; j < this.properties.Model_samples; j++)
            {
                modelVehicleList.Enqueue(tmpVehicleList[j]);
                modelOperatorList.Enqueue(tmpOperatorList[j]);
            }


            int i = 0;
            List<Vector3> result = new List<Vector3>();
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
                    modelOperatorList.Enqueue(tmpOperatorList[this.properties.Model_samples + result.Count - 1]);
                }
                else
                {
                    Pose newEntry = new Pose();
                    newEntry.position = result[result.Count - this.properties.Predicted_step];
                    modelVehicleList.Enqueue((reaction_time + t, newEntry));
                    modelOperatorList.Enqueue(tmpOperatorList[tmpVehicleList.Length - 1]);
                }
            }
        }
        else
        {
            // Calculate next step in time
            predPose = calculateLSTM(this.VehiclePoseHistory.GetLast(this.properties.Model_samples),this.OperatorPoseHistory.GetLast(this.properties.Model_samples));
        }

        // Initialize, Update and return predictied pose
        Pose prediciton = new Pose();
        prediciton.position = predPose.position;
        //prediciton.rotation = VehiclePose.rotation * delta.rotation;
        prediciton.rotation.eulerAngles = predPose.rotation;

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

        (double time, Pose pose)[] tmpVehicleList = this.VehiclePoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step-1).ToArray();
        (double time, Pose pose)[] tmpOperatorList = this.OperatorPoseHistory.GetLast(this.properties.Model_samples + this.properties.Predicted_step-1).ToArray();
        
        Queue<(double time, Pose pose)> modelVehicleList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);
        Queue<(double time, Pose pose)> modelOperatorList = new Queue<(double time, Pose pose)>(this.properties.Model_samples);

        for (int j = 0; j < this.properties.Model_samples; j++)
        {
            modelVehicleList.Enqueue(tmpVehicleList[j]);
            modelOperatorList.Enqueue(tmpOperatorList[j]);
        }


        int i = 0;
        for (t = 0.0; t < relative_time; t += time_step)
        {
            predPose = calculateLSTM(modelVehicleList.ToList(), modelOperatorList.ToList());

            result.Add(predPose.position);

            i++ ;

            modelVehicleList.Dequeue();
            modelOperatorList.Dequeue();

            if (result.Count < this.properties.Predicted_step)
            {
                modelVehicleList.Enqueue(  tmpVehicleList[this.properties.Model_samples + result.Count-1]);
                modelOperatorList.Enqueue(tmpOperatorList[this.properties.Model_samples + result.Count-1]);
            }
            else
            {
                Pose newEntry = new Pose();
                newEntry.position = result[result.Count - this.properties.Predicted_step];
                modelVehicleList.Enqueue((reaction_time+t, newEntry));
                modelOperatorList.Enqueue(tmpOperatorList[tmpVehicleList.Length - 1]);
            }
        }
        //MonoBehaviour.print(modelVehicleList.Count);
        
        //result.Add(this.VehiclePose.position);
        return result;

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
        if (obj is PMLSTMProperties)
        {
            this.properties = (PMLSTMProperties)obj;
        }
        if (obj is OperatorState.Automatism)
        {
            switch ((OperatorState.Automatism)obj)
            {
                case OperatorState.Automatism.NONE:
                    this.properties = new PMLSTMProperties(
                        new Vector3(0.0f,0.0f,0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                case OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE:
                    this.properties = new PMLSTMProperties(
                        new Vector3(0.0f, -0.0f, 0.0f), // offset of position limit to calculate error
                        0.1f); // Set Range of Acceptance for waypoint 
                    break;
                default:
                    this.properties = new PMLSTMProperties(
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
        this.properties = new PMLSTMProperties();
        this.VehiclePoseHistory.Clear();
        this.modelFunc = Function.Load(this.properties.Model_path, cpu, ModelFormat.CNTKv2);
        this.outputVar = modelFunc.Output;
        this.inputVar = modelFunc.Arguments[0];
    }
}

/// <summary>
/// This class stores all 
/// </summary>
public class PMLSTMProperties
{
    // base parameters
    private Vector3 limit_offset;
    private float waypointRangeOfAcceptance;
    private string model_path = "Assets\\LRTUnity\\LRT_Skripts\\PM\\Models\\ONNXModels\\20200320_cntkLSTM_Si_N30_M5_1397ms.cntkv2"; //20200320_cntkLSTM_Si_N10_M62_1397ms.cntkv2";
    private int model_samples = 30;
    private int predicted_step = 5;
    private bool enablePropagation = false;

    // help parameters


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
    /// Enables or disable propagation of the predicted time, when latency exceed predicted trained position
    /// </summary>
    public bool EnablePropagation { get => enablePropagation; set => enablePropagation = value; }

    public PMLSTMProperties()
    {
        this.Limit_offset = new Vector3();
        this.waypointRangeOfAcceptance = 0.1f;

    }

    public PMLSTMProperties(Vector3 offset, float waypointRangeOfAcceptance)
    {
        this.Limit_offset = offset;
        this.WaypointRangeOfAcceptance = waypointRangeOfAcceptance;

    }

    public PMLSTMProperties(PMLSTMProperties properties)
    {
        this.Model_samples = properties.Model_samples;
        this.Predicted_step = properties.Predicted_step;
        this.Model_path = properties.Model_path;
        this.EnablePropagation = properties.EnablePropagation;

        this.Limit_offset = properties.Limit_offset;
        this.WaypointRangeOfAcceptance = properties.WaypointRangeOfAcceptance;
    }

    public string getCommentString()
    {
        string floatFormat = "0.000000";
        return " N: " + model_samples.ToString(floatFormat) + " ," +
               " M: " + predicted_step.ToString(floatFormat) + " ," +
               " Model: " + model_path;
    }

    public object Clone()
    {
        return new PMLSTMProperties(this);
    }

}



