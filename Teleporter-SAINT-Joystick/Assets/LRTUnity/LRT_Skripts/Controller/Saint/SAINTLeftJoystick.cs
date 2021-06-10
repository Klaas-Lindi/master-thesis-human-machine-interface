using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SAINTLeftJoystick : MonoBehaviour
{
    private OperatorState operatorState;
    private OperatorArmState operatorArmState;

    private GameObject uiOperatorPosition;

    private bool setPosition = false;
    private Color setPositionColor;

    public float joystickSensitivity = 1.0f;
    public bool alterntaiveControl;

    // Start is called before the first frame update
    void Start()
    {
        setPosition = false;

        operatorArmState = this.GetComponent<OperatorArmState>();
        uiOperatorPosition = this.GetComponent<SAINTHandler>().uiOperatorPosition;

        setPositionColor = uiOperatorPosition.GetComponent<Renderer>().material.color;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cur_pos = uiOperatorPosition.transform.position;


        if (!this.GetComponent<SAINTRightJoystick>().FPVControl)
        {
            /*if (!alterntaiveControl)
            {
                uiOperatorPosition.transform.position = cur_pos;
                uiOperatorPosition.transform.Rotate(-joystickSensitivity * Input.GetAxis("TM_Y_Left"), -joystickSensitivity * Input.GetAxis("TM_Z_Left"), joystickSensitivity * Input.GetAxis("TM_X_Left"));
            }
            else
            {
                uiOperatorPosition.transform.Rotate(-joystickSensitivity * Input.GetAxis("TM_Y_Left"), -joystickSensitivity * Input.GetAxis("TM_X_Left"), joystickSensitivity * Input.GetAxis("TM_Z_Left"));
            }*/
            if (Input.GetKey(KeyCode.JoystickButton1))
            {
                uiOperatorPosition.transform.Rotate(-joystickSensitivity * Input.GetAxis("TM_Y_Left"), 0, joystickSensitivity * Input.GetAxis("TM_X_Left"));
            }
            else
            {
                Vector3 dir = new Vector3(0, -this.GetComponent<SAINTRightJoystick>().joystickSensitivity * Input.GetAxis("TM_Y_Left"), 0);
                //dir.x = joystickSensitivity * Input.GetAxis("TM_Y_Right");
                //dir.z = joystickSensitivity * Input.GetAxis("TM_X_Right");
                uiOperatorPosition.transform.Translate(dir);

                uiOperatorPosition.transform.Rotate(0, -joystickSensitivity * Input.GetAxis("TM_Z_Left"), 0);
            }

        }
        else
        {
            if (Input.GetKey(KeyCode.JoystickButton1))
            {
                uiOperatorPosition.transform.Rotate(-joystickSensitivity * Input.GetAxis("TM_Y_Left"), 0, joystickSensitivity * Input.GetAxis("TM_X_Left"));
            }
            else
            {
                Vector3 dir = new Vector3(0, -this.GetComponent<SAINTRightJoystick>().joystickSensitivity * Input.GetAxis("TM_Y_Left"), 0);
                //dir.x = joystickSensitivity * Input.GetAxis("TM_Y_Right");
                //dir.z = joystickSensitivity * Input.GetAxis("TM_X_Right");
                uiOperatorPosition.transform.Translate(dir, uiOperatorPosition.transform);

                uiOperatorPosition.transform.Rotate(0, -joystickSensitivity * Input.GetAxis("TM_Z_Left"), 0);
            }
        }

        /*if (setPosition)
        {
            operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
            operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;
        }*/

        // Handle Space and active and not active mode
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            setPosition = !setPosition;

            if (setPosition)
            {
                uiOperatorPosition.GetComponent<Renderer>().material.color = setPositionColor;
                operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
            }
            else
            {
                uiOperatorPosition.GetComponent<Renderer>().material.color = Color.blue;
            }
        }*/

    }
}
