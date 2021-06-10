using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SAINTHandler : MonoBehaviour
{

    private OperatorState operatorState;
    private OperatorArmState operatorArmState;

    public SAINTState saintState;

    public GameObject uiOperatorPosition;
    public GameObject uiOperatorOldPosition;

    public SAINTRightJoystick saintRightJoystick;

    // Get Buttons
    public float operatorSensitivty = 0.25f;
    public float operatorRotationSensitivty = 1.0f;
    public float moveYFaktorWheel = 0.25f;

    private float f_key_timer = 0.0f;
    private float realTime;

    private int reversefactor;

    // SAINT New
    public Camera mainCamera;
    private Vector3 previousMousePosition;
    private Vector3 lastPosition;

    public float rotationRateX;
    public float rotationRateY;
    private Vector3 targetPoint;

    public float viewAngle = 30f;

    private bool setPosition = false;
    private Color setPositionColor;

    // Use this for initialization
    void Start()
    {
        //mainCamera = this.GetComponentInChildren<Camera>();
        targetPoint = new Vector3();
        this.transform.LookAt(targetPoint);
        mainCamera.transform.localPosition = new Vector3((this.transform.position - targetPoint).magnitude * Mathf.Tan(Mathf.Deg2Rad * viewAngle), 0, 0);

        operatorArmState = this.GetComponent<OperatorArmState>();
        setPosition = true;
        setPositionColor = uiOperatorPosition.GetComponent<Renderer>().material.color;
        //uiOperatorPosition.GetComponent<Renderer>().material.color = Color.blue;

        uiOperatorPosition.GetComponent<Renderer>().material.color = setPositionColor;
        operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;

        // Preset position on starting point
        operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cur_pos = uiOperatorPosition.transform.position;
        Vector3 cur_rot = uiOperatorPosition.transform.rotation.eulerAngles;

        if (!saintRightJoystick.FPVControl)
        {
            reversefactor = -1;
        }
        else reversefactor = 1;

        // Add x movement
        if (Input.GetKey(KeyCode.W))
        {
            cur_pos.z += operatorSensitivty * reversefactor;
        }
        if (Input.GetKey(KeyCode.S))
        {
            cur_pos.z -= operatorSensitivty * reversefactor;
        }
        // Add z movement
        if (Input.GetKey(KeyCode.A))
        {
            cur_pos.x -= operatorSensitivty * reversefactor;
        }
        if (Input.GetKey(KeyCode.D))
        {
            cur_pos.x += operatorSensitivty * reversefactor;
        }
        // Add y movement
        if (Input.GetKey(KeyCode.E))
        {
            cur_pos.y += operatorSensitivty;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            cur_pos.y -= operatorSensitivty;
        }
        uiOperatorPosition.transform.position = cur_pos;

        // Add z rotation
        if (Input.GetKey(KeyCode.UpArrow))
        {
            cur_rot.x -= operatorRotationSensitivty;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            cur_rot.x += operatorRotationSensitivty;
        }
        // Add x movement
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            cur_rot.z += operatorRotationSensitivty;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            cur_rot.z -= operatorRotationSensitivty;
        }
        // Add z movement
        if (Input.GetKey(KeyCode.Y))
        {
            cur_rot.y -= operatorRotationSensitivty;
        }
        if (Input.GetKey(KeyCode.X))
        {
            cur_rot.y += operatorRotationSensitivty;
        }
        Vector3 tmp = uiOperatorPosition.transform.rotation.eulerAngles - cur_rot;
        uiOperatorPosition.transform.Rotate(tmp.x, tmp.y, tmp.z);
        //uiOperatorPosition.transform.rotation = Quaternion.Euler(cur_rot);


        if (setPosition)
        {
            operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
            operatorArmState.EndEffector.rotation = uiOperatorPosition.transform.rotation;

            uiOperatorOldPosition.transform.position = operatorArmState.EndEffector.position;
            uiOperatorOldPosition.transform.rotation = operatorArmState.EndEffector.rotation;
        }

        // Handle Space and active and not active mode
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleSetPostion();
        }

        //Get real time form system
        realTime = Time.realtimeSinceStartup;
        //print(operatorArmState.EndEffector.position);
    }


    public void ToggleSetPostion()
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
    }

    public void ToggleSetPostion(bool newValue)
    {
        setPosition = newValue;

        if (setPosition)
        {
            uiOperatorPosition.GetComponent<Renderer>().material.color = setPositionColor;
            operatorArmState.EndEffector.position = uiOperatorPosition.transform.position;
        }
        else
        {
            uiOperatorPosition.GetComponent<Renderer>().material.color = Color.blue;
        }
    }

    // Update camera control in 3D World
    void LateUpdate()
    {
        Vector3 mouseCoordinates = Display.RelativeMouseAt(new Vector3(Input.mousePosition.x, Input.mousePosition.y));

        Vector3 mouseViewportPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        if (isMouseinScreen())
        {
            // Calculating important inputs;
            float wheelAxis = Input.GetAxis("Mouse ScrollWheel");

            if (Input.GetKey(KeyCode.Mouse1))
            {
                //print(mainCamera.ScreenToWorldPoint(Input.mousePosition));

                this.transform.RotateAround(targetPoint, Vector3.up, (mouseViewportPosition.x - previousMousePosition.x) * rotationRateX);
                this.transform.RotateAround(targetPoint, Vector3.left, (mouseViewportPosition.y - previousMousePosition.y) * rotationRateY * Mathf.Cos(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y));
                this.transform.RotateAround(targetPoint, Vector3.back, (mouseViewportPosition.y - previousMousePosition.y) * rotationRateY * -Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y));

                this.transform.LookAt(targetPoint);

                //print(Mathf.Cos(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y));

                Vector3 vec = this.transform.position - targetPoint;

                if (wheelAxis < 0)
                {
                    this.transform.position += vec * moveYFaktorWheel;
                }
                if (wheelAxis > 0)
                {
                    this.transform.position -= vec * moveYFaktorWheel;
                }
                //print(mouseViewportPosition);
                mainCamera.transform.localPosition = new Vector3((this.transform.position - targetPoint).magnitude * Mathf.Tan(Mathf.Deg2Rad * viewAngle), 0, 0);
                //print(mainCamera.transform.localPosition);// = new Vector3(0.1f, 0, 0);
                //print(this.transform.localPosition);

                //this.transform.rotation = Quaternion.Euler(this.transform.rotation.eulerAngles + (new Vector3(Input.mousePosition.x - previousMousePosition.x, 0, Input.mousePosition.y - previousMousePosition.y)) * translationFaktor);
                /*}
                else
                { 
                    //Vector3 change = ((new Vector3(Input.mousePosition.x - previousMousePosition.x, 0, Input.mousePosition.y - previousMousePosition.y)) * translationFaktor);
                    float xDirection = -(mouseViewportPosition.x - previousMousePosition.x) * Mathf.Cos(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y) - (mouseViewportPosition.y - previousMousePosition.y) * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y);
                    float yDirection = (mouseViewportPosition.x - previousMousePosition.x) * Mathf.Sin(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y) - (mouseViewportPosition.y - previousMousePosition.y) * Mathf.Cos(Mathf.Deg2Rad * this.transform.rotation.eulerAngles.y);
                    Vector3 change = ((new Vector3(xDirection, 0, yDirection) * (camera.orthographicSize / translationFaktor)));
                    targetPoint += change;
                    this.transform.position += change;

                    if (-boundary.x > this.transform.position.x)
                    {
                        this.transform.position = new Vector3(-boundary.x, this.transform.position.y, this.transform.position.z);
                        targetPoint.x -= (Input.mousePosition.x - previousMousePosition.x) * translationFaktor;
                    }
                    if (boundary.x < this.transform.position.x)
                    {
                        this.transform.position = new Vector3(boundary.x, this.transform.position.y, this.transform.position.z);
                        targetPoint.x += (Input.mousePosition.x - previousMousePosition.x) * translationFaktor;
                    }
                    if (-boundary.y > this.transform.position.z)
                    {
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -boundary.y);
                        targetPoint.z -= (Input.mousePosition.y - previousMousePosition.y) * translationFaktor;
                    }
                    if (boundary.y < this.transform.position.z)
                    {
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, boundary.y);
                        targetPoint.z += (Input.mousePosition.y - previousMousePosition.y) * translationFaktor;
                    }
                }*/
                //this.transform.LookAt(targetPoint);
            }
        }
        previousMousePosition = mouseViewportPosition;
    }

    /// <summary>
    /// Check if Mouse is over screen
    /// </summary>
    /// <returns>True if over screen else False</returns>
    private bool isMouseinScreen()
    {
        Vector3 tmp = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        return (tmp.x >= 0.0f && tmp.x <= 1.0f && tmp.y <= 1.0f && tmp.y >= 0.0f);
    }
}
