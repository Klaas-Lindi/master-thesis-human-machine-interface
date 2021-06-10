using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PMSensivityAnalysis : MonoBehaviour
{
    [Tooltip("If this is checked, the script will terminate all other scripts and run the optimization instead.")]
    public bool enableSensitivityAnalysis;

    [Header("Sensitivity Analysis Objects")]
    public PMHandler pmHandler;
    public LoadLog loadLog;
    public PMAnalyser pmAnalyser;

    [Header("Read Log Settings")]
    [Tooltip("Paths to log which should be readed")]
    public string readLogPath;
    [Tooltip("Start index of the log file")]
    public int readLogStartIndex;
    [Tooltip("Count of line which will be readed from log. -1 indicates unlimmited")]
    public int readLogLineCount;

    [Header("Write Log Settings")]
    [Tooltip("The log postfix which should be stored in the log file path")]
    public string logPrefix;
    [Tooltip("The comment which should be stored in the log files")]
    public string logComment;
    [Tooltip("The directory where the log files should be stored")]
    public string logDirectory;

    /*
    [Header("Model Settings")]
    [Tooltip("The prediction horizon of the model")]
    public float calculationTimeLimitOfPath;
    [Tooltip("The prediction time step")]
    public float timeStep;*/

    public enum ParameterSelection { None, P, I, D, V, L, A, O, M, K, APG_D };
    [Header("Parameter settings")]
    [Tooltip("Parameter which should be analysed. P: P-Parameter, I: I-Parameter, D: D-Parameter, V: Vmax-Parameter, L: Latency")]
    public ParameterSelection parameter;
    [Tooltip("Use the values as precentage of the original value")]
    public bool valuesInPrecentage;
    [Tooltip("List of values which should be calculated for the log file")]
    public float[] values;

    [Header("Model properties")]
    public PMPIDProperties pidProperties;
    public PMAdvPIDProperties advPidProperties;
    public PMAPGProperties apgProperties;

    private int valueIndex = 0;
    private int counterSetting = 0;

    private object pmModelProperty;
    private float forwardLatency;
    private float backwardLatency;

    void Awake()
    {
        if (this.enableSensitivityAnalysis)
        {
            Debug.LogWarning("Sensitivity Analysis is running. Overwritting LoadLog and pmAnalyser is enabled!");

            /*
            if (this.readLogPath.Length == 0 ||
               this.readLogPaths.Length != this.setLatencies.Length ||
               this.readLogPaths.Length != this.readLogStartIndices.Length ||
               this.readLogPaths.Length != this.readLogLineCounts.Length)
            {
                Debug.LogError("Quick Run Log Settings have not equal size!");
                Application.Quit();
            }*/

            valueIndex = 0;
            pmAnalyser.enableRealTimeAnalyse = false;
            pmAnalyser.startScript = false;
            loadLog.quitAfterLoad = false;
        }
    }

    void Start()
    {
        if (this.enableSensitivityAnalysis)
        {
            pmHandler.operatorState.StateChanged = false;
            this.pmModelProperty = pmHandler.GetModelProperties();
            this.forwardLatency = pmHandler.forwardLatency;
            this.backwardLatency = pmHandler.backwardLatency;
            this.setNextRunSettings();
        }
    }

    void Update()
    {

        if (loadLog.lineCounts <= 0 && this.enableSensitivityAnalysis)
        {
            Debug.Log("Close log file of current run...");
            loadLog.CloseLog();
            pmAnalyser.CloseLog();

            valueIndex++;

            if (valueIndex == this.values.Length)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            Debug.Log("New Log file will be loaded");
            if (valueIndex != this.values.Length)
                this.setNextRunSettings();
        }
    }

    void setNextRunSettings()
    {
        pmHandler.Reset();
        setProperties();

        counterSetting = 0;

        loadLog.pathToLog = this.readLogPath;
        loadLog.skipEntries = this.readLogStartIndex;
        loadLog.lineCounts = this.readLogLineCount;
        loadLog.InitLog();
        loadLog.logNotInit = false;

        pmAnalyser.logPostfix = this.logPrefix + "_" + parameter.ToString() + "_" + this.values[this.valueIndex];
        pmAnalyser.logComment = this.logComment + 
                               "| RunDate: " + DateTime.Now.ToString("yyyyMMddHHmmss") +
                               "| Senstivity Analysis with " + readLogPath + ". Used parameter" + parameter.ToString() + " with " + this.values[this.valueIndex] +
                               "| Original parameter: " + getPropertyString();
        pmAnalyser.InitLog(this.logDirectory);

  



        //pmHandler.SetModelProperties(this.pmModelProperty);
        //pmHandler.backwardLatency = this.latencies[latencyIndex] / 2000;
        //pmHandler.forwardLatency = this.latencies[latencyIndex] / 2000;
    }

    string getPropertyString()
    {
        switch (pmHandler.model)
        {
            case PMHandler.Model.PID: return ((PMPIDProperties)pmHandler.GetModelProperties()).getCommentString(); break;
            case PMHandler.Model.AdvPID: return ((PMAdvPIDProperties)pmHandler.GetModelProperties()).getCommentString(); break;
            case PMHandler.Model.APG: return ((PMAPGProperties)pmHandler.GetModelProperties()).getCommentString(); break;
            case PMHandler.Model.LSTMSingleOutput: return ((PMLSTMProperties)pmHandler.GetModelProperties()).getCommentString(); break;
        }

        return "No Parameter";
    }


    void setProperties()
    {
        object property = null;

        switch (pmHandler.model)
        {
            case PMHandler.Model.PID: property = (object)setPIDProperties(); break;
            case PMHandler.Model.AdvPID: property = (object)setAdvPIDProperties(); break;
            case PMHandler.Model.APG: property = (object)setAPGProperties(); break;
        }

        if(parameter == ParameterSelection.L)
        {
            pmHandler.forwardLatency = setValue(this.forwardLatency);
            pmHandler.backwardLatency = setValue(this.backwardLatency);
        }

        pmHandler.SetModelProperties(property);
    }

    float setValue(float set)
    {
        print("Set new value to " + values[this.valueIndex]);
        if (valuesInPrecentage)
            return set * (values[this.valueIndex] / 100);
        else
            return values[this.valueIndex];
    }

    PMPIDProperties setPIDProperties()
    {
        PMPIDProperties property = (PMPIDProperties)this.pidProperties.Clone();

        switch (parameter)
        {
            case ParameterSelection.P:
                property.Position.X.P = setValue(property.Position.X.P);
                property.Position.Y.P = setValue(property.Position.Y.P);
                property.Position.Z.P = setValue(property.Position.Z.P);
                break;
            case ParameterSelection.I:
                property.Position.X.I = setValue(property.Position.X.I);
                property.Position.Y.I = setValue(property.Position.Y.I);
                property.Position.Z.I = setValue(property.Position.Z.I);
                break;
            case ParameterSelection.D:
                property.Position.X.D = setValue(property.Position.X.D);
                property.Position.Y.D = setValue(property.Position.Y.D);
                property.Position.Z.D = setValue(property.Position.Z.D);
                break;
            case ParameterSelection.V:
                property.MaxVelocityHeight = setValue(property.MaxVelocityHeight);
                property.MaxVelocityPlane = setValue(property.MaxVelocityPlane);
                break;
        }

        return property;
    }


    PMAdvPIDProperties setAdvPIDProperties()
    {
        PMAdvPIDProperties property = (PMAdvPIDProperties)this.advPidProperties.Clone();

        switch (parameter)
        {
            case ParameterSelection.P:
                property.Position.X.P = setValue(property.Position.X.P);
                property.Position.Y.P = setValue(property.Position.Y.P);
                property.Position.Z.P = setValue(property.Position.Z.P);
                break;
            case ParameterSelection.I:
                property.Position.X.I = setValue(property.Position.X.I);
                property.Position.Y.I = setValue(property.Position.Y.I);
                property.Position.Z.I = setValue(property.Position.Z.I);
                break;
            case ParameterSelection.D:
                property.Position.X.D = setValue(property.Position.X.D);
                property.Position.Y.D = setValue(property.Position.Y.D);
                property.Position.Z.D = setValue(property.Position.Z.D);
                break;
            case ParameterSelection.V:
                property.MaxVelocityHeight = setValue(property.MaxVelocityHeight);
                property.MaxVelocityPlane = setValue(property.MaxVelocityPlane);
                break;
            case ParameterSelection.A:
                property.Acc = new Vector3(setValue(property.Acc.x), setValue(property.Acc.y), setValue(property.Acc.z));
                break;
            case ParameterSelection.O:
                property.Limit_offset = new Vector3(setValue(property.Limit_offset.x), setValue(property.Limit_offset.y), setValue(property.Limit_offset.z));
                break;
        }

        return property;
    }

    PMAPGProperties setAPGProperties()
    {
        PMAPGProperties property = (PMAPGProperties)this.apgProperties.Clone();

        switch (parameter)
        {
            case ParameterSelection.M:
                property.M = new Vector3(setValue(property.M.x), setValue(property.M.y), setValue(property.M.z));
                break;
            case ParameterSelection.K:
                property.K = new Vector3(setValue(property.K.x), setValue(property.K.y), setValue(property.K.z));
                break;
            case ParameterSelection.APG_D:
                property.D = new Vector3(setValue(property.D.x), setValue(property.D.y), setValue(property.D.z));
                break;
            case ParameterSelection.O:
                property.Limit_offset = new Vector3(setValue(property.Limit_offset.x), setValue(property.Limit_offset.y), setValue(property.Limit_offset.z));
                break;
        }

        return property;
    }



}


