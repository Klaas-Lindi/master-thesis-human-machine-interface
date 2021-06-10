using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;


internal class PMLatencyEstimatorActiveTMTC : PMLatencyEstimator
{
    private PMHandler pmHandler;
    private UavState uavState;
    private PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation;

    private float newEstimatedLatency;
    private uint counter;
    private List<float> estimatedLatencies = new List<float>();
    //private List<float> setLatencies = new List<float>();

    public PMLatencyEstimatorActiveTMTC(PMHandler pmHandler, UavState uavState, PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation)
    {
        this.pmHandler = pmHandler;
        this.uavState = uavState;
        this.statisitcalCalculation = statisitcalCalculation;
    }

    internal override void EstimateLatency()
    {
        uint number;
        (newEstimatedLatency, number) = uavState.TMTCLatency;

        if (number > counter)
        {
            counter = number;
            float mean = 0.0f;
            float stdev = 0.0f;
            float median = 0.0f;

            estimatedLatencies.Add((float)newEstimatedLatency);

            float setLatency = 0.0f;
            switch (statisitcalCalculation)
            {
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Last:
                    setLatency = ((float)newEstimatedLatency / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + newEstimatedLatency.ToString("0.00")); break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Mean:
                    (mean, stdev) = PMLatencyEstimator.Average(estimatedLatencies);
                    setLatency = (float)(mean / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + newEstimatedLatency.ToString("0.00") +
                    " Mean Latency: " + mean.ToString("0.00") +
                    " Stddev Latency: " + stdev.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Mean_Last_10:
                    if (estimatedLatencies.Count < 10) break;
                    (mean, stdev) = PMLatencyEstimator.Average(estimatedLatencies.GetRange(estimatedLatencies.Count - 10, 10));
                    setLatency = (float)(mean / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + newEstimatedLatency.ToString("0.00") +
                    " Mean Latency: " + mean.ToString("0.00") +
                    " Stddev Latency: " + stdev.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Median:
                    median = PMLatencyEstimator.Median(estimatedLatencies);
                    setLatency = (float)(median / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + newEstimatedLatency.ToString("0.00") +
                    " Mean Latency: " + median.ToString("0.00"));
                    break;
                case PMHandler.LatencyEstimatorStatisitcalCalculation.Median_Last_10:
                    if (estimatedLatencies.Count < 10) break;
                    median = PMLatencyEstimator.Median(estimatedLatencies.GetRange(estimatedLatencies.Count - 10, 10));
                    setLatency = (float)(median / 2000);
                    UnityEngine.MonoBehaviour.print("Estimated Latency: " + newEstimatedLatency.ToString("0.00") +
                    " Mean Latency: " + median.ToString("0.00"));
                    break;
                    // case PMHandler.LatencyEstimatorStatisitcalCalculation.Median: setLatency = (float)(mean / 2000); break;
            }
            //setLatencies.Add(setLatency);

            this.ForwardLatency = setLatency;
            this.BackwardLatency = setLatency;

            pmHandler.forwardLatency = this.ForwardLatency;
            pmHandler.backwardLatency = this.BackwardLatency;
        }

    }
}