using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

internal class PMLatencyEstimatorActivePing : PMLatencyEstimator
{
    private string targetAddress;
    private PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation;

    private long newEstimatedLatency;
    private bool newEstimation;
    private bool measurementRunning;
    private List<float> estimatedLatencies = new List<float>();
    //private List<float> setLatencies = new List<float>();

    private Ping pingSender;
    private PingOptions options;
    private int timeout;
    // Create a buffer of 32 bytes of data to be transmitted.
    private byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    private PMHandler pmHandler;

    private uint counter = 0; // for log mode

    public PMLatencyEstimatorActivePing(PMHandler pmHandler, string targetAddress, PMHandler.LatencyEstimatorStatisitcalCalculation statisitcalCalculation)
    {
        this.pmHandler = pmHandler;
        this.targetAddress = targetAddress;
        this.statisitcalCalculation = statisitcalCalculation;

        this.newEstimation = false;
        this.measurementRunning = false;

        pingSender = new Ping();

        // Add event to receiving completed task
        pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

        // Wait 12 seconds for a reply.
        timeout = 5000;

        // Set options for transmission:
        // The data can go through 64 gateways or routers
        // before it is destroyed, and the data packet
        // cannot be fragmented.
        options = new PingOptions(64, true);
    }

    internal override void EstimateLatency()
    {


        if (!this.measurementRunning)
        {
            
            if (pmHandler.currentUavState.IsConnected) // Online mode
                startPingRoutine();
            else // Log mode
            {
                uint number;
                float estimatedLatency;
                (estimatedLatency, number) = pmHandler.currentUavState.TMTCLatency;

                if (number > counter)
                {
                    counter = number;
                    newEstimatedLatency = (long)(estimatedLatency*1000);
                    this.newEstimation = true;
                }
            }
        }

        if(this.newEstimation)
        {
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
                    (mean, stdev) = PMLatencyEstimator.Average(estimatedLatencies.GetRange(estimatedLatencies.Count-10,10));
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

            this.newEstimation = false;
        }
        
    }

    private void startPingRoutine()
    {
        pingSender.SendAsync(this.targetAddress, timeout, buffer, options);

        this.measurementRunning = true;
    }

    private void PingCompletedCallback(object sender, PingCompletedEventArgs e)
    {
        this.measurementRunning = false;
        
        // If the operation was canceled, display a message to the user.
        if (e.Cancelled)
        {
            Console.WriteLine("Ping canceled.");
            return;            
        }

        // If an error occurred, display the exception to the user.
        if (e.Error != null)
        {
            Console.WriteLine("Ping failed:");
            Console.WriteLine(e.Error.ToString());
            return;
        }

        PingReply reply = e.Reply;

        this.newEstimatedLatency = reply.RoundtripTime;
        this.newEstimation = true;
    }
}