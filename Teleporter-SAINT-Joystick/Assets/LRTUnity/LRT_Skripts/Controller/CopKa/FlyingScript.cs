using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingScript : MonoBehaviour {

    private OperatorState operatorState;
    private float realTime;
    private float wait = 20;
    private int SkriptIndex = 0;
    public bool SkriptEnabled = false;
    public int SkriptStartIndex = 0;
    public int SkriptEndIndex = -1;
    private Pose tmp = new Pose();

    private float standardInterval = 15; // sec
    private float standardRotationInterval = 10; // sec

    private string lastCommand = "";
    private int fps = 60;
    private int count = 0;

    // Use this for initialization
    public enum Script { ALL_AXIS_ONLY_POSITION, STANDARD_SHORT, STANDARD_LONG}
    public Script selectedScript;

    void Start () {
        operatorState = this.GetComponent<OperatorState>();
        SkriptIndex = SkriptStartIndex;
    }

    // Update is called once per frame
    void Update()
    {

        if (SkriptEnabled)
        {
            if (count++ > (int)(1.0f / Time.deltaTime))
            {
                count = 0;
                print("Time: " + wait.ToString("0") + " s | Skriptnummer: " + SkriptIndex + " | Command: " + lastCommand);
            }
            if (wait <= 0)
            {
                switch (selectedScript)
                {
                    case Script.ALL_AXIS_ONLY_POSITION: scriptAllAxisOnlyPosition(); break;
                    case Script.STANDARD_SHORT: scriptStandardShort(); break;
                    case Script.STANDARD_LONG: scriptStandardLong(); break;
                }

                lastCommand = operatorState.Command;
                SkriptIndex++;
                if (SkriptIndex > SkriptEndIndex && SkriptEndIndex > 0)
                {
                    SkriptEnabled = false;
                    print("End index reached! Script will be disabled");
                }
            }
            else
            {
                wait = wait - (Time.realtimeSinceStartup - realTime);
            }

            // Set parameters into operator state
            operatorState.OperatorPose = tmp;
            operatorState.VehicleActive = true;

            //Get real time form system
            realTime = Time.realtimeSinceStartup;
        }
    }

    private void scriptAllAxisOnlyPosition()
    {
        switch (SkriptIndex)
        {
            case 0: // Takeoff and get into starting Position
                operatorState.Command = TORCommand.VehicleTakeOff;
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 1: // get into starting Position

                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;

            // Until 10 the begin sequence starts
            // Translationtest in all axis
            case 10: // Move along Z-Axis positive
                tmp.position = new Vector3(5, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 11: // Move along X-Axis positive
                tmp.position = new Vector3(10, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 12: // Move along Y-Axis positive
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 13: // Move Z-Axis back
                tmp.position = new Vector3(10, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 14: // Move X-Axis back
                tmp.position = new Vector3(5, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 15: // Move Y-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 16: // Move x-z-Axis 
                tmp.position = new Vector3(10, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 17: // Move x-z-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 18: // Move x-y-Axis 
                tmp.position = new Vector3(10, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 19: // Move x-y-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 20: // Move y-z-Axis 
                tmp.position = new Vector3(5, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 21: // Move y-z-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 22: // Move x-y-z-Axis 
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 23: // Move x-y-z-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            // Starting from 90 the end sequence starts
            case 90: // Return to home
                operatorState.Command = TORCommand.VehicleRTL;
                wait = 120; // sec
                break;
        }
    }

    private void scriptStandardShort()
    {
        switch (SkriptIndex)
        {
            case 0: // Takeoff and get into starting Position
                operatorState.Command = TORCommand.VehicleTakeOff;
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            // Translationtest
            case 1: // Move along Z-Axis
                tmp.position = new Vector3(5, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 2: // Move along X-Axis
                tmp.position = new Vector3(10, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 3: // Move along Y-Axis
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 4: // Move X-Z-Axis back
                tmp.position = new Vector3(5, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 5: // Move X-Y-Axis back
                tmp.position = new Vector3(10, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 6: // Move z-Y-Axis back
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 7: // Move x-y-z-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 22: // Return to home
                operatorState.Command = TORCommand.VehicleRTL;
                wait = 120; // sec
                break;
        }
    }

    private void scriptStandardLong()
    {
        switch (SkriptIndex)
        {
            case 0: // Takeoff and get into starting Position
                operatorState.Command = TORCommand.VehicleTakeOff;
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            // Translationtest
            case 1: // Move along Z-Axis
                tmp.position = new Vector3(5, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 2: // Move along X-Axis
                tmp.position = new Vector3(10, 5, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 3: // Move along Y-Axis
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 4: // Move X-Z-Axis back
                tmp.position = new Vector3(5, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 5: // Move X-Y-Axis back
                tmp.position = new Vector3(10, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 6: // Move z-Y-Axis back
                tmp.position = new Vector3(10, 10, 10);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            case 7: // Move x-y-z-Axis back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardInterval; // sec
                break;
            // Rotationtest
            case 10: // Rotate yaw 90 degree
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 90, 0);
                wait = standardRotationInterval; // sec
                break;
            case 11: // Rotate yaw -90 degree
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, -90, 0);
                wait = standardRotationInterval; // sec
                break;
            case 12: // Rotate back
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardRotationInterval; // sec
                break;
            case 13: // Rotate pitch 45 degree
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(45,0, 0);
                wait = standardRotationInterval; // sec
                break;
            case 14: // Rotate pitch -45 degree
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(-45, 0, 0);
                wait = standardRotationInterval; // sec
                break;
            case 15: // Rotate pitch -30 degree and yaw 30 degree 
                tmp.position = new Vector3(5, 5, 5);
                tmp.rotation = Quaternion.Euler(-30, 30, 0);
                wait = standardRotationInterval; // sec
                break;
            case 16: // Reset and go to automatisation point 
                tmp.position = new Vector3(5, 10, 5);
                tmp.rotation = Quaternion.Euler(0, 0, 0);
                wait = standardRotationInterval; // sec
                break;
            // Test Automatism
            case 20: // Waypoints with height differences
                operatorState.Command = TORCommand.VehicleAutoWaypoints(
                         new List<Vector3> {
                             new Vector3(10, 15, 5),
                             new Vector3(10, 20, 10),
                             new Vector3(5, 10, 10),
                             new Vector3(5, 10, 5) });
                wait = 60; // sec
                break;
            case 21: // Circel around 10,10,10 with 10 m radius
                operatorState.Command = TORCommand.VehicleAutoCircle(10, new Vector3(10, 15, 10));
                wait = 120; // sec
                break;
            case 22: // Return to home
                operatorState.Command = TORCommand.VehicleRTL;
                wait = 120; // sec
                break;
        }
    }

}
