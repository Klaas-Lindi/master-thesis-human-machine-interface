using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

public class RobotControlSAINT : MonoBehaviour
{

    [Tooltip("State of the operator and user commands")]
    public OperatorState operatorState;

    [Tooltip("State of the saint system and saint msg")]
    public SAINTState saintState;


    // Meta commands from gnc to command
    private string command = "";
    /// <summary>
    /// Set or get the operational commands of gnc
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

    // send a callback to ROS
    private string callback = "";
    /// <summary>
    /// Set or get the operational callback
    /// </summary>
    public string Callback
    {
        get
        {
            return callback;
        }

        set
        {
            callback = value;
        }
    }

    // Meta operator sequence from operator
    private string operator_sequence = "";
    /// <summary>
    /// Set or get the operational sequence of operator
    /// </summary>
    public string Sequence
    {
        get
        {
            return operator_sequence;
        }

        set
        {
            operator_sequence = value;
        }
    }

    // Meta commands from gnc to command
    private string item_select = "";
    /// <summary>
    /// Set or get the operational commands of gnc
    /// </summary>
    public string ItemSelect
    {
        get
        {
            return item_select;
        }

        set
        {
            item_select = value;
        }
    }

    // Meta msg from the teleoperartor
    private string msg = "";
    /// <summary>
    /// Set or get the operational commands of gnc
    /// </summary>
    public string Msg
    {
        get
        {
            return msg;
        }

        set
        {
            msg = value;
        }
    }

    private Vector3 waypointMarking;
    public Vector3 WaypointMarking
    {
        get
        {
            return waypointMarking;
        }

        set
        {
            waypointMarking = value;
        }
    }

    private bool tmpFlag = false;

    private void Awake()
    {
        Msg = "";
    }

    // Task space pose
    private Vector3 task_space_pose_position;
    public Vector3 TaskSpacePosePosition
    {
        get
        {
            return task_space_pose_position;
        }

        set
        {
            task_space_pose_position = value;
        }
    }

