using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// This scripts overrides changes and state of uav to test predictive modelling
/// </summary>
public class PMAnalyser : MonoBehaviour {

    [Header("General Settings")]

    [Tooltip("The PM handler which will be analysed")]
    /// <summary>
    /// The PM handler which will be analysed
    /// </summary>
    public PMHandler pmHandler;

    [Tooltip("The predicted UAV State which will be analysed")]
    /// <summary>
    /// The predicted UAV State which will be analysed
    /// </summary>
    public UavState predictedUavState;

    [Tooltip("If enabled the key metrics will be calculated in real time")]
    /// <summary>
    /// If enabled the key metrics will be calculated in real time
    /// </summary>
    public bool enableRealTimeAnalyse = false;
    
    [Tooltip("If enabled the uav states will be overrided")]
    /// <summary>
    /// If enabled the uav states will be overrided
    /// </summary>
    public bool enableOverride = false;

    [Header("Log Settings")]
    [Tooltip("Header informations/comments in log files")]
    /// <summary>
    /// Header informations/comments in log files
    /// </summary>
    public string logComment;
    [Tooltip("Path postfix of log file")]
    /// <summary>
    /// Path postfix of log file
    /// </summary>
    public string logPostfix;
    [Tooltip("If enabled the data will be buffered and then saved (No intime save, and huge memory demand)")]
    /// <summary>
    /// Path postfix of log file
    /// </summary>
    public bool enableBuffering;


    [Header("Real Time Analysis")]
    public float ERTPP;
    public Vector3 ERTPP_Axes;

    public float ERTPR;
    public Vector3 ERTPR_Axes;

    [Header("Post Analysis Settings")]
    public bool startScript;
    public string pathToScript;


    [Header("Current UAV Settings")]
    [Tooltip("The position of uav. The position will be overrided when \"Set Current UAV Position\"-button is pressed")]
    /// <summary>
    /// The position of uav. The position will be overrided when \"Set Current UAV Position\"-button is pressed
    /// </summary>
    public Vector3 uavPosition;

    [Tooltip("UAV Mode is the tmtc state. If waiting the system waits for incomming commands and is not directly controlable. " +
             "Guidance_active means direct control is activated. Guidance_stop indicates the stop procedure has started. " +
             "Reboot means the tmtc server restarts.")]
    /// <summary>
    /// UAV Mode is the tmtc state. If waiting the system waits for incomming commands and is not directly controlable.
    /// Guidance_active means direct control is activated. Guidance_stop indicates the stop procedure has started. 
    /// Reboot means the tmtc server restarts.
    /// </summary>
    public UavState.UavMode uavMode;

    [Tooltip("UAV Condiction explains the enviromental uav state. If LANDED the system is on the ground. FLYING means the uav is operational in the air. " +
         "FREEZE means no incomming command will not be executed until a FLYING or LANDED state was reached " +
         "ERROR means something went wrong.")]
    /// <summary>
    /// UAV Condiction explains the enviromental uav state. If LANDED the system is on the ground. FLYING means the uav is operational in the air.
    /// FREEZE means no incomming command will not be executed until a FLYING or LANDED state was reached
    /// ERROR means something went wrong.
    /// </summary>
    public UavState.UavCondition uavCondition;

    [Tooltip("UAV Operation state indicates what behavior of uav will be expected. If TAKEOFF the system starts and LANDING uav is landing. " +
     "IDLE the uav is hovering the last received position. With TACTIVE the uav follows operator position in the 3D World. " +
     "RTL is Return to Lanch and Land. AUTOSCAN Circles around the last position")]
    /// <summary>
    /// UAV Operation state indicates what behavior of uav will be expected. If TAKEOFF the system starts and LANDING uav is landing.
    /// IDLE the uav is hovering the last received position. With TACTIVE the uav follows operator position in the 3D World. 
    /// RTL is Return to Lanch and Land. AUTOSCAN Circles around the last position
    /// </summary>
    public UavState.UavOperationState uavOperationState;

