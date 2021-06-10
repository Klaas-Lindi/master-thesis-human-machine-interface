using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This module handles the differen Predictive Models and is Framework for executing them
/// </summary>
public class PMHandler : MonoBehaviour {

    private UavState predictiveUavState;

    [Header("General Settings and Relations")]
    [Tooltip("Enables or disables Predictive Modeling, if disabled no animation occurs.")]
    /// <summary>
    /// Enables or disables Predictive Modeling, if disabled no animation occurs.
    /// </summary>
    public bool PMEnabled = false;

    [Tooltip("The current state of the current teleoperator.")]
    /// <summary>
    /// The current state of the current teleoperator
    /// </summary>
    public UavState currentUavState;

    [Tooltip("The current operator state of the head position and controller inputs.")]
    /// <summary>
    /// The current operator state of the head position and controller inputs
    /// </summary>
    public OperatorState operatorState;

    [Tooltip("Object to set path")]
    /// <summary>
    /// Path Viewer
    /// </summary>
    public ParticlePathViewer pathViewer;

    //Get all wrappers for predictive modeling
    private PMModel pmModel;

    //Get all wrappers for vechicles
    private PMVehicle pmVehicle;

    //Get wrapper for Latency Estimator
    private PMLatencyEstimator pmLatencyEstimator;


    [Tooltip("The time range to calulate predicted path in seconds")]
    /// <summary>
    /// The time range to calulate predicted path in seconds
    /// </summary>
    public float calculationTimeLimitOfPath;
    [Tooltip("The time step size to calulate the predicted path in seconds")]
    /// <summary>
    /// The time range to calulate predicted path in seconds
    /// </summary>
    public float predictionTimeStep = 0.05f;

    public enum LatencyEstimator { Constant, PassivLimit, ActivePing, ActiveTMTC };
    [Header("Latency Settings")]
    [Tooltip("This list contains all possible latency estimator which can be used to predicte the latency of the system")]
    public LatencyEstimator latencyEstimator;
    
    [Tooltip("The forward latency of communciation in seconds")]
    public float forwardLatency;

    [Tooltip("The backward latency of communciation in seconds")]
    public float backwardLatency;

    public enum LatencyEstimatorStatisitcalCalculation { Last, Mean, Mean_Last_10, Median, Median_Last_10 };
    [Tooltip("List how the latency will be calculated")]
    public LatencyEstimatorStatisitcalCalculation statisitcalCalculation;

    [Tooltip("Parameter to set the tolerance limit when a new latency will be estimated. For PassivLimit it is the tolerance level when the movement of the operator will be followed.")]
    public float toleranceLimit;

    [Tooltip("Address of the target vehicle to measure active latency.")]
    public string targetAddress;

    public enum Vehicle { None, Copter }; //Rover, VTOL, Plane, Satellite 
    [Header("Automatism Settings")]
    [Tooltip("This list contains all possible vehicles which can be used to predicte the automatism")]
    public Vehicle vehicle;

    [Tooltip("The radius to accept a waypoint from UAV")]
    public float waypointAcceptanceRange;

    //[Tooltip("The radius over POI for autoscan circle")]
    //public float autoCircleRadius;

    public enum Model { None, PID, AdvPID, APG, LSTMSingleOutput, LSTMSingleOutputLC, LSTMMultiOutputLC };
    [Header("Model Settings")]
    [Tooltip("This list contains all possible models which can be used to predicte the position of the vehicle")]
    public Model model;

    //Path Handling Variables
    private List<Vector3> predictedPath;
    private PlausibilityCheck plausibilityCheck;

    //Automatism Variables
    private PredictedCollision predictedCollision;

