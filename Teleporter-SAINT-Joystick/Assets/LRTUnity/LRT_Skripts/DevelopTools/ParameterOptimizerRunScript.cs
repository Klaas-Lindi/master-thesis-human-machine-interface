using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ParameterOptimizerRunScript : MonoBehaviour
{
    [Tooltip("If this is checked, the script will terminate all other scripts and run the optimization instead.")]
    public bool enableParameterOptimization;

    [Header("Read Log Settings")]
    [Tooltip("Paths to log which should be readed")]
    public string readLogPath;
    [Tooltip("Start indices paths")]
    public int  readLogStartIndex;
    [Tooltip("Count of line which will be readed from logPath. -1 indicates unlimmited")]
    public int readLogLineCount;

    [Header("Trajectory Settings")]
    [Tooltip("Latency of the trajectory")]
    public float latency;

    public enum OptimizableModels { None, PID, AdvPID, APG };
    [Header("Model Settings")]
    [Tooltip("This list contains all possible models which can be optimized.")]
    public OptimizableModels optimizableModel;
    public enum Axis { x, y, z };
    [Tooltip("Axis which should be optimized.")]
    public Axis optimizableAxis;

    public float predictionHorizon;
    public float predictionTimeStep;
    public float bestMeanValue;


    [Header("PID Model Parameter")]
    public ParameterRange p;
    public ParameterRange i;
    public ParameterRange d;
    public ParameterRange vMax;

    [Header("PID Adv Model Parameter")]
    public ParameterRange pBias;
    public ParameterRange iBias;
    public ParameterRange dBias;
    public ParameterRange vMaxBias;
    public ParameterRange aMax;
    public ParameterRange offsett;

    [Header("APG Model Parameter")]
    public ParameterRange m;
    public ParameterRange k;
    public ParameterRange di;

    // LoadLog() Lists
    private List<double> timestamps_log = new List<double>();
    private List<Pose> opPoseHistory_log = new List<Pose>();
    private List<Pose> uavPoseHistory_log = new List<Pose>();

    private List<StaticRegion> staticRegions;

    // Own model settings
    private PMModel pmModel;
    private ulong runNumber = 0;
    private int workIndex = 0; // Workingpoint index for getting parameters
    public int staticIndex = 1;
    private IEnumerator<object> propertyList;
    private ulong predRunTime;

    void Awake()
    {
        if (enableParameterOptimization)
        {
            TimeSpan runtimeStart = DateTime.Now.TimeOfDay;
            string runtimeStartString = DateTime.Now.ToString("yyyyMMddHHmmss");

            #region StopAllOtherScripts
            // Iterate over all GameObjects
            object[] obj = GameObject.FindObjectsOfType(typeof(GameObject));
            foreach (object o in obj)
            {
                GameObject g = (GameObject)o;

                // Iterate over all scripts
                MonoBehaviour[] scripts = g.GetComponents<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script != this) // Disable all scripts except for this one
                    {
                        script.enabled = false;
                    }
                    else
                    {
                        script.enabled = true;
                    }
                }
            }
            // Stop the Unity Editor
            //UnityEditor.EditorApplication.isPlaying = false;
            #endregion
        }
    }
            // Start is called before the first frame update
    void Start()
    {
        if (enableParameterOptimization)
        {
            // Load data
            this.LoadLog();
            staticRegions = CalculateStaticRegions(CalculateDerivativesSR(opPoseHistory_log));

            // Set model
            switch (optimizableModel)
            {
                case OptimizableModels.None: pmModel = new PMModel(); break;
                case OptimizableModels.PID: pmModel = new PMModelPID(); break;
                case OptimizableModels.AdvPID: pmModel = new PMModelAdvPID(); break;
                case OptimizableModels.APG: pmModel = new PMModelAPG(); break;
            }

            // Configure model
            // Set latency to null
            pmModel.SetForwardLatency(0.0);
            pmModel.SetBackwardsLatency(0.0);
            pmModel.SetPredictionTimeStep(predictionTimeStep);

            // Adjust model for working point
            // Get current uav state (pose, sensors)
            workIndex = 0;
            while (timestamps_log[workIndex] < (timestamps_log[staticRegions[this.staticIndex].start] + this.latency * 1000))
            {
                pmModel.OverwriteCurrentTime(timestamps_log[workIndex]);
                pmModel.SetCurrentUavPose(uavPoseHistory_log[workIndex]);
                pmModel.SetCurrentOperatorPose(opPoseHistory_log[workIndex]);

                // Handle Waypoints
                pmModel.UpdateWaypoints();
                workIndex++;
            }

            bestMeanValue = 10000000000000;
            propertyList = getNextProperty().GetEnumerator();
            this.predRunTime = calculateRunTime();
            print("Start Parameter Optimization: Predicted runtime: " + (this.predRunTime * 0.004f) + " s");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (enableParameterOptimization)
        {
            if (!propertyList.MoveNext())
                Application.Quit();

            object prop = propertyList.Current;

            // Set next Properties
            pmModel.SetModelProperties(prop);

            List<Vector3> predPath = new List<Vector3>();
            // Run calculation 
            pmModel.OverwriteCurrentTime(timestamps_log[this.workIndex]);
            predPath.AddRange(pmModel.GetPathOptimized(timestamps_log[this.workIndex], this.predictionHorizon, this.predictionTimeStep));
            float mean_error = this.bestMeanValue;
            switch (optimizableAxis)
            {
                case Axis.x: mean_error = Mathf.Abs(calculateMeanError(predPath, this.predictionTimeStep).x); break;
                case Axis.y: mean_error = Mathf.Abs(calculateMeanError(predPath, this.predictionTimeStep).y); break;
                case Axis.z: mean_error = Mathf.Abs(calculateMeanError(predPath, this.predictionTimeStep).z); break;
            }

            if (mean_error < this.bestMeanValue)
            {
                this.bestMeanValue = mean_error;
                saveAllValues();
                switch (optimizableModel)
                {
                    case OptimizableModels.PID:
                        print(runNumber + ": " + mean_error +
                            " mit P: " + p.best_fit + " I: " + i.best_fit + " D: " + d.best_fit + " vMax: " + vMax.best_fit); break;
                    case OptimizableModels.AdvPID:
                        print(runNumber + ": " + mean_error +
                            " mit P: " + p.best_fit + " I: " + i.best_fit + " D: " + d.best_fit + " vMax: " + vMax.best_fit +
                            " mit P_Bias: " + pBias.best_fit + " I_Bias: " + iBias.best_fit + " D_Bias: " + dBias.best_fit + " vMax_Bias: " + vMaxBias.best_fit +
                            " mit aMax: " + aMax.best_fit + " Offset: " + offsett.best_fit); break;
                    case OptimizableModels.APG:
                        print(runNumber + ": " + mean_error +
                            " mit M: " + m.best_fit + " k: " + k.best_fit + " d: " + di.best_fit  + " Offset: " + offsett.best_fit); break;
                }
                print("Predicted Runtime: " + (this.predRunTime - runNumber) * Time.deltaTime + " s");

            }

            runNumber++;
       
            if (runNumber > this.predRunTime)
                print("Optimization Finished!");
        }
    }

    private void saveAllValues()
    {
        // PID Values
        p.saveValue(); 
        i.saveValue(); 
        d.saveValue(); 
        vMax.saveValue();

        // PID Adv Values
        pBias.saveValue();
        iBias.saveValue();
        dBias.saveValue();
        vMaxBias.saveValue();
        aMax.saveValue();
        offsett.saveValue();

        // APG Values
        m.saveValue();
        k.saveValue();
        di.saveValue();
    }

    private ulong calculateRunTime()
    {
        ulong result = 1;
        // PID Values
        result *= p.numberOfSteps();
        result *= i.numberOfSteps();
        result *= d.numberOfSteps();
        result *= vMax.numberOfSteps();

        // PID Adv Values
        result *= pBias.numberOfSteps();
        result *= iBias.numberOfSteps();
        result *= dBias.numberOfSteps();
        result *= vMaxBias.numberOfSteps();
        result *= aMax.numberOfSteps();
        result *= offsett.numberOfSteps();

        // APG Values
        result *= m.numberOfSteps();
        result *= k.numberOfSteps();
        result *= di.numberOfSteps();

        return result;
    }

    private IEnumerable<object> getNextProperty()
    {
        switch (optimizableModel)
        {
            case OptimizableModels.PID:
                for (vMax.current_value = vMax.min; vMax.current_value < vMax.max; vMax.current_value += vMax.step_size)
                {
                    for (i.current_value = i.min; i.current_value < i.max; i.current_value += i.step_size)
                    {
                        for (d.current_value = d.min; d.current_value < d.max; d.current_value += d.step_size)
                        {
                            for (p.current_value = p.min; p.current_value < p.max; p.current_value += p.step_size)
                            {
                                yield return new PMPIDProperties(
                                    new PIDProperties(p.current_value, i.current_value, d.current_value),
                                    new PIDProperties(p.current_value, i.current_value, d.current_value),
                                    new PIDProperties(p.current_value, i.current_value, d.current_value),
                                    new PIDProperties(1f, 0.0f, 0.0f),
                                    new PIDProperties(1f, 0.0f, 0.0f),
                                    new PIDProperties(1f, 0.0f, 0.0f),
                                    vMax.current_value,//2.46f, //1.74980f, //Max velocity in height translation in m/s //1.57f, //1.74980f, //Max velocity in height translation in m/s
                                    vMax.current_value,//2.46f, //1.06117f,//Max velocity in planar translation in m/s //1.57f, //1.06117f,//Max velocity in planar translation in m/s
                                    new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                                    0.0f, // Set delay of reaction 
                                    0.1f); // Set Range of Acceptance for waypoint 
                            }
                        }
                    }
                }
                break;
            case OptimizableModels.AdvPID: 
                for (vMax.current_value = vMax.min; vMax.current_value < vMax.max; vMax.current_value += vMax.step_size)
                {
                    for (vMaxBias.current_value = vMaxBias.min; vMaxBias.current_value < vMaxBias.max; vMaxBias.current_value += vMaxBias.step_size)
                    {
                        for (i.current_value = i.min; i.current_value < i.max; i.current_value += i.step_size)
                    {
                            for (iBias.current_value = iBias.min; iBias.current_value < iBias.max; iBias.current_value += iBias.step_size)
                            {
                                for (d.current_value = d.min; d.current_value < d.max; d.current_value += d.step_size)
                                {
                                    for (dBias.current_value = dBias.min; dBias.current_value < dBias.max; dBias.current_value += dBias.step_size)
                                    {
                                        for (p.current_value = p.min; p.current_value < p.max; p.current_value += p.step_size)
                                        {
                                            for (pBias.current_value = pBias.min; pBias.current_value < pBias.max; pBias.current_value += pBias.step_size)
                                            {
                                                for (aMax.current_value = aMax.min; aMax.current_value < aMax.max; aMax.current_value += aMax.step_size)
                                                {
                                                    for (offsett.current_value = offsett.min; offsett.current_value < offsett.max; offsett.current_value += offsett.step_size)
                                                    {

                                                        yield return new PMAdvPIDProperties(
                                                            new PIDProperties(p.current_value, i.current_value, d.current_value),
                                                            new PIDProperties(p.current_value, i.current_value, d.current_value),
                                                            new PIDProperties(p.current_value, i.current_value, d.current_value),
                                                            new PIDProperties(1f, 0.0f, 0.0f),
                                                            new PIDProperties(1f, 0.0f, 0.0f),
                                                            new PIDProperties(1f, 0.0f, 0.0f),
                                                            vMax.current_value,//2.46f, //1.74980f, //Max velocity in height translation in m/s //1.57f, //1.74980f, //Max velocity in height translation in m/s
                                                            vMax.current_value,//2.46f, //1.06117f,//Max velocity in planar translation in m/s //1.57f, //1.06117f,//Max velocity in planar translation in m/s
                                                            new Vector3(180, 180, 180),//Max velocity in rotation in degree/s 
                                                            new Vector3(aMax.current_value, aMax.current_value, aMax.current_value),//Max velocity in rotation in degree/s 
                                                            new PIDProperties(pBias.current_value, iBias.current_value, dBias.current_value),
                                                            new PIDProperties(pBias.current_value, iBias.current_value, dBias.current_value),
                                                            new PIDProperties(pBias.current_value, iBias.current_value, dBias.current_value),
                                                            vMaxBias.current_value, // Velocity height bias
                                                            new Vector3(offsett.current_value, offsett.current_value, offsett.current_value), // offset of position limit to calculate error
                                                            0.0f, // Set delay of reaction 
                                                            0.1f // Set Range of Acceptance for waypoint 
                                                            );
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                break; 
            case OptimizableModels.APG:
                for (offsett.current_value = offsett.min; offsett.current_value < offsett.max; offsett.current_value += offsett.step_size)
                {
                    for (m.current_value = m.min; m.current_value < m.max; m.current_value += m.step_size)
                    {
                        for (di.current_value = di.min; di.current_value < di.max; di.current_value += di.step_size)
                        {
                            for (k.current_value = k.min; k.current_value < k.max; k.current_value += k.step_size)
                            {
                                yield return new PMAPGProperties(
                                    new Vector3(m.current_value, m.current_value, m.current_value),
                                    new Vector3(k.current_value, k.current_value, k.current_value),
                                    new Vector3(di.current_value, di.current_value, di.current_value),
                                    new Vector3(offsett.current_value, offsett.current_value, offsett.current_value),
                                    0.1f);
                            }
                        }
                    }
                }
                break; 
        }

        yield return null;
    }

    private void LoadLog()
    {
        StreamReader streamReader = null;
        // Open Logfile
        try
        {
            streamReader = new StreamReader(this.readLogPath);
        }
        catch (ArgumentException e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("LogFile at " + this.readLogPath + " could not be opened!");
        }
        catch (FileNotFoundException e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("LogFile at " + this.readLogPath + " could not be opened!");
        }

        // Skip Header
        int nextReaderLine = 1; // Number of the line the streamreader is currently in
        if (streamReader != null)
        {
            streamReader.ReadLine(); // "Description:"
            nextReaderLine++;
            streamReader.ReadLine(); // "X and Z are the horizontal plane... "
            nextReaderLine++;
            streamReader.ReadLine(); // Names and units
            nextReaderLine++;
        }

        // Skip to specified line
        int skipCount = this.readLogStartIndex;
        while (skipCount-- > 0 && streamReader != null && !streamReader.EndOfStream)
        {
            streamReader.ReadLine().Split(';');
            nextReaderLine++;
        }

        int line = nextReaderLine;
        while (streamReader != null && !streamReader.EndOfStream)
        {
            if(this.readLogLineCount > 0)
            {
                if ((nextReaderLine - this.readLogStartIndex) > this.readLogLineCount)
                    return;
            }
            

            double timestamp;

            //String operatorCommand;
            //String uavCommand;

            Pose operatorPose = new Pose();
            Pose uavPose = new Pose();

            String[] lineArguments = streamReader.ReadLine().Split(';');

            int i = 0; // Index for lineArguments
            timestamp = double.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);//pmHandler.OverwriteCurrentTime(double.Parse(lineArguments[arg++])); // Get the Real-Datetime value in ms
            i++; // Skip Unity-Time since startup
            i++; // Skip Real-Time since startup

            // Operator
            operatorPose.position.x = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);
            operatorPose.position.y = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);
            operatorPose.position.z = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);


            float x_rotation = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);
            if (x_rotation < 0)
                x_rotation = -1 * x_rotation;
            else
                x_rotation = -1 * (x_rotation + 360);
            operatorPose.rotation.eulerAngles = new Vector3(x_rotation, float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture), float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture));

            // Teleoperator
            uavPose.position.x = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);
            uavPose.position.y = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);
            uavPose.position.z = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);

            uavPose.rotation.eulerAngles = new Vector3(float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture), float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture), float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture));


            // (Commands, reconstruction and prediction log entries are ignored)

            // Add the contents of the line to Lists
            timestamps_log.Add(timestamp);
            opPoseHistory_log.Add(operatorPose);
            uavPoseHistory_log.Add(uavPose);

            // feed the operator poses to the PMModelwrapper object
            //pmModelWrapper.OperatorPoseHistory.Add(timestamp, operatorPose);
            line++;
        }
        Debug.LogWarningFormat("End of Stream reached at line {0}. (Entry Nr. {1})", line, line - 3);
    }

    // Returns the regions where the operator is not moving
    private List<StaticRegion> CalculateStaticRegions(List<Pose> deviations)
    {
        List<StaticRegion> regions = new List<StaticRegion>();

        int startingIndex = 0;
        bool isStatic = false;

        // Loop over derivative List
        for (int i = 0; i < deviations.Count; i++)
        {
            // If position derivative is zero
            if (Math.Abs(deviations[i].position.magnitude) < 0.001)
            {
                if (!isStatic) // And we are not already in a static region
                {
                    startingIndex = i; // A new static Region starts here
                    isStatic = true;
                }
            }
            else if (isStatic) // If derivative is not zero and we are in a static region, it ends here and gets saved to the list
            {
                StaticRegion staticRegion = new StaticRegion(startingIndex, i, i - startingIndex);
                regions.Add(staticRegion);
                isStatic = false;
            }

        }

        if (isStatic) // If we are in a static region at the last element, the region gets cut off and saved
        {
            StaticRegion staticRegion = new StaticRegion(startingIndex, deviations.Count, deviations.Count - startingIndex);
            regions.Add(staticRegion);
        }

        return regions;
    }

    // Calculates the change in operator position for CalculateStaticRegions()
    private List<Pose> CalculateDerivativesSR(List<Pose> poseHistory)
    {
        List<Pose> result = new List<Pose>();

        for (int i = 0; i < poseHistory.Count - 1; i++)
        {
            float dt = (float)(timestamps_log[i + 1] - timestamps_log[i]) / 1000;

            Pose dev = new Pose();

            dev.position.x = (poseHistory[i + 1].position.x - poseHistory[i].position.x) / dt;
            dev.position.y = (poseHistory[i + 1].position.y - poseHistory[i].position.y) / dt;
            dev.position.z = (poseHistory[i + 1].position.z - poseHistory[i].position.z) / dt;

            dev.rotation = Quaternion.Euler((poseHistory[i + 1].rotation.eulerAngles.x - poseHistory[i].rotation.eulerAngles.x) / dt,
                                           (poseHistory[i + 1].rotation.eulerAngles.y - poseHistory[i].rotation.eulerAngles.y) / dt,
                                           (poseHistory[i + 1].rotation.eulerAngles.z - poseHistory[i].rotation.eulerAngles.z) / dt);


            result.Add(dev);
        }

        return result;
    }

    // Calculate the mean error of the trajectory in one axis
    private Vector3 calculateMeanError(List<Vector3> path, float step_size)
    {
        Vector3 sum = new Vector3();
        int count = 0;

        double time = timestamps_log[this.workIndex];
        int index = this.workIndex;
        foreach (Vector3 point in path)
        {
            time += step_size*1000;
            // Interpolate point
            while (timestamps_log[index++] < time);

            if(index >= this.staticRegions[this.staticIndex].end)
            {
                return sum / count;
            }

            double delta_time = timestamps_log[index] - timestamps_log[index - 1];
            Vector3 delta_point = uavPoseHistory_log[index].position - uavPoseHistory_log[index - 1].position;

            Vector3 interpolatedPoint = uavPoseHistory_log[index - 1].position + (delta_point / (float)delta_time) * (float)(time - timestamps_log[index - 1]);
            Vector3 error = point - interpolatedPoint;
            sum += new Vector3(Mathf.Abs(error.x), Mathf.Abs(error.y), Mathf.Abs(error.z));
            count++;
        }
        return sum / count;

    }
}

[Serializable]
public class ParameterRange
{
    public float min;
    public float max;
    public float step_size;
    public float current_value;
    public float best_fit;

    public void saveValue()
    {
        this.best_fit = this.current_value;
    }

    public ulong numberOfSteps()
    {
        return (ulong)((this.max - this.min) / this.step_size);
    }
}
