using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SAINTKeyboard : MonoBehaviour
{
    private OperatorState operatorState;
    private SAINTState saintState;

    private OperatorArmState operatorArmState;
    private GameObject uiOperatorPosition;
    private GameObject uiOperatorOldPosition;

    public GameObject hand;

    // Start is called before the first frame update
    void Start()
    {
        operatorState = this.GetComponent<OperatorState>();
        saintState = this.GetComponent<SAINTHandler>().saintState;

        uiOperatorPosition = this.GetComponent<SAINTHandler>().uiOperatorPosition;
        uiOperatorOldPosition = this.GetComponent<SAINTHandler>().uiOperatorOldPosition;
        operatorArmState = this.GetComponent<OperatorArmState>();
    }

    // Update is called once per frame
    void Update()
    {
        // Manual mode
        if (Input.GetKeyDown(KeyCode.M))            //press S key every time you want to send messages to ROS
        {
            this.SetManualMode();
        }

        // Manual mode
        if (Input.GetKeyDown(KeyCode.J))            //press S key every time you want to send messages to ROS
        {
            operatorState.Command = TORCommand.SAINT.SwitchToAutonomous;    //UNITY message for ROS
        }

        // Pause/Resume mode
        /*if (Input.GetKeyDown(KeyCode.N))            //press S key every time you want to send messages to ROS
        {
            if (saintState.AutonomousState == SAINTState.SaintAutonomousState.EXECUTING)
            {
                operatorState.Command = TORCommand.SAINT.PauseAutonomous;    //UNITY message for ROS
            }
            else
            {
                operatorState.Command = TORCommand.SAINT.ResumeAutonomous;    //UNITY message for ROS
            }
        }*/
        if (Input.GetKeyDown(KeyCode.B))            //press S key every time you want to send messages to ROS
        {
            operatorState.Command = TORCommand.SAINT.PauseAutonomous;    //UNITY message for ROS
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            operatorState.Command = TORCommand.SAINT.ResumeAutonomous;    //UNITY message for ROS
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            operatorState.Command = TORCommand.SAINT.CancelAutonomous;    //UNITY message for ROS
        }

        // Reset mode
        if (Input.GetKeyDown(KeyCode.R))            //press S key every time you want to send messages to ROS
        {
            this.ResetSaint();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            this.CenterEgoCamToPosition();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            operatorState.Command = TORCommand.SAINT.CloseGripper;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            operatorState.Command = TORCommand.SAINT.OpenGripper;
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            operatorState.Command = TORCommand.SAINT.Request;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            operatorState.Command = TORCommand.SAINT.Grasp;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            operatorState.Command = TORCommand.SAINT.BoxCycle;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            operatorState.Command = TORCommand.SAINT.PickCycle;
        }

        //Test
        //Quaternion quad = new Quaternion(1, 0, 0, 0);
        //uiOperatorPosition.transform.rotation = new Quaternion(quad.y, -quad.z, -quad.x, quad.w);
    }

    public void CenterEgoCamToPosition()
    {
        uiOperatorPosition.transform.position = hand.transform.position;
        uiOperatorPosition.transform.localPosition += new Vector3(0, -0.1029358f, 0.0000f);
        uiOperatorPosition.transform.rotation = hand.transform.rotation;
        uiOperatorPosition.transform.Rotate(new Vector3(0, -90, 0));

        operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

        uiOperatorOldPosition.transform.position = operatorArmState.EndEffector.position;
        uiOperatorOldPosition.transform.rotation = operatorArmState.EndEffector.rotation;
    }

    public void ResetSaint()
    {
        operatorState.Command = TORCommand.SAINT.Reset;    //UNITY message for ROS

        uiOperatorPosition.transform.position = new Vector3(0.0f, 0.5f, 0.3f);
        uiOperatorPosition.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, -180f));

        operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

        uiOperatorOldPosition.transform.position = operatorArmState.EndEffector.position;
        uiOperatorOldPosition.transform.rotation = operatorArmState.EndEffector.rotation;
    }

    public void SetManualMode()
    {
        uiOperatorPosition.transform.position = hand.transform.position;
        uiOperatorPosition.transform.localPosition += new Vector3(0, -0.1029358f, 0.0000f);
        uiOperatorPosition.transform.rotation = hand.transform.rotation;
        uiOperatorPosition.transform.Rotate(new Vector3(0, -90, 0));

        operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

        uiOperatorOldPosition.transform.position = operatorArmState.EndEffector.position;
        uiOperatorOldPosition.transform.rotation = operatorArmState.EndEffector.rotation;

        operatorState.Command = TORCommand.SAINT.SwitchToManual;    //UNITY message for ROS

        this.GetComponent<SAINTHandler>().ToggleSetPostion(true);
    }
}
