using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Logger : MonoBehaviour {

    // Current state of current uav with data from teleoperator
    private UavState currentUavState;

    // Operator State from the operator
    public OperatorState operatorState;

    // Rekonstruktionsstate
    private ZEDWrapper zedwrapper;

    // Prediction Pose
    public UavState predictedUavState;

    public PMHandler pmHandler;

    // Debug Console UI
    public Text debugText;

    // Streamer
    private StreamWriter stream;
    private StreamWriter streamMesh;
    private string delimiter = ";";
    private string format_float = "0.000";
    DateTime startTime;

    // LoggerValues
    private string lastCommand = "";
    private string console_format_float = "000.0";

    // Properites
    public bool enableConsoleInfo = true;
    public bool enableMeshLog = false;

    // Path
    private string loggerPath = null;

    public string LoggerPath
    {
        get
        {
            return loggerPath;
        }

        set
        {
            loggerPath = value;
        }
    }

    public string Delimiter
    {
        get
        {
            return delimiter;
        }

        set
        {
            delimiter = value;
        }
    }

    public string Format_float
    {
        get
        {
            return format_float;
        }

        set
        {
            format_float = value;
        }
    }

    public string LoggerPathMesh { get; set; }

    // Use this for initialization
    void Start () {
        currentUavState = this.GetComponent<UavState>();
        zedwrapper = this.GetComponent<ZEDWrapper>();
        if (this.LoggerPath == null)
        {
            this.LoggerPath = "log\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_unity_tmtc.log";
            this.LoggerPathMesh = "log\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_unity_mesh.log";
        }
        stream = new StreamWriter(this.LoggerPath);
        streamMesh = new StreamWriter(this.LoggerPathMesh);

        //Header
        stream.WriteLine("Description:");
        stream.WriteLine("X and Z are the horizontal plane. Y is the height. Rotation are given as Euler. " +
                         "First Position of operaterr, then from the teleoperator ");

        //Description
        stream.Write("Real-Datetime in [ms]" + Delimiter);
        stream.Write("Unity-Time since startup in [s]" + Delimiter);
        stream.Write("Real-Time since startup in [s]" + Delimiter);

        // Operator
        stream.Write("Op Pos X [m]" + Delimiter);
        stream.Write("Op Pos Y [m]" + Delimiter);
        stream.Write("Op Pos Z [m]" + Delimiter);
        stream.Write("Op Ang X" + Delimiter);
        stream.Write("Op Ang Y" + Delimiter);
        stream.Write("Op Ang Z" + Delimiter);

        // Teleoperator
        stream.Write("TOp Pos X [m]" + Delimiter);
        stream.Write("TOp Pos Y [m]" + Delimiter);
        stream.Write("TOp Pos Z [m]" + Delimiter);
        stream.Write("TOp Ang X" + Delimiter);
        stream.Write("TOp Ang Y" + Delimiter);
        stream.Write("TOp Ang Z" + Delimiter);

        // Teleoperator Control Commands
        stream.Write("Op CMD" + Delimiter);
        stream.Write("TOp CMD" + Delimiter);

        // Rekonstruktions Camera Orientation
        stream.Write("ReCam Pos X [m]" + Delimiter);
        stream.Write("ReCam Pos Y [m]" + Delimiter);
        stream.Write("ReCam Pos Z [m]" + Delimiter);
        stream.Write("ReCam Ang X" + Delimiter);
        stream.Write("ReCam Ang Y" + Delimiter);
        stream.Write("ReCam Ang Z" + Delimiter);
        stream.Write("ReCam State" + Delimiter);

        // Prediction
        stream.Write("Prediction Pos X [m]" + Delimiter);
        stream.Write("Prediction Pos Y [m]" + Delimiter);
        stream.Write("Prediction Pos Z [m]" + Delimiter);
        stream.Write("Prediction Ang X" + Delimiter);
        stream.Write("Prediction Ang Y" + Delimiter);
        stream.Write("Prediction Ang Z" + Delimiter);
        stream.Write("Prediction State" + Delimiter);

        // Latency
        stream.Write("Forward Latency [s]" + Delimiter);
        stream.Write("Backward Latency [s]");

        stream.Write("\r\n");
        stream.Flush();
    }
	
	// Update is called once per frame
	void Update () {
        // Time
        stream.Write(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds.ToString() + Delimiter);
        stream.Write(Time.time.ToString(Format_float) + Delimiter);
        stream.Write(Time.realtimeSinceStartup.ToString(Format_float) + Delimiter);

        // Operator
        try
        {
            stream.Write((operatorState.OperatorPose.position.x).ToString(Format_float) + Delimiter);
            stream.Write((operatorState.OperatorPose.position.y).ToString(Format_float) + Delimiter);
            stream.Write((operatorState.OperatorPose.position.z).ToString(Format_float) + Delimiter);
            if (operatorState.OperatorPose.rotation.eulerAngles.x < 90)
                stream.Write((-operatorState.OperatorPose.rotation.eulerAngles.x).ToString(Format_float) + Delimiter);
            else
                stream.Write((-(operatorState.OperatorPose.rotation.eulerAngles.x - 360)).ToString(Format_float) + Delimiter);
            stream.Write(operatorState.OperatorPose.rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            stream.Write(operatorState.OperatorPose.rotation.eulerAngles.z.ToString(Format_float) + Delimiter);

            // Teleoperator
            stream.Write((currentUavState.CameraPose.position.x).ToString(Format_float) + Delimiter);
            stream.Write((currentUavState.CameraPose.position.y).ToString(Format_float) + Delimiter);
            stream.Write((currentUavState.CameraPose.position.z).ToString(Format_float) + Delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.x.ToString(Format_float) + Delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            stream.Write(currentUavState.CameraPose.rotation.eulerAngles.z.ToString(Format_float) + Delimiter);

            // Teleoperator Control Commands
            stream.Write(operatorState.Command + Delimiter);
            stream.Write(currentUavState.Msg.Replace('\n', '\\') + Delimiter);

            // Rekonstruktion
            stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.x).ToString(Format_float) + Delimiter);
            stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.y).ToString(Format_float) + Delimiter);
            stream.Write((zedwrapper.GetRekonstruktionCameraPose().position.z).ToString(Format_float) + Delimiter);
            stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.x.ToString(Format_float) + Delimiter);
            stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            stream.Write(zedwrapper.GetRekonstruktionCameraPose().rotation.eulerAngles.z.ToString(Format_float) + Delimiter);
            stream.Write(zedwrapper.GetRekonstruktionCameraPose().state.ToString(Format_float) + Delimiter);

            // Teleoperator
            stream.Write((predictedUavState.CameraPose.position.x).ToString(Format_float) + Delimiter);
            stream.Write((predictedUavState.CameraPose.position.y).ToString(Format_float) + Delimiter);
            stream.Write((predictedUavState.CameraPose.position.z).ToString(Format_float) + Delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.x.ToString(Format_float) + Delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.y.ToString(Format_float) + Delimiter);
            stream.Write(predictedUavState.CameraPose.rotation.eulerAngles.z.ToString(Format_float) + Delimiter);
            stream.Write(predictedUavState.CameraPose.state.ToString(Format_float) + Delimiter);

            // Time
            stream.Write(pmHandler.forwardLatency.ToString(Format_float) + Delimiter);
            stream.Write(pmHandler.backwardLatency.ToString(Format_float));

            // Print States
            if (operatorState.Command.Length > 0)
                lastCommand = operatorState.Command;

            string debugMessage = "Operator: " + operatorState.OperatorPose.rotation.eulerAngles.x.ToString(console_format_float) + "; " + operatorState.OperatorPose.rotation.eulerAngles.y.ToString(console_format_float) + "; " + operatorState.OperatorPose.rotation.eulerAngles.z.ToString(console_format_float) + " | " + operatorState.OperatorPose.position.x.ToString(console_format_float) + "; " + operatorState.OperatorPose.position.y.ToString(console_format_float) + "; " + operatorState.OperatorPose.position.z.ToString(console_format_float)
                 + " || Teleoperator: " + currentUavState.CameraPose.rotation.eulerAngles.x.ToString(console_format_float) + "; " + currentUavState.CameraPose.rotation.eulerAngles.y.ToString(console_format_float) + "; " + currentUavState.CameraPose.rotation.eulerAngles.z.ToString(console_format_float) + " | " + currentUavState.CameraPose.position.x.ToString(console_format_float) + "; " + currentUavState.CameraPose.position.y.ToString(console_format_float) + "; " + currentUavState.CameraPose.position.z.ToString(console_format_float)
                 + " || Cmd: " + lastCommand + "; Sta:" + Enum.GetName(typeof(UavState.UavCondition), currentUavState.Condition) + "; Con:" + Enum.GetName(typeof(UavState.UavControlState), currentUavState.ControlState) + "; Bat:" + currentUavState.Battery.ToString(console_format_float);

            if (enableConsoleInfo)
            {
                print(debugMessage);
            }

            debugText.text = debugMessage;
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

    internal void logMesh(List<MeshUpdate> updates)
    {
        if(enableMeshLog)
        {
            streamMesh.Write(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds.ToString() + Delimiter);
            streamMesh.Write(updates.Count.ToString() + Delimiter);
            foreach (MeshUpdate update in updates)
            {
                streamMesh.Write(update.MeshId.ToString() + Delimiter);
                streamMesh.Write(update.Mesh.vertices.Length.ToString() + Delimiter);
                streamMesh.Write(update.Mesh.triangles.Length.ToString() + Delimiter);
                foreach(Vector3 vertex in update.Mesh.vertices)
                {
                    streamMesh.Write(vertex.ToString() + Delimiter);
                }
                foreach (int triangle in update.Mesh.triangles)
                {
                    streamMesh.Write(triangle.ToString() + Delimiter);
                }
            }
            streamMesh.Write("\r\n");
            streamMesh.Flush();
        }
    }
}
