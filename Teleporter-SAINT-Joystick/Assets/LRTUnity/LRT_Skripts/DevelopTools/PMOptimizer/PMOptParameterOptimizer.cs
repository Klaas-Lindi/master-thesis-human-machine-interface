
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if MODULE_OPTIMZER_ENABLED
using Accord.Genetic;

// Script to automatically determine good paramters for the PMA using a genetic algorithm
public class PMOptParameterOptimizer : MonoBehaviour, Accord.Genetic.IFitnessFunction
{
#region Declarations
    [Tooltip("If this is checked, the script will terminate all other scripts and run the optimization instead.")]
    public bool enableOptimizerMode;
    public bool writeLog;
    public string logName;

    public int iterations = 10;
    public int populationSize = 10;
    public float weightError = 1;
    public float weightDerivativeError = 1;

    [Range(0.1f, 1.0f)]
    public double crossoverProb = 0.75;
    [Range(0.1f, 1.0f)]
    public double mutationProb = 0.1;
    [Range(0.0f, 0.9f)]
    public double randomSelectionPart = 0;

    public enum SelMethod { Rank_Selection, Roulette_Selection, Elite_Selection };
    public SelMethod selectionMethod;
    
    public float PUpperBound = 1f;
    public float PLowerBound = 0f;
    public float IUpperBound = 1f; 
    public float ILowerBound = 0f;
    public float DUpperBound = 1f;
    public float DLowerBound = 0f;
    [Tooltip("Upper bound for the maximum velocity in vertical direction.")]
    public float MaxVelHeightUB = 10f; // [m/s]
    [Tooltip("Lower bound for the maximum velocity in vertical direction.")]
    public float MaxVelHeightLB = 0f;
    [Tooltip("Upper bound for the maximum velocity in horizontal direction.")]
    public float MaxVelPlaneUB = 10f; // [m/s]
    [Tooltip("Lower bound for the maximum velocity in horizontal direction.")]
    public float MaxVelPlaneLB = 0f;

    public enum PredictiveModel { SIMPLE_PID, NONE }; // Currently does nothing, only the PID Model is used
    public PredictiveModel model;

    public string pathToLog;

    [Tooltip("Skip the first n entries from loaded log. At run time it indicates the line count of file")]
    public int skipEntries = 0;
    [Tooltip("Load this counts of entries from log. -1 indicates unlimmited")]
    public int lineCounts = -1;

    [Tooltip("Indices of the Static Regions to be used in X-Direction (zero based).")]
    public int[] XRegionIndices;
    [Tooltip("Indices of the Static Regions to be used in Y-Direction (zero based).")]
    public int[] YRegionIndices;
    [Tooltip("Indices of the Static Regions to be used in Z-Direction (zero based).")]
    public int[] ZRegionIndices;

    private float debugZOffset = 0.15f;

    private StreamReader streamReader;
    private StreamWriter streamWriter;

    // LoadLog() Lists
    private List<double> timestamps_log = new List<double>();
    private List<Pose> opPoseHistory_log = new List<Pose>();
    private List<Pose> uavPoseHistory_log = new List<Pose>();

    private List<Pose> operatorDerivs;
    private List<StaticRegion> staticRegions;
    private List<StaticRegion> staticRegionsX;
    private List<StaticRegion> staticRegionsY;
    private List<StaticRegion> staticRegionsZ;
    private List<List<Vector3>> pathListX;
    private List<List<Vector3>> pathListY;
    private List<List<Vector3>> pathListZ;
    private List<List<double>> fitnessAvgX;
    private List<List<double>> fitnessAvgY;
    private List<List<double>> fitnessAvgZ;
    private List<List<double>> fitnessMaxX;
    private List<List<double>> fitnessMaxY;
    private List<List<double>> fitnessMaxZ;
    private PMModel pmModelWrapper;
    private double timestepModel_sec = 0.1f;

    // Optimization Region specific
    private int startIndex;
    private int endIndex;
    private Vector3 region_movement;
    private double duration_region_sec;
    private List<Vector3> realUavPath_region;

    // Results
    private List<List<Solution>> solutionsX;
    private List<List<Solution>> solutionsY;
    private List<List<Solution>> solutionsZ;
    private int regionIndex;

    private int axis; // X, Y, Z = 0, 1, 2 
#endregion