    [Tooltip("UAV Control state indicates if the uav can be controled remotly or not. MANUAL means the local remote control has" +
        " control or TELEOPERATION means the unity operator has the control")]
        /// <summary>
    /// UAV Control state indicates if the uav can be controled remotly or not. MANUAL means the local remote control has
    /// control or TELEOPERATION means the unity operator has the control
    /// </summary>
    public UavState.UavControlState uavControlState;

    [Header("Current Operator Settings")]
    [Tooltip("Automatism command which will be setted when button \"Set Operator Automatism\" is pressed!")]
    /// <summary>
    /// Automatism command which will be setted when button "Set Operator Automatism" is pressed!
    /// </summary>
    public OperatorState.Automatism automatism;

    // Variables to store state
    private UavState currentUavState;
    private OperatorState operatorState;
    private ParticlePathViewer predictedPathViewer;

    // Use this for initialization

    // Logging variables    
    // Streamer
    private string loggerPath;
    private StreamWriter stream;
    private string delimiter = ";";
    private string format_float = "0.000";
    DateTime startTime;

    // PM Real Time Analysis
    private List<double> timeStamp = new List<double>();
    private List<float> backwardLatencies = new List<float>();
    private List<float> forwardLatencies = new List<float>();
    private List<Pose> operatorPoses = new List<Pose>();
    private List<Pose> uavPoses = new List<Pose>();
    private List<Pose> predictionPoses = new List<Pose>();
    private List<List<Vector3>> predictedPath = new List<List<Vector3>>();

    // Performance measure
    private int index_prediction = 0;
    private double g_time;

    private int pCount = 0;
    private double realTime;
    private double meanRealTimeCyclus;
    private double meanUnityCyclus;

    [HideInInspector]
    public bool logNotInit = true;

    void Start () {
        // Get states
        currentUavState = pmHandler.currentUavState;
        operatorState = pmHandler.operatorState;
        predictedPathViewer = pmHandler.pathViewer;
        g_time = DateTime.Now.TimeOfDay.TotalMilliseconds;
        realTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
        if(logNotInit)
            this.InitLog();
    }




    private float RMS(float a, float b)
    {
        return Mathf.Sqrt(Mathf.Pow(a-b, 2));
    }

