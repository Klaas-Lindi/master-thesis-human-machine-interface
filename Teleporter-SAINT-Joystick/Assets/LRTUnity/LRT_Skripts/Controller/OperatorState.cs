using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class OperatorState : MonoBehaviour {

    // pose of the operator in the vr world
    private Pose operatorPose = new Pose();

    /// <summary>
    /// Set or get the current operator pose in vr
    /// </summary>
    public Pose OperatorPose
    {
        get
        {
            return operatorPose;
        }

        set
        {
            operatorPose = value;
        }
    }



    // flag of wished state of uav
    private bool vehicleActive = true;

    /// <summary>
    /// Set or get the state of vehicle from the operator input
    /// </summary>
    public bool VehicleActive
    {
        get
        {
            return vehicleActive;
        }

        set
        {
            vehicleActive = value;
        }
    }

    // Meta commands from operator
    private string command = "";
    /// <summary>
    /// Set or get the operational commands of operator
    /// </summary>
    public string Command
    {
        get
        {
            return command;
        }

        set
        {
            command = value;
        }
    }

    // Marked Item Grasp pose and selectbox from operator
    private string item_marking = "";
    /// <summary>
    /// Set or get the operational commands of operator
    /// </summary>
    public string ItemMarking
    {
        get
        {
            return item_marking;
        }

        set
        {
            item_marking = value;
        }
    }

    // Automatism command
    public enum Automatism { NONE, UAV_TAKEOFF, UAV_LANDING, UAV_RTL, UAV_AUTOSCAN_CIRCLE, UAV_AUTOSCAN_CIRCLE_SET_POINT, WAYPOINTS};
    Automatism activeAutomatism;

    public Automatism ActiveAutomatism
    {
        get
        {
            return activeAutomatism;
        }

        set
        {
            activeAutomatism = value;
        }
    }
    // Status change flag
    private bool stateChanged;
    /// <summary>
    /// Flag to show state changes
    /// </summary>
    public bool StateChanged
    {
        get
        {
            return stateChanged;
        }

        set
        {
            stateChanged = value;
        }
    }

    // Automatism Settings
    // Radius of circle
    private float radiusAutoCircle = 5f;
    /// <summary>
    /// Get and set the autocircle radius settings
    /// </summary>
    public float RadiusAutoCircle
    {
        get
        {
            return radiusAutoCircle;
        }

        set
        {
            radiusAutoCircle = value;
        }
    }

    // Waypoints from the operator
    private List<Vector3> waypoints;
    /// <summary>
    /// Get or set the waypoints the operator sets
    /// </summary>
    public List<Vector3> Waypoints
    {
        get
        {
            return waypoints;
        }

        set
        {
            waypoints = value;
        }
    }


    // Log options
    private bool readFromLog = false;
    /// <summary>
    /// This flag handles the operator input. When true the points from the log file are used 
    /// </summary>
    public bool ReadFromLog
    {
        get
        {
            return readFromLog;
        }

        set
        {
            readFromLog = value;
        }
    }
          
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void callCommandFromFieldNameAsString(string CommandAsField)
    {
        //Record record = new Record();

        FieldInfo[] fields = typeof(TORCommand.SAINT).GetFields(BindingFlags.Static | BindingFlags.Public);
        foreach (FieldInfo field in fields)
        {
            if (field.Name.Equals(CommandAsField))
            {
                this.Command = field.GetValue(null).ToString();
                return;
            }

        }
        print(CommandAsField + " was not found!");
    }

    public void callItemMarkingFromFunction(string ItemMarkingString)
    {
        this.ItemMarking = ItemMarkingString;
        return;
    }
}