    void Awake()
    {
        if (enableOptimizerMode)
        {
            TimeSpan runtimeStart = DateTime.Now.TimeOfDay;
            String runtimeStartString = DateTime.Now.ToString("yyyyMMddHHmmss");

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
            UnityEditor.EditorApplication.isPlaying = false;
#endregion
            
            // Set model
            switch (model)
            {
                case PredictiveModel.NONE: pmModelWrapper = new PMModel(); break;
                case PredictiveModel.SIMPLE_PID: pmModelWrapper = new PMModelPID(); break;
            }
            // Initialize Model
            pmModelWrapper.SetForwardLatency(2.0f);
            pmModelWrapper.SetBackwardsLatency(2.0f);

            // Load the logfile into Lists and into the pmWrapper
            LoadLog();

            // Calculate the derivative of operator movement
            operatorDerivs = CalculateDerivativesSR(opPoseHistory_log);

            // Identify and store the static regions
            staticRegions = CalculateStaticRegions(operatorDerivs);

            // Process static regions
            for (int n = 0; n < staticRegions.Count; n++)
            {
                for (int i = staticRegions[n].start; i < staticRegions[n].end; i++)
                {
                    staticRegions[n].uavPath.Add(uavPoseHistory_log[i].position);
                }
                staticRegions[n].CalculateMovement();
            }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t SRs FOUND:\n");
            for (int i = 0; i < staticRegions.Count; i++)
            { Debug.LogFormat("SR#{0}: \t{1}\n", i, staticRegions[i]); }

            // Move selected SRs into Lists
            staticRegionsX = new List<StaticRegion>();
            staticRegionsY = new List<StaticRegion>();
            staticRegionsZ = new List<StaticRegion>();

            for (int i = 0; i < XRegionIndices.Length; i++)
            { staticRegionsX.Add(staticRegions[XRegionIndices[i]]); }

            for (int i = 0; i < YRegionIndices.Length; i++)
            { staticRegionsY.Add(staticRegions[YRegionIndices[i]]); }

            for (int i = 0; i < ZRegionIndices.Length; i++)
            { staticRegionsZ.Add(staticRegions[ZRegionIndices[i]]); }

            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t SRs SELECTED:\n");
            for (int i = 0; i < staticRegionsX.Count; i++)
            { Debug.LogFormat("SR_X#{0}: \t{1}\n", i, staticRegionsX[i]); }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n");
            for (int i = 0; i < staticRegionsY.Count; i++)
            { Debug.LogFormat("SR_Y#{0}: \t{1}\n", i, staticRegionsY[i]); }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n");
            for (int i = 0; i < staticRegionsZ.Count; i++)
            { Debug.LogFormat("SR_Z#{0}: \t{1}\n", i, staticRegionsZ[i]); }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n");

            // Set up Evalutation function in this class
            Accord.Genetic.IFitnessFunction fitnessFunction = this.GetComponent<PMOptParameterOptimizer>();

            // Create a model Chromosome
            BinaryChromosome ancestor = new BinaryChromosome(64);

            // Create Lists to store solutions
            solutionsX = new List<List<Solution>>();
            solutionsY = new List<List<Solution>>();
            solutionsZ = new List<List<Solution>>();
            pathListX = new List<List<Vector3>>(); // List for the UAV paths of the regions
            pathListY = new List<List<Vector3>>();
            pathListZ = new List<List<Vector3>>();
            fitnessAvgX = new List<List<double>>();
            fitnessAvgY = new List<List<double>>();
            fitnessAvgZ = new List<List<double>>();
            fitnessMaxX = new List<List<double>>();
            fitnessMaxY = new List<List<double>>();
            fitnessMaxZ = new List<List<double>>();

            Debug.LogFormat("Runtime before Start of Optimizer: " + (DateTime.Now.TimeOfDay - runtimeStart) + "\n");
            Debug.LogFormat("Starting Optimization...\n");

            for (int n = 0; n < 3; n++) // 3 axis
            {
                axis = n;
                switch (axis)
                {
                    case 0: // X
                        for (int regInd = 0; regInd < staticRegionsX.Count; regInd++) // Perform Optimization for every selected SR
                        {
                            startIndex = staticRegionsX[regInd].start;
                            endIndex = staticRegionsX[regInd].end;

                            // Cut the UAV Path out of the list
                            realUavPath_region = staticRegionsX[regInd].uavPath;
                            pathListX.Add(realUavPath_region);

                            // Determine the total movement to normalize the error with
                            region_movement = staticRegionsX[regInd].movement;

                            // Determine duration of the region in seconds
                            duration_region_sec = (timestamps_log[endIndex] - timestamps_log[startIndex]) / 1000;

                            // Hand the initial timestamp and UAV Pose over to the pmWrapper
                            pmModelWrapper.OverwriteCurrentTime(timestamps_log[startIndex]);
                            pmModelWrapper.SetCurrentUavPose(uavPoseHistory_log[startIndex]);

                            // Create a sublist for the current static region
                            solutionsX.Add(new List<Solution>());
                            regionIndex = regInd;
                            fitnessAvgX.Add(new List<double>());
                            fitnessMaxX.Add(new List<double>());

                            // GA Loop
                            Population pop = new Population(populationSize, ancestor, fitnessFunction, new RankSelection());
                            switch (selectionMethod)
                            {
                                case SelMethod.Roulette_Selection: pop.SelectionMethod = new RouletteWheelSelection(); break;
                                case SelMethod.Elite_Selection: pop.SelectionMethod = new EliteSelection(); break;
                            }
                            pop.MutationRate = mutationProb;
                            pop.CrossoverRate = crossoverProb;
                            pop.RandomSelectionPortion = randomSelectionPart;
                            for (int iter = 0; iter < iterations; iter++)
                            {
                                pop.RunEpoch();
                                fitnessAvgX[regionIndex].Add(pop.FitnessAvg);
                                fitnessMaxX[regionIndex].Add(pop.FitnessMax);
                            }
                        }
                        break;
                    case 1: // Y
                        for (int regInd = 0; regInd < staticRegionsY.Count; regInd++)
                        {
                            startIndex = staticRegionsY[regInd].start;
                            endIndex = staticRegionsY[regInd].end;

                            // Cut the UAV Path out of the list
                            realUavPath_region = staticRegionsY[regInd].uavPath;
                            pathListY.Add(realUavPath_region);

                            // Determine the total movement to normalize the error with
                            region_movement = staticRegionsY[regInd].movement;

                            // Determine duration of the region in seconds
                            duration_region_sec = (timestamps_log[endIndex] - timestamps_log[startIndex]) / 1000;

                            // Hand the initial timestamp and UAV Pose over to the pmWrapper
                            pmModelWrapper.OverwriteCurrentTime(timestamps_log[startIndex]);
                            pmModelWrapper.SetCurrentUavPose(uavPoseHistory_log[startIndex]);

                            // Create a sublist for the current static region
                            solutionsY.Add(new List<Solution>());
                            regionIndex = regInd;
                            fitnessAvgY.Add(new List<double>());
                            fitnessMaxY.Add(new List<double>());

                            // GA Loop
                            Population pop = new Population(populationSize, ancestor, fitnessFunction, new RankSelection());
                            switch (selectionMethod)
                            {
                                case SelMethod.Roulette_Selection: pop.SelectionMethod = new RouletteWheelSelection(); break;
                                case SelMethod.Elite_Selection: pop.SelectionMethod = new EliteSelection(); break;
                            }
                            pop.MutationRate = mutationProb;
                            pop.CrossoverRate = crossoverProb;
                            pop.RandomSelectionPortion = randomSelectionPart;
                            for (int iter = 0; iter < iterations; iter++)
                            {
                                pop.RunEpoch();
                                fitnessAvgY[regionIndex].Add(pop.FitnessAvg);
                                fitnessMaxY[regionIndex].Add(pop.FitnessMax);
                            }
                        }
                        break;
                    case 2: // Z
                        for (int regInd = 0; regInd < staticRegionsZ.Count; regInd++)
                        {
                            startIndex = staticRegionsZ[regInd].start;
                            endIndex = staticRegionsZ[regInd].end;

                            // Cut the UAV Path out of the list
                            realUavPath_region = staticRegionsZ[regInd].uavPath;
                            pathListZ.Add(realUavPath_region);

                            // Determine the total movement to normalize the error with
                            region_movement = staticRegionsZ[regInd].movement;

                            // Determine duration of the region in seconds
                            duration_region_sec = (timestamps_log[endIndex] - timestamps_log[startIndex]) / 1000;

                            // Hand the initial timestamp and UAV Pose over to the pmWrapper
                            pmModelWrapper.OverwriteCurrentTime(timestamps_log[startIndex]);
                            pmModelWrapper.SetCurrentUavPose(uavPoseHistory_log[startIndex]);

                            // Create a sublist for the current static region
                            solutionsZ.Add(new List<Solution>());
                            regionIndex = regInd;
                            fitnessAvgZ.Add(new List<double>());
                            fitnessMaxZ.Add(new List<double>());

                            // GA Loop
                            Population pop = new Population(populationSize, ancestor, fitnessFunction, new RankSelection());
                            switch (selectionMethod)
                            {
                                case SelMethod.Roulette_Selection: pop.SelectionMethod = new RouletteWheelSelection(); break;
                                case SelMethod.Elite_Selection: pop.SelectionMethod = new EliteSelection(); break;
                            }
                            pop.MutationRate = mutationProb;
                            pop.CrossoverRate = crossoverProb;
                            pop.RandomSelectionPortion = randomSelectionPart;
                            for (int iter = 0; iter < iterations; iter++)
                            {
                                pop.RunEpoch();
                                fitnessAvgZ[regionIndex].Add(pop.FitnessAvg);
                                fitnessMaxZ[regionIndex].Add(pop.FitnessMax);
                            }
                        }
                        break;
                }

                Debug.LogFormat("Runtime after GA Axis {0}: {1}\n", n, (DateTime.Now.TimeOfDay - runtimeStart));
            }

            int keep = 1; // Keep only the top N solutions for every SR (keep = 1 for Matlab Analysis)
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n" +
                            "\t BEST SOLUTIONS:");
            for (int regInd = 0; regInd < solutionsX.Count; regInd++) // For every region in solutionsX
            {
                Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t SR_X#{0}", regInd);
                solutionsX[regInd] = solutionsX[regInd].OrderByDescending(sol => GetFitnessXYZ(sol).x).ToList(); // sort by fitness
                solutionsX[regInd].RemoveRange(keep, solutionsX[regInd].Count - keep); // remove all but the first N
                for (int i = 0; i < solutionsX[regInd].Count; i++) // Print solution in the region
                {
                    Debug.LogFormat("SR {4}_X: #{0} X Solution with ErrorX: {1}\tDerivErrX: {2}\tFitnessX: {3}\n",
                                    i, solutionsX[regInd][i].Error_total.x, solutionsX[regInd][i].Error_deriv_total.x, GetFitnessXYZ(solutionsX[regInd][i]).x, regInd);
                    Debug.LogFormat(solutionsX[regInd][i].Properties.ToStringTable());
                }
            }
            for (int regInd = 0; regInd < solutionsY.Count; regInd++) // For every region in solutionsY
            {
                Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t SR_Y#{0}", regInd);
                solutionsY[regInd] = solutionsY[regInd].OrderByDescending(sol => GetFitnessXYZ(sol).y).ToList();
                solutionsY[regInd].RemoveRange(keep, solutionsY[regInd].Count - keep);
                for (int i = 0; i < solutionsY[regInd].Count; i++) // Print solution in the region
                {
                    Debug.LogFormat("SR {4}_Y: #{0} Y Solution with ErrorY: {1}\tDerivErrY: {2}\tFitnessY: {3}\n",
                                    i, solutionsY[regInd][i].Error_total.y, solutionsY[regInd][i].Error_deriv_total.y, GetFitnessXYZ(solutionsY[regInd][i]).y, regInd);
                    Debug.LogFormat(solutionsY[regInd][i].Properties.ToStringTable());
                }
            }
            for (int regInd = 0; regInd < solutionsZ.Count; regInd++) // For every region in solutionsZ
            {
                Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n\t SR_Z#{0}", regInd);
                solutionsZ[regInd] = solutionsZ[regInd].OrderByDescending(sol => GetFitnessXYZ(sol).z).ToList();
                solutionsZ[regInd].RemoveRange(keep, solutionsZ[regInd].Count - keep);
                for (int i = 0; i < solutionsZ[regInd].Count; i++) // Print solution in the region
                {
                    Debug.LogFormat("SR {4}_Z: #{0} Z Solution with ErrorZ: {1}\tDerivErrZ: {2}\tFitnessZ: {3}\n",
                                    i, solutionsZ[regInd][i].Error_total.z, solutionsZ[regInd][i].Error_deriv_total.z, GetFitnessXYZ(solutionsZ[regInd][i]).z, regInd);
                    Debug.LogFormat(solutionsZ[regInd][i].Properties.ToStringTable());
                }
            }
            Debug.LogFormat("<color=#ff0000ff>-------------------------------------------------------------</color>\n");
            Debug.LogFormat("Runtime after GA + Sort: " + (DateTime.Now.TimeOfDay - runtimeStart) + "\n");

#region WriteLog
            if (writeLog)
            {
                streamWriter = new StreamWriter("log/optimizerLogs/PMOpt_" + runtimeStartString + "_" +
                                                iterations + "It" + populationSize + "Pop" +
                                                (DateTime.Now.TimeOfDay - runtimeStart).Minutes + "min_" + logName + ".csv");

                streamWriter.Write("X Reg Sol," + staticRegionsX.Count);
                for (int regX = 0; regX < staticRegionsX.Count; regX++)
                { streamWriter.Write("," + solutionsX[regX].Count); }
                streamWriter.WriteLine();

                streamWriter.Write("Y Reg Sol," + staticRegionsY.Count);
                for (int regY = 0; regY < staticRegionsY.Count; regY++)
                { streamWriter.Write("," + solutionsY[regY].Count); }
                streamWriter.WriteLine();

                streamWriter.Write("Z Reg Sol," + staticRegionsZ.Count);
                for (int regZ = 0; regZ < staticRegionsZ.Count; regZ++)
                { streamWriter.Write("," + solutionsZ[regZ].Count); }
                streamWriter.WriteLine();

                // X
                for (int regX = 0; regX < staticRegionsX.Count; regX++) // For all regions in the axis
                {
                    streamWriter.Write(staticRegionsX[regX].start + "," + staticRegionsX[regX].end + ",0,," + // 0 -> X
                                       staticRegionsX[regX].movement.x.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    for (int i = 0; i < staticRegionsX[regX].uavPath.Count; i++) // region real path
                    { streamWriter.Write("," + staticRegionsX[regX].uavPath[i].x.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                    streamWriter.WriteLine();

                    streamWriter.Write(",,,,"); // Log avg. fitness values
                    for (int fitInd = 0; fitInd < fitnessAvgX[regX].Count; fitInd++)
                    { streamWriter.Write("," + fitnessAvgX[regX][fitInd]); }
                    streamWriter.WriteLine();
                    streamWriter.Write(",,,,"); // Log max. fitness values
                    for (int fitInd = 0; fitInd < fitnessMaxX[regX].Count; fitInd++)
                    { streamWriter.Write("," + fitnessMaxX[regX][fitInd]); }
                    streamWriter.WriteLine();

                    for (int sol = 0; sol < solutionsX[regX].Count; sol++) // For all solutions for that region
                    {
                        streamWriter.Write(solutionsX[regX][sol].Properties.Position.X.P.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsX[regX][sol].Properties.Position.X.I.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                                           solutionsX[regX][sol].Properties.Position.X.D.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsX[regX][sol].Properties.MaxVelocityPlane.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                        for (int i = 0; i < solutionsX[regX][sol].Path_interp.Count; i++) // solution path
                        { streamWriter.Write("," + solutionsX[regX][sol].Path_interp[i].x.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsX[regX][sol].Errors.Count; i++) // solution error
                        { streamWriter.Write("," + solutionsX[regX][sol].Errors[i].x.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsX[regX][sol].Errors_deriv.Count; i++) // solution derivation error
                        { streamWriter.Write("," + solutionsX[regX][sol].Errors_deriv[i].x.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                    }
                }

                // Y
                for (int regY = 0; regY < staticRegionsY.Count; regY++) // For all regions in the axis
                {
                    streamWriter.Write(staticRegionsY[regY].start + "," + staticRegionsY[regY].end + ",1,," + // 1 -> Y
                                       staticRegionsY[regY].movement.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    for (int i = 0; i < staticRegionsY[regY].uavPath.Count; i++) // region real path
                    { streamWriter.Write("," + staticRegionsY[regY].uavPath[i].y.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                    streamWriter.WriteLine();

                    streamWriter.Write(",,,,"); // Log avg. fitness values
                    for (int fitInd = 0; fitInd < fitnessAvgY[regY].Count; fitInd++)
                    { streamWriter.Write("," + fitnessAvgY[regY][fitInd]); }
                    streamWriter.WriteLine();
                    streamWriter.Write(",,,,"); // Log max. fitness values
                    for (int fitInd = 0; fitInd < fitnessMaxY[regY].Count; fitInd++)
                    { streamWriter.Write("," + fitnessMaxY[regY][fitInd]); }
                    streamWriter.WriteLine();

                    for (int sol = 0; sol < solutionsY[regY].Count; sol++) // For all solutions for that region
                    {
                        streamWriter.Write(solutionsY[regY][sol].Properties.Position.Y.P.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsY[regY][sol].Properties.Position.Y.I.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                                           solutionsY[regY][sol].Properties.Position.Y.D.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsY[regY][sol].Properties.MaxVelocityHeight.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                        for (int i = 0; i < solutionsY[regY][sol].Path_interp.Count; i++) // solution path
                        { streamWriter.Write("," + solutionsY[regY][sol].Path_interp[i].y.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsY[regY][sol].Errors.Count; i++) // solution error
                        { streamWriter.Write("," + solutionsY[regY][sol].Errors[i].y.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsY[regY][sol].Errors_deriv.Count; i++) // solution derivation error
                        { streamWriter.Write("," + solutionsY[regY][sol].Errors_deriv[i].y.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                    }
                }

                // Z
                for (int regZ = 0; regZ < staticRegionsZ.Count; regZ++) // For all regions in the axis
                {
                    streamWriter.Write(staticRegionsZ[regZ].start + "," + staticRegionsZ[regZ].end + ",2,," + // 2 -> Z
                                       staticRegionsZ[regZ].movement.z.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    for (int i = 0; i < staticRegionsZ[regZ].uavPath.Count; i++) // region real path
                    { streamWriter.Write("," + staticRegionsZ[regZ].uavPath[i].z.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                    streamWriter.WriteLine();

                    streamWriter.Write(",,,,"); // Log avg. fitness values
                    for (int fitInd = 0; fitInd < fitnessAvgZ[regZ].Count; fitInd++)
                    { streamWriter.Write("," + fitnessAvgZ[regZ][fitInd]); }
                    streamWriter.WriteLine();
                    streamWriter.Write(",,,,"); // Log max. fitness values
                    for (int fitInd = 0; fitInd < fitnessMaxZ[regZ].Count; fitInd++)
                    { streamWriter.Write("," + fitnessMaxZ[regZ][fitInd]); }
                    streamWriter.WriteLine();

                    for (int sol = 0; sol < solutionsZ[regZ].Count; sol++) // For all solutions for that region
                    {
                        streamWriter.Write(solutionsZ[regZ][sol].Properties.Position.Z.P.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsZ[regZ][sol].Properties.Position.Z.I.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                                           solutionsZ[regZ][sol].Properties.Position.Z.D.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + solutionsZ[regZ][sol].Properties.MaxVelocityPlane.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",");
                        for (int i = 0; i < solutionsZ[regZ][sol].Path_interp.Count; i++) // solution path
                        { streamWriter.Write("," + solutionsZ[regZ][sol].Path_interp[i].z.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsZ[regZ][sol].Errors.Count; i++) // solution error
                        { streamWriter.Write("," + solutionsZ[regZ][sol].Errors[i].z.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                        streamWriter.Write(",,,,");
                        for (int i = 0; i < solutionsZ[regZ][sol].Errors_deriv.Count; i++) // solution derivation error
                        { streamWriter.Write("," + solutionsZ[regZ][sol].Errors_deriv[i].z.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
                        streamWriter.WriteLine();
                    }
                }
                streamWriter.Flush();
            }
#endregion

            Debug.LogFormat("Runtime after logging: " + (DateTime.Now.TimeOfDay - runtimeStart) + "\n");

            // Play sound when done
            //AudioSource audio = gameObject.AddComponent<AudioSource>();
            //audio.clip = (AudioClip)Resources.Load("bell");
            //audio.Play();

        }
    }

    // Evaluation of the fitness of a chromosome
    public double Evaluate(Accord.Genetic.IChromosome iChromosome)
    {
        double result = 0;

        PMPIDProperties chrProp = DecodeBinaryChromosome(iChromosome, axis);

        // Create a solution with the new properties and hand it to the pmModelWrapper
        Solution chrSolution = new Solution();
        chrSolution.Properties = chrProp;
        pmModelWrapper.SetModelProperties(chrProp);

        // Perform prediction
        chrSolution.Path = pmModelWrapper.GetPathOptimized(0, duration_region_sec, timestepModel_sec);
        // Match prediction and real path indices by interpolation
        chrSolution.Path_interp = this.GetInterpolatedPath(chrSolution);
        // Evaluate Prediction
        chrSolution.Errors = this.CalculateInterpPredictionErrors(chrSolution);
        chrSolution.Error_total = this.CalculateTotalError(chrSolution.Errors);
        chrSolution.Errors_deriv = this.CalculatePredictionErrors_deriv(chrSolution);
        chrSolution.Error_deriv_total = this.CalculateTotalError_deriv(chrSolution.Errors_deriv);

        Vector3 fitness = GetFitnessXYZ(chrSolution);

        switch (axis)
        {
            case 0:
                solutionsX[regionIndex].Add(chrSolution);
                result = fitness.x;
                break;
            case 1:
                solutionsY[regionIndex].Add(chrSolution);
                result = fitness.y;
                break;
            case 2:
                solutionsZ[regionIndex].Add(chrSolution);
                result = fitness.z;
                break;
        }
        return result;
    }

    // Get the PID Properties from the Binary Chromosome
    public PMPIDProperties DecodeBinaryChromosome(Accord.Genetic.IChromosome iChromosome, int axis_index)
    {
        BinaryChromosome chr = (BinaryChromosome)iChromosome;

        // Decode the chromosome
        BitArray bitArray = new BitArray(BitConverter.GetBytes(chr.Value));
        int codeLength = 16; // Length of the bit sequence per parameter
        int parameterCount = 4;
        float[] par = new float[parameterCount];

        // Save the decimal values in an array
        for (int paraInd = 0; paraInd < par.Length; paraInd++)
        {
            for (int n = 0; n < codeLength; n++)
            {
                if (bitArray[paraInd * codeLength + n])
                {
                    par[paraInd] = par[paraInd] + Mathf.Pow(2f, n);
                }
            }
        }

        // Convert to scale
        par[0] = PLowerBound + (PUpperBound - PLowerBound) * (par[0] / (Mathf.Pow(2f, codeLength) - 1));
        par[1] = ILowerBound + (IUpperBound - ILowerBound) * (par[1] / (Mathf.Pow(2f, codeLength) - 1));
        par[2] = DLowerBound + (DUpperBound - DLowerBound) * (par[2] / (Mathf.Pow(2f, codeLength) - 1));

        if (axis_index == 1) // Y
        {
            par[3] = MaxVelHeightLB + (MaxVelHeightUB - MaxVelHeightLB) * (par[3] / (Mathf.Pow(2f, codeLength) - 1));
        }
        else // X, Z
        {
            par[3] = MaxVelPlaneLB + (MaxVelPlaneUB - MaxVelPlaneLB) * (par[3] / (Mathf.Pow(2f, codeLength) - 1));
        }

        PMPIDProperties chrProp = new PMPIDProperties(
        new PIDProperties(0.0f, 0.0f, 0.0f),     // Position X: P, I, D
        new PIDProperties(0.0f, 0.0f, 0.0f),     // Position Y: P, I, D
        new PIDProperties(0.0f, 0.0f, 0.0f),     // Position Z: P, I, D
        new PIDProperties(1f, 0.0f, 0.0f),          // Rotation X: P, I, D
        new PIDProperties(1f, 0.0f, 0.0f),          // Rotation Y: P, I, D
        new PIDProperties(1f, 0.0f, 0.0f),          // Rotation Z: P, I, D
        3000.0f,                                       // Max velocity in height translation in m/s
        5000.0f,                                       // Max velocity in planar translation in m/s
        new Vector3(180, 180, 180),                 // Max velocity in rotation in degree/s 
        0.0f,                                       // Set delay of reaction 
        0.1f);

        switch (axis_index)
        {
            case 0:
                chrProp.Position.X = new PIDProperties(par[0], par[1], par[2]);
                chrProp.MaxVelocityPlane = par[3];
                break;
            case 1:
                chrProp.Position.Y = new PIDProperties(par[0], par[1], par[2]);
                chrProp.MaxVelocityHeight = par[3];
                break;
            case 2:
                chrProp.Position.Z = new PIDProperties(par[0], par[1], par[2]);
                chrProp.MaxVelocityPlane = par[3];
                break;

        }

        return chrProp;
    }

    // Calculate the fitness values of a solution
    public Vector3 GetFitnessXYZ(Solution sol)
    {
        Vector3 fitness = new Vector3();

        fitness.x = 1 /
            ((weightError * sol.Error_total.x + weightDerivativeError * sol.Error_deriv_total.x + 0.0000001f) / //to avoid dividing by zero
                (weightError + weightDerivativeError));
        fitness.y = 1 /
            ((weightError * sol.Error_total.y + weightDerivativeError * sol.Error_deriv_total.y + 0.0000001f) /
                (weightError + weightDerivativeError));
        fitness.z = 1 /
            ((weightError * sol.Error_total.z + weightDerivativeError * sol.Error_deriv_total.z + 0.0000001f) /
                (weightError + weightDerivativeError));

        return fitness;
    }

#region ErrorCalculation
    // Creates a point by interpolating the prediction path at the given time
    private Vector3 GetInterpolatedPoint(double interpTime, Solution solution)
    {
        Vector3 interpPoint = new Vector3();

        // Calculate between which two points we should interpolate (index = 0 --> between 0 and 1)
        int predIndex = 0;
        while (interpTime >= timestamps_log[startIndex] + timestepModel_sec * 1000 * predIndex)
        {
            predIndex++;
        }
        predIndex--;
        if (predIndex < 0)
            predIndex = 0;

        // Linear interpolation
        double deltaT = interpTime - (timestamps_log[startIndex] + timestepModel_sec * 1000 * predIndex);

        if (predIndex < solution.Path.Count - 1)
        {
            interpPoint.x = solution.Path[predIndex].x + (solution.Path[predIndex + 1].x - solution.Path[predIndex].x) * (float)deltaT / ((float)timestepModel_sec * 1000);
            interpPoint.y = solution.Path[predIndex].y + (solution.Path[predIndex + 1].y - solution.Path[predIndex].y) * (float)deltaT / ((float)timestepModel_sec * 1000);
            interpPoint.z = solution.Path[predIndex].z + (solution.Path[predIndex + 1].z - solution.Path[predIndex].z) * (float)deltaT / ((float)timestepModel_sec * 1000);
        }
        else if (predIndex >= 0) // Extrapolate the last points
        {
            interpPoint.x = solution.Path[solution.Path.Count - 1].x + (solution.Path[solution.Path.Count - 1].x - solution.Path[solution.Path.Count - 2].x) * (float)deltaT / ((float)timestepModel_sec * 1000);
            interpPoint.y = solution.Path[solution.Path.Count - 1].y + (solution.Path[solution.Path.Count - 1].y - solution.Path[solution.Path.Count - 2].y) * (float)deltaT / ((float)timestepModel_sec * 1000);
            interpPoint.z = solution.Path[solution.Path.Count - 1].z + (solution.Path[solution.Path.Count - 1].z - solution.Path[solution.Path.Count - 2].z) * (float)deltaT / ((float)timestepModel_sec * 1000);
        }

        return interpPoint;
    }

    // Returns the interpolated prediction path
    private List<Vector3> GetInterpolatedPath(Solution solution)
    {
        List<Vector3> path_interp = new List<Vector3>();

        for (int i = 0; i < realUavPath_region.Count; i++)
        {
            Vector3 ipoint = this.GetInterpolatedPoint(timestamps_log[startIndex + i], solution);

            path_interp.Add(ipoint);
        }

        return path_interp;
    }

    // Calculates the diffenrences between the real uav path and the (interpolated) prediction path
    private List<Vector3> CalculateInterpPredictionErrors(Solution solution)
    {
        List<Vector3> predErrors = new List<Vector3>();

        for (int i = 0; i < realUavPath_region.Count; i++)
        {
            Vector3 error = new Vector3();

            error.x = (realUavPath_region[i].x - solution.Path_interp[i].x) / Mathf.Abs(region_movement.x); // Normalize Error with total travel
            error.y = (realUavPath_region[i].y - solution.Path_interp[i].y) / Mathf.Abs(region_movement.y);
            error.z = (realUavPath_region[i].z - solution.Path_interp[i].z) / Mathf.Abs(region_movement.z);

            if (Mathf.Abs(region_movement.x) < 0.00001f)
            {
                error.x = 0.00001f;
            }
            if (Mathf.Abs(region_movement.y) < 0.00001f)
            {
                error.y = 0.00001f;
            }
            if (Mathf.Abs(region_movement.z) < 0.00001f)
            {
                error.z = 0.00001f;
            }

            predErrors.Add(error);
        }

        return predErrors;
    }

    // Integrates/sums up the errors from CalculateInterpPredictionErrors
    private Vector3 CalculateTotalError(List<Vector3> predictionErrors)
    {
        Vector3 totalError = new Vector3();

        for (int i = 0; i < predictionErrors.Count; i++)
        {
            float dt_sec = (float)(timestamps_log[startIndex + i + 1] - timestamps_log[startIndex + i]) / 1000;

            totalError.x = totalError.x + Mathf.Abs(predictionErrors[i].x) / dt_sec;
            totalError.y = totalError.y + Mathf.Abs(predictionErrors[i].y) / dt_sec; 
            totalError.z = totalError.z + Mathf.Abs(predictionErrors[i].z) / dt_sec; 
        }

        return totalError;
    }

    // Calculates the differences of the slopes of the real and predicted path
    private List<Vector3> CalculatePredictionErrors_deriv(Solution solution)
    {
        List<Vector3> predErrors_deriv = new List<Vector3>();

        // Iterate over the region
        for (int i = 0; i < realUavPath_region.Count; i++)
        {
            if (i != realUavPath_region.Count - 1)
            {
                float dt_sec = (float)(timestamps_log[startIndex + i + 1] - timestamps_log[startIndex + i]) / 1000;

                Vector3 real_deriv = new Vector3();
                Vector3 pred_deriv = new Vector3();
                Vector3 error_deriv = new Vector3();

                real_deriv.x = (realUavPath_region[i + 1].x - realUavPath_region[i].x) / dt_sec;
                real_deriv.y = (realUavPath_region[i + 1].y - realUavPath_region[i].y) / dt_sec;
                real_deriv.z = (realUavPath_region[i + 1].z - realUavPath_region[i].z) / dt_sec;

                pred_deriv.x = (solution.Path_interp[i + 1].x - solution.Path_interp[i].x) / dt_sec;
                pred_deriv.y = (solution.Path_interp[i + 1].y - solution.Path_interp[i].y) / dt_sec;
                pred_deriv.z = (solution.Path_interp[i + 1].z - solution.Path_interp[i].z) / dt_sec;

                error_deriv.x = real_deriv.x - pred_deriv.x / Mathf.Abs(region_movement.x);
                error_deriv.y = real_deriv.y - pred_deriv.y / Mathf.Abs(region_movement.y);
                error_deriv.z = real_deriv.z - pred_deriv.z / Mathf.Abs(region_movement.z);

                if (Mathf.Abs(region_movement.x) < 0.00001f)
                {
                    error_deriv.x = 0.00001f;
                }
                if (Mathf.Abs(region_movement.y) < 0.00001f)
                {
                    error_deriv.y = 0.00001f;
                }
                if (Mathf.Abs(region_movement.z) < 0.00001f)
                {
                    error_deriv.z = 0.00001f;
                }

                predErrors_deriv.Add(error_deriv);

            }
            else // handle last
            {
                float dt_sec = (float)(timestamps_log[startIndex + i] - timestamps_log[startIndex + i - 1]) / 1000;

                Vector3 real_deriv = new Vector3();
                Vector3 pred_deriv = new Vector3();
                Vector3 error_deriv = new Vector3();

                real_deriv.x = (realUavPath_region[i].x - realUavPath_region[i - 1].x) / dt_sec;
                real_deriv.y = (realUavPath_region[i].y - realUavPath_region[i - 1].y) / dt_sec;
                real_deriv.z = (realUavPath_region[i].z - realUavPath_region[i - 1].z) / dt_sec;

                pred_deriv.x = (solution.Path_interp[i].x - solution.Path_interp[i - 1].x) / dt_sec;
                pred_deriv.y = (solution.Path_interp[i].y - solution.Path_interp[i - 1].y) / dt_sec;
                pred_deriv.z = (solution.Path_interp[i].z - solution.Path_interp[i - 1].z) / dt_sec;

                error_deriv.x = real_deriv.x - pred_deriv.x / Mathf.Abs(region_movement.x);
                error_deriv.y = real_deriv.y - pred_deriv.y / Mathf.Abs(region_movement.y);
                error_deriv.z = real_deriv.z - pred_deriv.z / Mathf.Abs(region_movement.z);

                if (Mathf.Abs(region_movement.x) < 0.00001f)
                {
                    error_deriv.x = 0.00001f;
                }
                if (Mathf.Abs(region_movement.y) < 0.00001f)
                {
                    error_deriv.y = 0.00001f;
                }
                if (Mathf.Abs(region_movement.z) < 0.00001f)
                {
                    error_deriv.z = 0.00001f;
                }

                predErrors_deriv.Add(error_deriv);

            }


        }

        return predErrors_deriv;
    }

    // Integrates/sums up the derivative errors from CalculatePredictionErrors_deriv
    private Vector3 CalculateTotalError_deriv(List<Vector3> predictionErrors_deriv)
    {
        Vector3 totalError_deriv = new Vector3();

        for (int i = 0; i < predictionErrors_deriv.Count; i++)
        {
            float dt_sec = (float)(timestamps_log[startIndex + i + 1] - timestamps_log[startIndex + i]) / 1000;

            totalError_deriv.x = totalError_deriv.x + Mathf.Abs(predictionErrors_deriv[i].x) / dt_sec;
            totalError_deriv.y = totalError_deriv.y + Mathf.Abs(predictionErrors_deriv[i].y) / dt_sec;
            totalError_deriv.z = totalError_deriv.z + Mathf.Abs(predictionErrors_deriv[i].z) / dt_sec;
        }

        return totalError_deriv;
    }
#endregion

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
                this.skipEntries++; // Show entry number in inspector

                double timestamp;

                //String operatorCommand;
                //String uavCommand;

                Pose operatorPose = new Pose();
                Pose uavPose = new Pose();

                String[] lineArguments = streamReader.ReadLine().Split(';');

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
                pmModelWrapper.OperatorPoseHistory.Add(timestamp, operatorPose);
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

                    String[] lineArguments = streamReader.ReadLine().Split(';');

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
                    pmModelWrapper.OperatorPoseHistory.Add(timestamp, operatorPose);

                }
                else if (streamReader.EndOfStream)
                {
                    Debug.LogFormat("End of Stream reached at line {0}. (Entry Nr. {1})", line, line - 3);
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
        List<StaticRegion> regions = new List<StaticRegion>();

        int startingIndex = 0;
        bool isStatic = false;

        // Loop over derivative List
        for (int i = 0; i < deviations.Count; i++)
        {
            // If position derivative is zero
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
                regions.Add(staticRegion);
                isStatic = false;
            }

        }

        if (isStatic) // If we are in a static region at the last element, the region gets cut off and saved
        {
            StaticRegion staticRegion = new StaticRegion(startingIndex, deviations.Count, deviations.Count - startingIndex);
            regions.Add(staticRegion);
        }

        return regions;
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

            dev.rotation.SetEulerAngles((poseHistory[i + 1].rotation.eulerAngles.x - poseHistory[i].rotation.eulerAngles.x) / dt,
                                        (poseHistory[i + 1].rotation.eulerAngles.y - poseHistory[i].rotation.eulerAngles.y) / dt,
                                        (poseHistory[i + 1].rotation.eulerAngles.z - poseHistory[i].rotation.eulerAngles.z) / dt);


            result.Add(dev);
        }

        return result;
    }
#endregion
}
#endif