    // Update is called once per frame
    void Update () {

        if(!this.enableBuffering)
            writeLog();

        // Make real time analysis
        if (enableRealTimeAnalyse || this.enableBuffering)
        {
            this.timeStamp.Add(pmHandler.GetCurrentTime(false));
            this.backwardLatencies.Add(pmHandler.backwardLatency);
            this.forwardLatencies.Add(pmHandler.forwardLatency);

            this.operatorPoses.Add(operatorState.OperatorPose.Clone());
            this.uavPoses.Add(currentUavState.CameraPose.Clone());
            this.predictionPoses.Add(predictedUavState.CameraPose.Clone());

            List<Vector3> list = new List<Vector3>();
            foreach (Vector3 tmp in predictedPathViewer.Waypoints)
                list.Add(tmp);
            this.predictedPath.Add(list);

            //print(this.uavPoses[0].position);
        }
        if (enableRealTimeAnalyse)
        {
                if (pmHandler.GetCurrentTime() > (this.timeStamp[index_prediction] + this.backwardLatencies[index_prediction] + this.forwardLatencies[index_prediction]))
            {
                ERTPP = Vector3.Distance(predictionPoses[index_prediction].position, currentUavState.CameraPose.position);
                ERTPP_Axes.x = RMS(predictionPoses[index_prediction].position.x, currentUavState.CameraPose.position.x);
                ERTPP_Axes.y = RMS(predictionPoses[index_prediction].position.y, currentUavState.CameraPose.position.y);
                ERTPP_Axes.z = RMS(predictionPoses[index_prediction].position.z, currentUavState.CameraPose.position.z);

                ERTPR_Axes = new Vector3(
            (Mathf.DeltaAngle((predictionPoses[index_prediction].rotation.eulerAngles.x), currentUavState.CameraPose.rotation.eulerAngles.x)),
            (Mathf.DeltaAngle((predictionPoses[index_prediction].rotation.eulerAngles.y), currentUavState.CameraPose.rotation.eulerAngles.y)),
            (Mathf.DeltaAngle((predictionPoses[index_prediction].rotation.eulerAngles.z), currentUavState.CameraPose.rotation.eulerAngles.z)));
                ERTPR = Mathf.Sqrt(Mathf.Pow(ERTPR_Axes.x,2) + Mathf.Pow(ERTPR_Axes.y, 2) + Mathf.Pow(ERTPR_Axes.z, 2));

                index_prediction++;
            }
        }

        // Override only when enabled
        if (enableOverride)
        {
            // Override settings
            currentUavState.Mode = uavMode;
            currentUavState.Condition = uavCondition;
            currentUavState.OperationState = uavOperationState;
            currentUavState.ControlState = uavControlState;           
        }
        pCount++;
        // Performance measure
        if (pCount == 2)
        {
            meanRealTimeCyclus = (DateTime.Now.TimeOfDay.TotalMilliseconds - realTime);
            meanUnityCyclus = Time.unscaledDeltaTime;
        }
        if (pCount > 2)
        {
            meanRealTimeCyclus += (((DateTime.Now.TimeOfDay.TotalMilliseconds - realTime)- meanRealTimeCyclus) / (pCount-1));
            meanUnityCyclus += ((Time.unscaledDeltaTime- meanRealTimeCyclus) / (pCount - 1));
        }

        realTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
    }

    //Inspector Methods
    public void SetUavPostition()
    {
        // Override only when enabled
        if (enableOverride)
        {
            // Set Position
            currentUavState.SetVehiclePosition(uavPosition);
        }
    }

    public void SetOperatorAutomatism()
    {
        // Override only when enabled
        if (enableOverride)
        {
            // Set automatism
            operatorState.ActiveAutomatism = automatism;
            operatorState.StateChanged = true;
        }
    }