    // Use this for initialization
    void Start () {

        // Get Components and initlize variables
        predictiveUavState = this.GetComponent<UavState>();
        pmVehicle = new PMVehicle();
        pmModel = new PMModel();
        predictedPath = new List<Vector3>();
        plausibilityCheck = this.GetComponent<PlausibilityCheck>();

        // Set Latency Model
        switch (latencyEstimator)
        {
            case LatencyEstimator.Constant: pmLatencyEstimator = new PMLatencyEstimator(this.forwardLatency,this.backwardLatency); break;
            case LatencyEstimator.PassivLimit: pmLatencyEstimator = new PMLatencyEstimatorPassivLimit(this,currentUavState, operatorState,this.forwardLatency, this.backwardLatency , this.toleranceLimit, statisitcalCalculation); break;
            case LatencyEstimator.ActivePing: pmLatencyEstimator = new PMLatencyEstimatorActivePing(this, targetAddress, statisitcalCalculation); break;
            case LatencyEstimator.ActiveTMTC: pmLatencyEstimator = new PMLatencyEstimatorActiveTMTC(this, currentUavState, statisitcalCalculation); break;
        }


        // Set model
        switch (model)
        {
            case Model.None: pmModel = new PMModel(); break;
            case Model.PID: pmModel = new PMModelPID(); break;
            case Model.AdvPID: pmModel = new PMModelAdvPID(); break;
            case Model.APG: pmModel = new PMModelAPG(); break;
            case Model.LSTMSingleOutput: pmModel = new PMModelLSTMSinOut(); break;
            case Model.LSTMSingleOutputLC: pmModel = new PMModelLSTMSinOutLC(); break;
            case Model.LSTMMultiOutputLC: pmModel = new PMModelLSTMMultiOutLC(); break;
        }
        // Set settings
        pmModel.SetPredictionTimeStep(predictionTimeStep);

        // Set vehicle
        switch (vehicle)
        {
            case Vehicle.None:
                pmVehicle = new PMVehicle(currentUavState,operatorState,pmModel);
                pmVehicle.SetVehicleProperties(new PMVehicleSettings(this.waypointAcceptanceRange)); //TBD not dynamic
                break;
            case Vehicle.Copter:
                pmVehicle = new PMVehicleCopter(currentUavState, operatorState, pmModel);
                pmVehicle.SetVehicleProperties(new PMVehicleCopterSettings(this.waypointAcceptanceRange)); //TBD not dynamic
                break;
        }

        // Disable rendering if not activated 
        if (!PMEnabled)
        {
            foreach (MeshRenderer tmp in this.GetComponentsInChildren<MeshRenderer>())
            {
                tmp.enabled = false;
            }            
        }
    }
	
	// Update is called once per frame
	void Update () {
        
        // Predict only if enabled
        if (PMEnabled)
        {
            /* print(this.gameObject.name + ": " 
                 + currentUavState.ControlState.ToString() + ", " 
                 + currentUavState.Condition.ToString() + ", " 
                 + currentUavState.OperationState.ToString() + ", " 
                 + pmModelWrapper.Waypoints.Count + ", " 
                 + operatorState.ActiveAutomatism.ToString());*/
            /*string result = "";
            foreach (Waypoint point in pmModelWrapper.Waypoints)
            {
                result += point.Position + "; ";
            }*/
            EstimateLatency();
            
            // Handle Automatism
            pmVehicle.HandleAutomatism();

            // Calculate Prediction of vehicle reaction
            CalculatePrediction();

            // Predicted Path
            RefreshPredictivedPath();
            
            // Check for collision and plausibility
            CheckForPlausibility();

            // Set path to render
            pathViewer.Waypoints = predictedPath;
         }
    }



    private void EstimateLatency()
    {
        // Estimate latency
        pmLatencyEstimator.EstimateLatency();

        // estimate or calculate latency towards the teleoperator
        pmModel.SetForwardLatency(pmLatencyEstimator.ForwardLatency);

        // estimate or calculate latency backwards from the operator
        pmModel.SetBackwardsLatency(pmLatencyEstimator.BackwardLatency);
    }

