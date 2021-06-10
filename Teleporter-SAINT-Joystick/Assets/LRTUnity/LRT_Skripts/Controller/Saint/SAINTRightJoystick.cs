using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SAINTRightJoystick : MonoBehaviour
{
    private OperatorState operatorState;
    private OperatorArmState operatorArmState;

    private GameObject uiOperatorPosition;
    private GameObject uiOperatorOldPosition;

    private bool setPosition = false;
    private Color setPositionColor;

    public float joystickSensitivity = 1.0f;
    public bool FPV_Control;

    private bool fpvControl;
    public bool FPVControl
    {
        get
        {
            return fpvControl;
        }

        set
        {
            fpvControl = value;
        }
    }

    private Transform view;

    // Start is called before the first frame update
    void Start()
    {
        setPosition = false;
        view = this.GetComponent<Transform>();
        operatorState = this.GetComponent<OperatorState>();
        operatorArmState = this.GetComponent<OperatorArmState>();
        uiOperatorPosition = this.GetComponent<SAINTHandler>().uiOperatorPosition;
        uiOperatorOldPosition = this.GetComponent<SAINTHandler>().uiOperatorOldPosition;

        setPositionColor = uiOperatorPosition.GetComponent<Renderer>().material.color;

    }

    // Update is called once per frame
    void Update()
    {

        Vector3 cur_pos = uiOperatorPosition.transform.position;
        Vector3 cur_rot = uiOperatorPosition.transform.rotation.eulerAngles;
        this.FPVControl = FPV_Control;

        if (!FPVControl)
        {
            // Add x movement
            //cur_pos.x -= joystickSensitivity * Input.GetAxis("TM_Y_Right");
            //cur_pos.z -= joystickSensitivity * Input.GetAxis("TM_X_Right");
            //if (!this.GetComponent<LeftJoystick>().alterntaiveControl)
            //    cur_pos.y -= joystickSensitivity * Input.GetAxis("TM_Z_Right");

            //uiOperatorPosition.transform.position = cur_pos;
            //camera forward and right vectors:
            //Vector3 forward = view.forward;
            //Vector3 right = view.right;
            //forward.y = 0;


            //this is the direction in the world space we want to move:
            Vector3 dir = new Vector3(joystickSensitivity * Input.GetAxis("TM_X_Right"), 0, -joystickSensitivity * Input.GetAxis("TM_Y_Right"));// + right * -joystickSensitivity * Input.GetAxis("TM_Y_Right");
            //dir.y = 0;
            //print(dir);
            uiOperatorPosition.transform.Translate(dir, view);
            cur_pos.x = uiOperatorPosition.transform.position.x;
            cur_pos.z = uiOperatorPosition.transform.position.z;
            //cur_pos.y = 0;// joystickSensitivity * Input.GetAxis("TM_Z_Right");

            uiOperatorPosition.transform.position = cur_pos;
        }
        else
        {
            Vector3 dir = new Vector3(-joystickSensitivity * Input.GetAxis("TM_X_Right"), 0, -joystickSensitivity * Input.GetAxis("TM_Y_Right"));
            //dir.x = joystickSensitivity * Input.GetAxis("TM_Y_Right");
            //dir.z = joystickSensitivity * Input.GetAxis("TM_X_Right");
            uiOperatorPosition.transform.Translate(dir, uiOperatorPosition.transform);
        }

        if (setPosition)
        {
            operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
            operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

            uiOperatorOldPosition.transform.position = operatorArmState.EndEffector.position;
            uiOperatorOldPosition.transform.rotation = operatorArmState.EndEffector.rotation;
        }

        // Handle Space and active and not active mode
        if (Input.GetKey(KeyCode.JoystickButton0))
        {
            setPosition = true;

            uiOperatorPosition.GetComponent<Renderer>().material.color = setPositionColor;
            operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        }
        else
        {
            setPosition = false;
            uiOperatorPosition.GetComponent<Renderer>().material.color = Color.blue;
        }

        if (Input.GetKeyDown(KeyCode.JoystickButton2))
        {
            operatorState.Command = TORCommand.SAINT.CloseGripper;
        }
        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            operatorState.Command = TORCommand.SAINT.OpenGripper;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            this.toggleFPVControl();
        }


        //Get real time form system
        //if (Input.GetKey(KeyCode.Joystick1Button1))
        //    print("Key pressed!");
        //print("TM_X_Right:" + Input.GetAxis("TM_X_Right"));
        //print("Horizontal:" + Input.GetAxis("Horizontal"));
        //print("TM_X_Right:" + Input.GetAxisRaw("TM_X_Right"));
        //print("TM_Y_Right:" + Input.GetAxis("TM_Y_Right"));
        //print("TM_Z_Right:" + Input.GetAxis("TM_Z_Right"));
        //print("TM_X_Left:" + Input.GetAxis("TM_X_Left"));
        //print("TM_Y_Left:" + Input.GetAxis("TM_Y_Left"));
        //print("TM_Z_Left:" + Input.GetAxis("TM_Z_Left"));

        //print("TM_Fire_2:" + Input.GetKey(KeyCode.JoystickButton0));
        //print("TM_Fire_3:" + Input.GetKey(KeyCode.JoystickButton1));
        //print("TM_Fire_4:" + Input.GetKey(KeyCode.JoystickButton2));
        //print("TM_Fire_5:" + Input.GetKey(KeyCode.JoystickButton3));

    }

    public void toggleFPVControl()
    {
        FPV_Control = !FPV_Control;
        print("FPFControl:" + FPV_Control);
    }
}