    public void InitLog(string logDirectory = null)
    {
        // Set Stream
        if (logDirectory == null)
            this.loggerPath = "log\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_unity_pm_" + logPostfix + ".log";
        else
            this.loggerPath = logDirectory + logPostfix + ".log";

        stream = new StreamWriter(this.loggerPath);

        //Header
        stream.WriteLine("Comment:");
        stream.WriteLine(this.logComment);

        //Description
        stream.Write("Time-Stamp [ms]" + delimiter);
        stream.Write("Forward Latency [s]" + delimiter);
        stream.Write("Backward Latency [s]" + delimiter);

        // Operator
        stream.Write("Op Pos X [m]" + delimiter);
        stream.Write("Op Pos Y [m]" + delimiter);
        stream.Write("Op Pos Z [m]" + delimiter);
        stream.Write("Op Ang X" + delimiter);
        stream.Write("Op Ang Y" + delimiter);
        stream.Write("Op Ang Z" + delimiter);

        // Teleoperator
        stream.Write("TOp Pos X [m]" + delimiter);
        stream.Write("TOp Pos Y [m]" + delimiter);
        stream.Write("TOp Pos Z [m]" + delimiter);
        stream.Write("TOp Ang X" + delimiter);
        stream.Write("TOp Ang Y" + delimiter);
        stream.Write("TOp Ang Z" + delimiter);

        // Predicted Pose
        stream.Write("P Pos X [m]" + delimiter);
        stream.Write("P Pos Y [m]" + delimiter);
        stream.Write("P Pos Z [m]" + delimiter);
        stream.Write("P Ang X" + delimiter);
        stream.Write("P Ang Y" + delimiter);
        stream.Write("P Ang Z" + delimiter);

        stream.Write("Predicted Path" + delimiter);

        stream.Write("\r\n");
        stream.Flush();

        logNotInit = false;
    }
    private void writeLog()
    {
        if (predictedPathViewer.Waypoints.Count <= 1)
            return;

        NumberFormatInfo ni = new CultureInfo("en-US").NumberFormat;

        try
        {

            // Time
            stream.Write(pmHandler.GetCurrentTime().ToString(format_float, ni) + delimiter);
            stream.Write(pmHandler.forwardLatency.ToString(format_float, ni) + delimiter);
            stream.Write(pmHandler.backwardLatency.ToString(format_float, ni) + delimiter);


            stream.Write((operatorState.OperatorPose.position.x).ToString(format_float, ni) + delimiter);
            stream.Write((operatorState.OperatorPose.position.y).ToString(format_float, ni) + delimiter);
            stream.Write((operatorState.OperatorPose.position.z).ToString(format_float, ni) + delimiter);
            if (operatorState.OperatorPose.rotation.eulerAngles.x < 90)
                stream.Write((-operatorState.OperatorPose.rotation.eulerAngles.x).ToString(format_float, ni) + delimiter);
            else
                stream.Write((-(operatorState.OperatorPose.rotation.eulerAngles.x - 360)).ToString(format_float, ni) + delimiter);
            stream.Write(operatorState.OperatorPose.rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
            stream.Write(operatorState.OperatorPose.rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

            // Teleoperator
            stream.Write((currentUavState.CameraPose.position.x).ToString(format_float, ni) + delimiter);
            stream.Write((currentUavState.CameraPose.position.y).ToString(format_float, ni) + delimiter);
            stream.Write((currentUavState.CameraPose.position.z).ToString(format_float, ni) + delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.x.ToString(format_float, ni) + delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

            // Teleoperator
            stream.Write((predictedUavState.CameraPose.position.x).ToString(format_float, ni) + delimiter);
            stream.Write((predictedUavState.CameraPose.position.y).ToString(format_float, ni) + delimiter);
            stream.Write((predictedUavState.CameraPose.position.z).ToString(format_float, ni) + delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.x.ToString(format_float, ni) + delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

            foreach(Vector3 waypoint in predictedPathViewer.Waypoints)
            {
                stream.Write("(");
                stream.Write((waypoint.x).ToString(format_float, ni) + ",");
                stream.Write((waypoint.y).ToString(format_float, ni) + ",");
                stream.Write((waypoint.z).ToString(format_float, ni) + "),");
            }
        }
        catch (NullReferenceException e)
        {
            print(e.Message);
        }
        finally
        {
            stream.Write("\r\n");
            stream.Flush();
        }
    }
    public void CloseLog()
    {
        if (this.enableBuffering)
        {
            for (int index = 0; index < this.timeStamp.Count; index++)
            {
                if (this.predictedPath[index].Count <= 1)
                    continue;

                try
                {
                    NumberFormatInfo ni = new CultureInfo("en-US").NumberFormat;
                    // Time
                    stream.Write(this.timeStamp[index].ToString(format_float, ni) + delimiter);
                    stream.Write(this.forwardLatencies[index].ToString(format_float, ni) + delimiter);
                    stream.Write(this.backwardLatencies[index].ToString(format_float, ni) + delimiter);


                    stream.Write((this.operatorPoses[index].position.x).ToString(format_float, ni) + delimiter);
                    stream.Write((this.operatorPoses[index].position.y).ToString(format_float, ni) + delimiter);
                    stream.Write((this.operatorPoses[index].position.z).ToString(format_float, ni) + delimiter);
                    if (this.operatorPoses[index].rotation.eulerAngles.x < 90)
                        stream.Write((-this.operatorPoses[index].rotation.eulerAngles.x).ToString(format_float, ni) + delimiter);
                    else
                        stream.Write((-(this.operatorPoses[index].rotation.eulerAngles.x - 360)).ToString(format_float, ni) + delimiter);
                    stream.Write(this.operatorPoses[index].rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
                    stream.Write(this.operatorPoses[index].rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

                    // Teleoperator
                    stream.Write((this.uavPoses[index].position.x).ToString(format_float, ni) + delimiter);
                    stream.Write((this.uavPoses[index].position.y).ToString(format_float, ni) + delimiter);
                    stream.Write((this.uavPoses[index].position.z).ToString(format_float, ni) + delimiter);
                    stream.Write(this.uavPoses[index].rotation.eulerAngles.x.ToString(format_float, ni) + delimiter);
                    stream.Write(this.uavPoses[index].rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
                    stream.Write(this.uavPoses[index].rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

                    // Teleoperator
                    stream.Write((this.predictionPoses[index].position.x).ToString(format_float, ni) + delimiter);
                    stream.Write((this.predictionPoses[index].position.y).ToString(format_float, ni) + delimiter);
                    stream.Write((this.predictionPoses[index].position.z).ToString(format_float, ni) + delimiter);
                    stream.Write(this.predictionPoses[index].rotation.eulerAngles.x.ToString(format_float, ni) + delimiter);
                    stream.Write(this.predictionPoses[index].rotation.eulerAngles.y.ToString(format_float, ni) + delimiter);
                    stream.Write(this.predictionPoses[index].rotation.eulerAngles.z.ToString(format_float, ni) + delimiter);

                    foreach (Vector3 waypoint in this.predictedPath[index])
                    {
                        stream.Write("(");
                        stream.Write((waypoint.x).ToString(format_float, ni) + ",");
                        stream.Write((waypoint.y).ToString(format_float, ni) + ",");
                        stream.Write((waypoint.z).ToString(format_float, ni) + "),");
                    }
                }
                catch (NullReferenceException e)
                {
                    print(e.Message);
                }
                finally
                {
                    stream.Write("\r\n");
                }
                
            }
            try
            {
                stream.Flush();
                stream.Close();
            }
            catch (ObjectDisposedException e)
            {
                print(e.Message);
            }
        }
       
        // PM Real Time Analysis
        this.timeStamp.Clear();
        this.backwardLatencies.Clear();
        this.forwardLatencies.Clear();
        this.operatorPoses.Clear();
        this.uavPoses.Clear();
        this.predictionPoses.Clear();
        this.predictedPath.Clear();
    }
    private void OnApplicationQuit()
    {
        this.CloseLog();
        
        if (this.startScript && this.enabled)
        {
            string strCmdText = "";
            if (this.pmHandler.model == PMHandler.Model.LSTMSingleOutput || this.pmHandler.model == PMHandler.Model.LSTMSingleOutputLC || this.pmHandler.model == PMHandler.Model.LSTMMultiOutputLC)
                strCmdText = "/c python " + this.pathToScript + " " + this.loggerPath + " 0 -1 22";
            else
                strCmdText = "/c python " + this.pathToScript + " " + this.loggerPath + " 0 -1 "+ (this.pmHandler.predictionTimeStep*1000);
            // Terminal 
            Debug.LogWarning("Start Analysis Script: " + strCmdText);
            System.Diagnostics.Process.Start("CMD.exe ", strCmdText);
        }
        print("Performance data: \n" + 
              "Overall runtime: " + (DateTime.Now.TimeOfDay.TotalMilliseconds - g_time) + "\n" +
              "Prediction horizon: " + pmHandler.calculationTimeLimitOfPath + "\n" +
              "Prediction time step: " + pmHandler.predictionTimeStep + "\n" +
              "Frame rate unity: " + meanUnityCyclus + "\n" +
              "Frame rate realtime: " + meanRealTimeCyclus);

    }
}
