using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SAINTState : MonoBehaviour
{
    public enum SaintControlState { MANUAL, AUTONOMOUS, NONE };

    public enum SaintAutonomousState { EXECUTING, PAUSED, RESUME, SEMIAUTONOMOUS, IDLE };

    public enum SaintGripperState { OPEN, CLOSED };

    public enum SaintCycle { NONE, GRASP, BOX, PICK };

    public enum SaintStateMachineLevel { ERROR, WARNING, DEBUG, INFO };

    // Current control state of SAINT
    private SaintControlState controlState = new SaintControlState();
    /// <summary>
    /// Set or get the control state of the SAINT
    /// </summary>
    public SaintControlState ControlState
    {
        get
        {
            return controlState;
        }

        set
        {
            controlState = value;
        }
    }

    // Current gripper state of SAINT
    private SaintGripperState gripperState = new SaintGripperState();
    /// <summary>
    /// Set or get the gripper state of the SAINT
    /// </summary>
    public SaintGripperState GripperState
    {
        get
        {
            return gripperState;
        }

        set
        {
            gripperState = value;
        }
    }

    // Current control state of SAINT
    private SaintAutonomousState autonomousState = new SaintAutonomousState();
    /// <summary>
    /// Set or get the control state of the SAINT
    /// </summary>
    public SaintAutonomousState AutonomousState
    {
        get
        {
            return autonomousState;
        }

        set
        {
            autonomousState = value;
        }
    }

    // Current state machine level of SAINT
    private SaintStateMachineLevel stateMachineLevel = new SaintStateMachineLevel();
    /// <summary>
    /// Set or get the state machine level of the SAINT
    /// </summary>
    public SaintStateMachineLevel StateMachineLevel
    {
        get
        {
            return stateMachineLevel;
        }

        set
        {
            stateMachineLevel = value;
        }
    }

    // Current state machine flag of SAINT
    private string stateMachine ="";
    /// <summary>
    /// Set or get the state machine info of the SAINT
    /// </summary>
    public string StateMachine
    {
        get
        {
            return stateMachine;
        }

        set
        {
            stateMachine = value;
        }
    }

    // Current cycle state of SAINT
    private SaintCycle cycle = new SaintCycle();
    /// <summary>
    /// Set or get the cycle state of the SAINT
    /// </summary>
    public SaintCycle Cycle
    {
        get
        {
            return cycle;
        }

        set
        {
            cycle = value;
        }
    }



    //ConnectionState
    private bool isConnected = false;
    /// <summary>
    /// Set or get the connection state of the SAINT
    /// </summary>
    public bool IsConnected { get => isConnected; set => isConnected = value; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
