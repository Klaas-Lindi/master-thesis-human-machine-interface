using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;


// Script to automatically determine good paramters for the PMA using a genetic algorithm
public class PMQuickRunScript : MonoBehaviour
{
    [Tooltip("If this is checked, the script will terminate all other scripts and run the optimization instead.")]
    public bool enableQuickRun;

    [Header("Read Log Settings")]
    [Tooltip("Paths to logs which should be readed")]
    public string[] readLogPaths;
    [Tooltip("The description of each log files which will be added")]
    public float[] setLatencies;
    [Tooltip("Start indices of the corresponding paths")]
    public int[] readLogStartIndices;
    [Tooltip("Count of line which will be readed from logPath. -1 indicates unlimmited")]
    public int[] readLogLineCounts;

    [Header("Write Log Settings")]
    [Tooltip("The comment which should be stored in the log files")]
    public string logComment;
    [Tooltip("The directory where the log files should be stored")]
    public string logDirectory;



    [Header("Parameter settings")]
    [Tooltip("List of latencies which should be calculated for each log file")]
    public float[] latencies;
    public float calculationTimeLimitOfPath;

    [Header("Quick Run Objects")]
    public PMHandler pmHandler; // Currently does nothing, only the PID Model is used
    public LoadLog loadLog;
    public PMAnalyser pmAnalyser;

    private int fileIndex = 0;
    private int latencyIndex = 0;

    private object pmModelProperty;

    void Awake()
    {


    }

    void Start()
    {
        if (this.enableQuickRun)
        {
            Debug.LogWarning("Quick Run is running. Overwritting LoadLog and pmAnalyser is enabled!");

            if (this.readLogPaths.Length == 0 ||
               this.readLogPaths.Length != this.setLatencies.Length ||
               this.readLogPaths.Length != this.readLogStartIndices.Length ||
               this.readLogPaths.Length != this.readLogLineCounts.Length)
            {
                Debug.LogError("Quick Run Log Settings have not equal size!");
                Application.Quit();
            }

            fileIndex = 0;
            pmAnalyser.enableRealTimeAnalyse = false;
            pmAnalyser.startScript = false;
            loadLog.quitAfterLoad = false;
            this.pmModelProperty = pmHandler.GetModelProperties();
            this.setNextRunSettings(fileIndex, latencyIndex);
            
        }
    }

    void Update()
    {
        if(loadLog.lineCounts <= 0 && this.enableQuickRun)
        {
            Debug.Log("Close log file of current run...");
            loadLog.CloseLog();
            pmAnalyser.CloseLog();

            latencyIndex++;

            if(latencyIndex == this.latencies.Length)
            {
                latencyIndex = 0;
                fileIndex++;
                if (fileIndex == this.readLogPaths.Length)
                    Application.Quit();
            }

            Debug.Log("New Log file will be loaded");
            this.setNextRunSettings(fileIndex, latencyIndex);
        }
    }

    void setNextRunSettings(int fileIndex, int latencieIndex)
    {
        loadLog.pathToLog = this.readLogPaths[fileIndex];
        loadLog.skipEntries = this.readLogStartIndices[fileIndex];
        loadLog.lineCounts = this.readLogLineCounts[fileIndex];
        loadLog.InitLog();
        loadLog.logNotInit = false;

        pmAnalyser.logPostfix = this.logComment + "_" + this.setLatencies[fileIndex] + "_" + this.latencies[latencyIndex];
        pmAnalyser.InitLog();

        pmHandler.Reset();
        //pmHandler.SetModelProperties(this.pmModelProperty);
        pmHandler.backwardLatency = this.latencies[latencyIndex] / 2000;
        pmHandler.forwardLatency = this.latencies[latencyIndex] / 2000;        
    }        
}