    private Quaternion task_space_pose_orientation;
    public Quaternion TaskSpacePoseOrientation
    {
        get
        {
            return task_space_pose_orientation;
        }

        set
        {
            task_space_pose_orientation = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {


        // Set saint state at the beginning
        saintState.ControlState = SAINTState.SaintControlState.NONE;
        saintState.AutonomousState = SAINTState.SaintAutonomousState.IDLE;

        // Set String format globally
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
        Command = TORCommand.SAINT.Request;
    }

    // Update is called once per frame
    void Update()
    {
        // Go through all possible commands
        bool accept = false;
        foreach (string cmd in operatorState.Command.Split('\n'))
        {
            accept = false;
            // Threat single commands
            if (cmd.Length > 0)
                print(cmd);

            switch (cmd)
            {
                 
                // Control commands
                case TORCommand.SAINT.SwitchToManual:
                    //saintState.ControlState = SAINTState.SaintControlState.MANUAL;
                    goto case "accept";
                case TORCommand.SAINT.SwitchToAutonomous:
                    //saintState.ControlState = SAINTState.SaintControlState.AUTONOMOUS;
                    goto case "accept";
                case TORCommand.SAINT.CloseGripper:
                    //saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                    goto case "accept";
                case TORCommand.SAINT.OpenGripper:
                    //saintState.GripperState = SAINTState.SaintGripperState.OPEN;
                    goto case "accept";

                // Autonomous commands
                case TORCommand.SAINT.PauseAutonomous:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.PAUSED;
                    goto case "accept";
                case TORCommand.SAINT.ResumeAutonomous:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.CancelAutonomous:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.Grasp:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.BoxCycle:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.PickCycle:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.SemiAutonomous:
                    //saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                    
                // General Commands
                case TORCommand.SAINT.Request:
                    goto case "accept";
                case TORCommand.SAINT.Reset:
                    saintState.ControlState = SAINTState.SaintControlState.AUTONOMOUS;
                    saintState.AutonomousState = SAINTState.SaintAutonomousState.PAUSED;
                    goto case "accept";
                case "accept":
                    this.Command = operatorState.Command;
                    operatorState.Command = "";
                    accept = true;
                    break;
            }
            // command with parameter?
            /*if (!accept && cmd.Length > 0 && cmd.Contains(":"))
            {
                string identifier = cmd.Substring(0, cmd.LastIndexOf(":"));
                string[] values = cmd.Substring(cmd.LastIndexOf(':') + 1).Split(',');
                switch (identifier)
                {
                    // HANDLE STATES MESSAGE
                    case "DUMMY":
                        // Here implement the action
                        goto case "accept";
                    case "accept":
                        this.Command = operatorState.Command;
                        operatorState.Command = "";
                        accept = true;
                        break;
                }
            }*/
        }

        // Cmd not executed
        if (!accept && operatorState.Command.Length > 0)
        {
            Debug.LogWarning("Command not found: " + operatorState.Command);
            operatorState.Command = "";
        }
        
        // Handle msg from teleoperator cotrol
        accept = false;
        if (Msg == null)
            return;


        if (Msg.Length > 0)
            //print(Msg);



        foreach (string msg in Msg.Split('\n'))
        {
            accept = false;

            switch (msg)
            {
                // CONTROL MESSAGES
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.SwitchToManual:
                    saintState.ControlState = SAINTState.SaintControlState.MANUAL;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.SwitchToAutonomous:
                    saintState.ControlState = SAINTState.SaintControlState.AUTONOMOUS;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.CloseGripper:
                    saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.OpenGripper:
                    saintState.GripperState = SAINTState.SaintGripperState.OPEN;
                    goto case "accept";

                // Autonomous commands
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.PauseAutonomous:
                    saintState.AutonomousState = SAINTState.SaintAutonomousState.PAUSED;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.ResumeAutonomous:
                    saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING; // Has to be RESUME 
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.SemiAutonomous:
                    saintState.AutonomousState = SAINTState.SaintAutonomousState.SEMIAUTONOMOUS;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.Grasp:
                    //saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                    saintState.Cycle = SAINTState.SaintCycle.GRASP;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.BoxCycle:
                    //saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                    saintState.Cycle = SAINTState.SaintCycle.BOX;
                    goto case "accept";
                case TORCommand.SAINT.SUCCESS + TORCommand.SAINT.PickCycle:
                    //saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                    saintState.Cycle = SAINTState.SaintCycle.PICK;
                    goto case "accept";


                case "accept":
                    accept = true;
                    break;
            }
            
            if (!accept && msg.Length > 0 && msg.Contains(":"))
            {
                string identifier = msg.Substring(0, msg.LastIndexOf(":"));
                string[] values;
                switch (identifier)
                {
                    // HANDLE STATES MESSAGE
                    case "requestState":
                        values = msg.Substring(msg.LastIndexOf(':') + 1).Split(',');
                        switch (values[0])
                        {
                            case "autonomous":
                                saintState.ControlState = SAINTState.SaintControlState.AUTONOMOUS;
                                break;
                            case "manual":
                                saintState.ControlState = SAINTState.SaintControlState.MANUAL;
                                break;
                            case "none":
                                saintState.ControlState = SAINTState.SaintControlState.NONE;
                                break;
                        }
                        switch (values[1])
                        {

                            case "paused":
                                saintState.AutonomousState = SAINTState.SaintAutonomousState.PAUSED;
                                break;
                            case "running":
                                saintState.AutonomousState = SAINTState.SaintAutonomousState.EXECUTING;
                                break;
                            case "idle":
                                saintState.AutonomousState = SAINTState.SaintAutonomousState.IDLE;
                                break;
                        }
                        switch (values[2])
                        {

                            case "open":
                                saintState.GripperState = SAINTState.SaintGripperState.OPEN;
                                break;
                            case "closed":
                                saintState.GripperState = SAINTState.SaintGripperState.CLOSED;
                                break;
                        }
                        goto case "accept";
                    case "stateMachine":
                        values = msg.Substring(msg.LastIndexOf(':') + 1).Split(',');
                        switch (values[0])
                        {
                            case "error":
                                saintState.StateMachineLevel = SAINTState.SaintStateMachineLevel.ERROR;
                                break;
                            case "warning":
                                saintState.StateMachineLevel = SAINTState.SaintStateMachineLevel.WARNING;
                                break;
                            case "debug":
                                saintState.StateMachineLevel = SAINTState.SaintStateMachineLevel.DEBUG;
                                break;
                            case "info":
                                saintState.StateMachineLevel = SAINTState.SaintStateMachineLevel.INFO;
                                break;
                        }
                        saintState.StateMachine = values[1];
                        
                        goto case "accept";
                    case "accept":
                        accept = true;
                        break;
                }
            }
        }

        Msg = "";
    }
}
