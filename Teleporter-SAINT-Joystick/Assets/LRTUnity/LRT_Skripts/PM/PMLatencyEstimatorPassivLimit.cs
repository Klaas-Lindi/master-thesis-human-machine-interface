using System;
using System.Collections.Generic;
using UnityEngine;


internal class PMLatencyEstimatorPassivLimit : PMLatencyEstimator
{
    // Important input data
    private PMHandler pmHandler;
    private UavState currentUavState;
    private OperatorState operatorState;
    private float toleranceLimit;
    private PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation;


    // Storage variable
    private Vector3 lastOperatorStatePosition;
    private double tStart;
    private Vector3 vehiclePoseAtStart;
    private List<float> estimatedLatencies = new List<float>();
    //private List<float> setLatencies = new List<float>();

    // Flag variables
    private bool newEstimation = false;
    
    // Hidden parameters
    

    public PMLatencyEstimatorPassivLimit(PMHandler pmHandler, UavState currentUavState, OperatorState operatorState, float forwardLatency, float backwardLatency, float toleranceLimit, PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation)
    {
        this.pmHandler = pmHandler;
        this.currentUavState = currentUavState;
        this.operatorState = operatorState;
        this.ForwardLatency = forwardLatency;
        this.BackwardLatency = backwardLatency;
        this.toleranceLimit = toleranceLimit;
        this.statisitcalCalculation = statisitcalCalculation;
    }

    internal override void EstimateLatency()
    {
        if(this.lastOperatorStatePosition == Vector3.zero)
            this.lastOperatorStatePosition = this.operatorState.OperatorPose.position;

        if (this.lastOperatorStatePosition != this.operatorState.OperatorPose.position)
        {
            this.tStart = pmHandler.GetCurrentTime();
            this.vehiclePoseAtStart = this.currentUavState.CameraPose.position;

            newEstimation = true;

            // Get last operator pose
            this.lastOperatorStatePosition = this.operatorState.OperatorPose.position;
        }

        if(newEstimation && !isInTolerance())
        {
            float mean = 0.0f;
            float stdev = 0.0f;
            float median = 0.0f;

            double delta = pmHandler.GetCurrentTime() - this.tStart;

            estimatedLatencies.Add((float)delta);

            float setLatency = 0.0f;
            switch (statisitcalCalculation)
            {
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Last:
                    setLatency = (float)(delta / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + delta.ToString("0.00")); break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Mean:
                    (mean, stdev) = PMLatencyEstimator.Average(estimatedLatencies);
                    setLatency = (float)(mean / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + delta.ToString("0.00") +
                    " Mean Latency: " + mean.ToString("0.00") +
                    " Stddev Latency: " + stdev.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Mean_Last_10:
                    if (estimatedLatencies.Count < 10) break;
                    (mean, stdev) = PMLatencyEstimator.Average(estimatedLatencies.GetRange(estimatedLatencies.Count - 10, 10));
                    setLatency = (float)(mean / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + delta.ToString("0.00") +
                    " Mean Latency: " + mean.ToString("0.00") +
                    " Stddev Latency: " + stdev.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Median:
                    median = PMLatencyEstimator.Median(estimatedLatencies);
                    setLatency = (float)(median / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + delta.ToString("0.00") +
                    " Mean Latency: " + median.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Median_Last_10:
                    if (estimatedLatencies.Count < 10) break;
                    median = PMLatencyEstimator.Median(estimatedLatencies.GetRange(estimatedLatencies.Count - 10, 10));
                    setLatency = (float)(median / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + delta.ToString("0.00") +
                    " Mean Latency: " + median.ToString("0.00"));
                    break;
            }

            this.ForwardLatency = setLatency;
            this.BackwardLatency = setLatency;

            //setLatencies.Add(setLatency);

            pmHandler.forwardLatency = this.ForwardLatency;
            pmHandler.backwardLatency = this.BackwardLatency;

            newEstimation = false;
        }
    }

    private bool isInTolerance()
    {
        Vector3 factor = new Vector3(this.currentUavState.CameraPose.position.x / this.vehiclePoseAtStart.x,
                                     this.currentUavState.CameraPose.position.y / this.vehiclePoseAtStart.y,
                                     this.currentUavState.CameraPose.position.z / this.vehiclePoseAtStart.z);

        //MonoBehaviour.print(factor);
        if (Mathf.Abs(factor.x - 1) > this.toleranceLimit)
            return false;
        if (Mathf.Abs(factor.y - 1) > this.toleranceLimit)
            return false;
        if (Mathf.Abs(factor.z - 1) > this.toleranceLimit)
            return false;

        return true;
    }
}