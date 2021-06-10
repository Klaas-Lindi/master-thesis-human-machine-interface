
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Globalization;

// Script to compute the Static Regions of a log file and export them
public class PMOptStaticRegionAnalyzer : MonoBehaviour
{
    [Tooltip("If this is checked, the script will terminate all other scripts and run the optimization instead.")]
    public bool enableOptimizerMode;
    public bool writeLog;
    public string logComment;

    [Tooltip ("Insert the path to the flight log here.")]
    public string pathToLog;
    [Tooltip("Skip the first n entries from loaded log. At run time it indicates the line count of file")]
    public int skipEntries = 0;
    [Tooltip("Load this counts of entries from log. -1 indicates unlimmited")]
    public int lineCounts = -1;

    private float debugZOffset = 0.15f;

    private StreamReader streamReader;
    private StreamWriter streamWriter;

    // LoadLog() Lists
    private List<double> timestamps_log = new List<double>();
    private List<Pose> opPoseHistory_log = new List<Pose>();
    private List<Pose> uavPoseHistory_log = new List<Pose>();

    // Lists for SR Calculation
    private List<Pose> operatorDerivs;
    private List<StaticRegion> staticRegions;

    private void Awake()
    { 

        if (enableOptimizerMode)
        {

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

            // Load the logfile into Lists
            LoadLog();

            // Calculate the derivative of operator movement
            operatorDerivs = CalculateDerivativesSR(opPoseHistory_log);

            // Identify and store the static regions
            staticRegions = CalculateStaticRegions(operatorDerivs);

            List<StaticRegion> staticRegions_cleaned = new List<StaticRegion>();
            // Process static regions
            for (int n = 0; n < staticRegions.Count; n++)
            {
                for (int i = staticRegions[n].start; i < staticRegions[n].end; i++)
                {
                    staticRegions[n].uavPath.Add(uavPoseHistory_log[i].position); // Add path info
                }
                staticRegions[n].CalculateMovement();
                if (staticRegions[n].size > 30) // Discard very small Regions
                {
                    staticRegions_cleaned.Add(staticRegions[n]);
                }
            }
            staticRegions = staticRegions_cleaned;

            // Output Regions to console
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t List of SRs found in logfile:\n");
            for (int i = 0; i < staticRegions.Count; i++)
            { Debug.LogFormat("SR#{0}: \t{1}\n", i, staticRegions[i]); }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n");


            #region WriteLog
            if (writeLog) // Output Regions to a .csv file
            {
                streamWriter = new StreamWriter("log/optimizerLogs/SR_ANALYSIS" + "_" + logComment + ".csv");

                streamWriter.WriteLine(logComment);
                streamWriter.WriteLine("SR Start,SR End,Path XYZ");

                for (int reg = 0; reg < staticRegions.Count; reg++)
                {
                    streamWriter.Write(staticRegions[reg].start + "," + staticRegions[reg].end);

                    //X
                    for (int pInd = 0; pInd < staticRegions[reg].uavPath.Count; pInd++)
                    {
                        streamWriter.Write("," + staticRegions[reg].uavPath[pInd].x.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    streamWriter.WriteLine();

                    //Y
                    streamWriter.Write(",");
                    for (int pInd = 0; pInd < staticRegions[reg].uavPath.Count; pInd++)
                    {
                        streamWriter.Write("," + staticRegions[reg].uavPath[pInd].y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    streamWriter.WriteLine();

                    //Z
                    streamWriter.Write(",");
                    for (int pInd = 0; pInd < staticRegions[reg].uavPath.Count; pInd++)
                    {
                        streamWriter.Write("," + staticRegions[reg].uavPath[pInd].z.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    streamWriter.WriteLine();
                }


                streamWriter.Flush();
            }
            #endregion
        }
    }

    #region LogAndStaticRegions
    // Loads the specified lines from the log-file into histories
    private void LoadLog()
    {
        // Open Logfile
        try
        {
            streamReader = new StreamReader(pathToLog);
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
        int skipCount = this.skipEntries;
        while (skipCount-- > 0 && streamReader != null && !streamReader.EndOfStream)
        {
            streamReader.ReadLine().Split(';');
            nextReaderLine++;
        }

        if (lineCounts < 0) // -1 -> Load complete log
        {
            int line = nextReaderLine;
            while (streamReader != null && !streamReader.EndOfStream)
            {
                //Debug.LogFormat("Line to be read: {0} (Entry Nr. {1})", line, line - 3);
                this.skipEntries++; // Show entry number in inspector

                double timestamp;

                //String operatorCommand;
                //String uavCommand;

                Pose operatorPose = new Pose();
                Pose uavPose = new Pose();

                String[] lineArguments = streamReader.ReadLine().Split(';');//logger.Delimiter.ToCharArray());

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
                uavPose.position.y = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture) + debugZOffset;
                uavPose.position.z = float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture);

                uavPose.rotation.eulerAngles = new Vector3(float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture), float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture), float.Parse(lineArguments[i++], System.Globalization.CultureInfo.InvariantCulture));

                // (Commands, reconstruction and prediction log entries are ignored)

                // Add the contents of the line to Lists
                timestamps_log.Add(timestamp);
                opPoseHistory_log.Add(operatorPose);
                uavPoseHistory_log.Add(uavPose);

                // feed the operator poses to the PMModelwrapper object
                //pmModelWrapper.OperatorPoseHistory.Add(timestamp, operatorPose); // not needed for SR Analysis
                line++;
            }
            Debug.LogWarningFormat("End of Stream reached at line {0}. (Entry Nr. {1})", line, line - 3);
        }
        else
        {
            // Loop over lines
            for (int line = nextReaderLine; line < nextReaderLine + lineCounts; line++)
            {
                if (streamReader != null && !streamReader.EndOfStream)
                {
                    this.skipEntries++; // Show entry number in inspector

                    double timestamp;

                    //String operatorCommand;
                    //String uavCommand;

                    Pose operatorPose = new Pose();
                    Pose uavPose = new Pose();

                    String[] lineArguments = streamReader.ReadLine().Split(';');//logger.Delimiter.ToCharArray());

                    int i = 0; // Index for lineArguments
                    timestamp = double.Parse(lineArguments[i++]);//pmHandler.OverwriteCurrentTime(double.Parse(lineArguments[arg++])); // Get the Real-Datetime value in ms
                    i++; // Skip Unity-Time since startup
                    i++; // Skip Real-Time since startup



                    // Operator
                    operatorPose.position.x = float.Parse(lineArguments[i++]);
                    operatorPose.position.y = float.Parse(lineArguments[i++]);
                    operatorPose.position.z = float.Parse(lineArguments[i++]);

                    float x_rotation = float.Parse(lineArguments[i++]);
                    if (x_rotation < 0)
                        x_rotation = -1 * x_rotation;
                    else
                        x_rotation = -1 * (x_rotation + 360);
                    operatorPose.rotation.eulerAngles = new Vector3(x_rotation, float.Parse(lineArguments[i++]), float.Parse(lineArguments[i++]));

                    // Teleoperator
                    uavPose.position.x = float.Parse(lineArguments[i++]);
                    uavPose.position.y = float.Parse(lineArguments[i++]) + debugZOffset;
                    uavPose.position.z = float.Parse(lineArguments[i++]);

                    uavPose.rotation.eulerAngles = new Vector3(float.Parse(lineArguments[i++]), float.Parse(lineArguments[i++]), float.Parse(lineArguments[i++]));

                    // (Commands, reconstruction and prediction log entries are ignored)


                    // Add the contents of the line to Lists
                    timestamps_log.Add(timestamp);
                    opPoseHistory_log.Add(operatorPose);
                    uavPoseHistory_log.Add(uavPose);

                    // feed the operator poses to the PMModelwrapper object
                    //pmModelWrapper.OperatorPoseHistory.Add(timestamp, operatorPose); // not needed for SR Analysis

                }
                else if (streamReader.EndOfStream)
                {
                    Debug.LogWarningFormat("End of Stream reached at line {0}. (Entry Nr. {1})", line, line - 3);
                    break;
                }
                else
                {
                    Debug.LogWarningFormat("stream = null");
                    break;
                }
            }
        }
    }

    // Returns the regions where the operator is not moving
    private List<StaticRegion> CalculateStaticRegions(List<Pose> deviations)
    {
        List<StaticRegion> staticRegions = new List<StaticRegion>();

        int startingIndex = 0;
        bool isStatic = false;

        // Loop over derivative List
        for (int i = 0; i < deviations.Count; i++)
        {
            // If derivative is zero
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
                staticRegions.Add(staticRegion);
                isStatic = false;
            }

        }

        if (isStatic) // If we are in a static region at the last element, the region gets cut off and saved
        {
            StaticRegion staticRegion = new StaticRegion(startingIndex, deviations.Count, deviations.Count - startingIndex);
            staticRegions.Add(staticRegion);
        }

        return staticRegions;
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

            //dev.rotation.x = (poseHistory[i + 1].rotation.x - poseHistory[i].rotation.x) / dt;
            //dev.rotation.y = (poseHistory[i + 1].rotation.y - poseHistory[i].rotation.y) / dt;
            //dev.rotation.z = (poseHistory[i + 1].rotation.z - poseHistory[i].rotation.z) / dt;
            //dev.rotation.w = (poseHistory[i + 1].rotation.w - poseHistory[i].rotation.w) / dt;

            dev.rotation = Quaternion.Euler((poseHistory[i + 1].rotation.eulerAngles.x - poseHistory[i].rotation.eulerAngles.x) / dt,
                                        (poseHistory[i + 1].rotation.eulerAngles.y - poseHistory[i].rotation.eulerAngles.y) / dt,
                                        (poseHistory[i + 1].rotation.eulerAngles.z - poseHistory[i].rotation.eulerAngles.z) / dt);
                                      
            result.Add(dev);
        }

        return result;
    }
    #endregion
}