    /// <summary>
    /// This method sets the important parameters for calculating the predicted path
    /// </summary>
    private void CalculatePrediction()
    {
        // Get current uav state (pose, sensors)
        pmModel.SetCurrentUavPose(currentUavState.CameraPose);
        pmModel.SetCurrentUavState(currentUavState.OperationState);

        // Handle Waypoints
        pmModel.UpdateWaypoints();

        // get current prediction with delay
        predictiveUavState.SetCameraPose(pmModel.GetPredictivePose());
    }

    /// <summary>
    /// Get the predicted path to a certain time
    /// </summary>
    private void RefreshPredictivedPath()
    {
        // Clear path
        predictedPath.Clear();
        double default_timestep = this.predictionTimeStep; //seconds

        // Add current postion
        predictedPath.Add(currentUavState.CameraPose.position);

        if (pmModel is PMModelPID || pmModel is PMModelAdvPID || pmModel is PMModelAPG || pmModel is PMModelLSTMSinOut || pmModel is PMModelLSTMSinOutLC || pmModel is PMModelLSTMMultiOutLC)
        {
            predictedPath.AddRange(pmModel.GetPathOptimized(default_timestep * 1000, this.calculationTimeLimitOfPath , default_timestep));
        }
        else
        {
            // Calculate path
            for (double t = default_timestep * 1000; t < this.calculationTimeLimitOfPath * 1000; t += default_timestep * 1000)
            {
                if (Vector3.Distance(predictedPath[predictedPath.Count - 1], operatorState.OperatorPose.position) < 0.1) // Correct stop condition?
                {
                    predictedPath.Add(predictedPath[predictedPath.Count - 1]);
                }
                else
                {
                    predictedPath.Add(pmModel.GetPredictivePoseAt(t).position);
                }
            }
        }
    }


    /// <summary>
    /// This metod checks the incomming commands to plausibility
    /// </summary>
    private void CheckForPlausibility()
    {
        //print("Plausibility Chech not fully implemented! GEOFENCE is missing");

        // Predict Collisions
        predictedCollision = plausibilityCheck.Check(predictedPath);

        //Handle if collision was detected
        if (predictedCollision.Collision != PredictedCollision.CollisionType.None)
        {
            int i = predictedPath.IndexOf(predictedCollision.Position);
            predictedPath.RemoveRange(i, predictedPath.Count - i);
            foreach (Vector3 point in predictedPath)
            {
                if (Vector3.Distance(point, predictiveUavState.CameraPose.position) < 0.5)
                    return;
            }
            predictiveUavState.SetCameraPosition(predictedCollision.Position);

        }

    }


    /// <summary>
    /// Set the properties of the curent vehile
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    public void SetVehicleProperties(object obj)
    {
        if (pmVehicle != null)
        {
            pmVehicle.SetVehicleProperties(obj);
        }
    }

    /// <summary>
    /// Get the properties of the curent vehile
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    public object GetVehicleProperties()
    {
        if (pmVehicle != null)
        {
            return pmVehicle.GetVehicleProperties();
        }
        return null;
    }

    // External
    public void SetModelProperties(object obj)
    {
        if (pmModel != null)
        {
            pmModel.SetModelProperties(obj);
        }
    }

    /// <summary>
    /// Get the properties of the curent model
    /// </summary>
    /// <returns>Returns object of property class. null if there is no property</returns>
    public object GetModelProperties()
    {
        if (pmModel != null)
        {
            return pmModel.GetModelProperties();
        }
        return null;
    }

    // External Settings 
    public void OverwriteCurrentTime(double time)
    {
        pmModel.OverwriteCurrentTime(time);
    }

    public double GetCurrentTime(bool enableRealTimeCompensation = true)
    {
        return pmModel.GetCurrentTime(enableRealTimeCompensation);
    }

    /// <summary>
    /// Function to reset current data and settings to his beginning values. Does not change model or vehicle
    /// </summary>
    public void Reset()
    {
        if(this.predictedPath != null)
            this.predictedPath.Clear();
        if (this.pmModel != null)
            this.pmModel.Reset();
        if (this.pmVehicle != null)
            this.pmVehicle.Reset();
        // Set PM Properties TBD
    }
}
