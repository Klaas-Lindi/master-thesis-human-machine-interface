using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class LoadLog : MonoBehaviour {

    public PMHandler pmHandler;
    
    // File settings
    public string pathToLog;
        
    private Logger logger;

    private StreamReader stream;

    // Operator State from the operator
    private OperatorState operatorState;

    // Current state of current uav with data from teleoperator
    private UavState currentUavState;

    // Offset from wrong position sending (corrected 2018.08.13 0.15f before)
    public float debugZOffset = 0.15f;

    [Tooltip("Skip Entries from loaded log. At run it indicates the line count of file")]
    public int skipEntries = 100;

    [Tooltip("Load this counts of entries from log. -1 indicates unlimmited")]
    public int lineCounts = -1;

    [Tooltip("If enabled the uav states will be overrided at the beginning")]
    /// <summary>
    /// If enabled the uav states will be overrided
    /// </summary>
    public bool enableOverride = false;

    [Tooltip("If enabled the programm quits after loading data")]
    public bool quitAfterLoad = true;

    [Header("Current UAV Settings")]
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

    [HideInInspector]
    public bool logNotInit;

    // Latency logger
    float old_latency = 0.0f;
    uint latency_count = 0;

    private void Awake()
    {
        if (this.enabled)
        {
            logger = this.GetComponent<Logger>();
            logger.LoggerPath = "log\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_unity_analysis.log";
            logger.LoggerPathMesh = "log\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_unity_mesh_analysis.log";

            // Deactivate network connections automatically

            if(this.GetComponent<TelemetryAndTelecommand>() != null)
                this.GetComponent<TelemetryAndTelecommand>().onlineMode = false;
            if (this.GetComponent<ZEDWrapper>() != null)
                this.GetComponent<ZEDWrapper>().ZedEnabled = false;
            logNotInit = true;
        }
    }

    // Use this for initialization
    void Start()
    {
        Debug.LogWarning("Log loading is active! No real flight! Flight is loaded from: " + pathToLog);

        // Assign Components
        operatorState = logger.operatorState;
        operatorState.ReadFromLog = true;
        currentUavState = this.GetComponent<UavState>();

        if(logNotInit)
            this.InitLog();

        // Override settings if enabled
        if (this.enableOverride)
        {
            currentUavState.Mode = uavMode;
            currentUavState.Condition = uavCondition;
            currentUavState.OperationState = uavOperationState;
            currentUavState.ControlState = uavControlState;
            operatorState.ActiveAutomatism = automatism;
            operatorState.StateChanged = true;
        }
    }
    
       // Update is called once per frame
    void Update () {
        
        if (stream != null && !stream.EndOfStream && lineCounts != 0)
        {
            this.skipEntries++;
            NumberFormatInfo ni = new CultureInfo("en-US").NumberFormat;
            String[] lineArguments = stream.ReadLine().Split(logger.Delimiter.ToCharArray());
            int i = 0;
            // Skip Time because analysis should run as fast as possible
            pmHandler.OverwriteCurrentTime(double.Parse(lineArguments[i++], ni));//lineArguments[i++] // DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).TotalMilliseconds
            i++;//lineArguments[i++] // Time.time
            i++; // Time.realtimeSinceStartup

            // Operator
            operatorState.OperatorPose.position.x = float.Parse(lineArguments[i++], ni);
            operatorState.OperatorPose.position.y = float.Parse(lineArguments[i++], ni);
            operatorState.OperatorPose.position.z = float.Parse(lineArguments[i++], ni);

            float x_rotation = float.Parse(lineArguments[i++], ni);
            if (x_rotation < 0)
                x_rotation = -1 * x_rotation;
            else
                x_rotation = -1* (x_rotation + 360);

            operatorState.OperatorPose.rotation.eulerAngles = new Vector3(x_rotation, float.Parse(lineArguments[i++], ni), float.Parse(lineArguments[i++], ni));

            // Teleoperator
            currentUavState.SetCameraPosition(new Vector3(float.Parse(lineArguments[i++], ni), float.Parse(lineArguments[i++], ni) + debugZOffset, float.Parse(lineArguments[i++], ni)));
            currentUavState.CameraPose.rotation.eulerAngles = new Vector3(float.Parse(lineArguments[i++], ni), float.Parse(lineArguments[i++], ni), float.Parse(lineArguments[i++], ni));

            // Teleoperator Control Commands
            operatorState.Command = lineArguments[i++];

            // Compatibilty Set commands correct to old commands before 2019.03.14
            if (operatorState.Command.Contains("CIRCLE"))
                operatorState.Command = operatorState.Command.Replace("CIRCLE", "POI");
            if (operatorState.Command.Contains("AUTOSCAN"))
                operatorState.Command = operatorState.Command.Replace("AUTOSCAN", "AUTO");
            if (operatorState.Command.Contains("IDLE") && !operatorState.Command.Contains("D:IDLE"))
                operatorState.Command = operatorState.Command.Replace("IDLE", "D:IDLE");
            
            currentUavState.Msg = lineArguments[i++].Replace('\\', '\n');

            // Skip Reconstruktion not needed at the moment
            i++;//lineArguments[i++] //     stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.x).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.y).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.z).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.x.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.z.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(zedwrapper.GetRekonstruktionCameraPose().state.ToString(Format_float) + Delimiter);

            // Skip predictive state because, that is what wie want to recalulate
            i++;//lineArguments[i++] // stream.Write((predictedUavState.CameraPose.position.x).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] //    stream.Write((predictedUavState.CameraPose.position.y).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write((predictedUavState.CameraPose.position.z).ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.x.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.z.ToString(Format_float) + Delimiter);
            i++;//lineArguments[i++] // stream.Write(predictedUavState.CameraPose.state.ToString(Format_float));

            float latency = float.Parse(lineArguments[i++], ni);
            latency += float.Parse(lineArguments[i++], ni);

            if (latency != old_latency)
            {
                currentUavState.TMTCLatency = (latency, latency_count++);
                old_latency = latency;
            }
 
            // Count Number
            lineCounts--;
        }

#if UNITY_EDITOR
        if ((lineCounts == 0 || stream == null || stream.EndOfStream) && quitAfterLoad)
            UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public bool EndOfStream { get => stream.EndOfStream; }

    //-------------------------------------Log Functions----------------------------------------------------
    public void InitLog()
    {
        // Open file
        try
        {
            stream = new StreamReader(pathToLog);
        }
        catch (ArgumentException e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("LogFile at " + pathToLog + " could not be opened!");
        }
        catch (FileNotFoundException e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("LogFile at " + pathToLog + " could not be opened!");
        }

        if (stream != null)
        {
            // Skip Header
            stream.ReadLine();
            stream.ReadLine();
            stream.ReadLine();
        }

        int count = this.skipEntries;
        while (count-- > 0 && stream != null && !stream.EndOfStream)
            stream.ReadLine().Split(logger.Delimiter.ToCharArray());

        logNotInit = false;
    }

    public void CloseLog()
    {
        stream.Close();
    }
}
