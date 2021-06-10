using System;
using System.Collections.Generic;
using UnityEngine;

internal class PMLatencyEstimator
{
    private float forwardLatency;
    private float backwardLatency;

    public PMLatencyEstimator()
    {
        this.ForwardLatency = 0.0f;
        this.BackwardLatency = 0.0f;
    }

    public PMLatencyEstimator(float forwardLatency, float backwardLatency)
    {
        this.ForwardLatency = forwardLatency;
        this.BackwardLatency = backwardLatency;
    }

    public float ForwardLatency { get => forwardLatency; set => forwardLatency = value; }
    public float BackwardLatency { get => backwardLatency; set => backwardLatency = value; }

    virtual internal void EstimateLatency()
    {
        // Do nothing here 
    }

    // Common functions
    public static (float mean, float stdev) Average(List<float> estimatedLatencies)
    {
        if (estimatedLatencies == null || estimatedLatencies.Count == 0)
            throw new System.Exception("Average of empty array not defined.");

        float sum = 0;
        float count = estimatedLatencies.Count;

        foreach (float value in estimatedLatencies)
        {
            sum += value;
        }

        float mean = (sum / count);
        float sumOfSqrs = 0;
        foreach (float value in estimatedLatencies)
        {
            sumOfSqrs += (float)Math.Pow((value - mean), 2);
        }
        float stdev = (float)Math.Sqrt(sumOfSqrs / (count - 1));

        return (mean, stdev);
    }

    public static float Median(List<float> estimatedLatencies, int lastCount = -1)
    {
  
        if (estimatedLatencies == null || estimatedLatencies.Count == 0)
            throw new System.Exception("Median of empty array not defined.");

        //make sure the list is sorted, but use a new array
        estimatedLatencies.Sort();
        //make sure the list is sorted, but use a new array

        //get the median
        int size = estimatedLatencies.Count;
        int mid = size / 2;
        float median = (size % 2 != 0) ? estimatedLatencies[mid] : (estimatedLatencies[mid] + estimatedLatencies[mid - 1]) / 2;
        return median;
    }
}
