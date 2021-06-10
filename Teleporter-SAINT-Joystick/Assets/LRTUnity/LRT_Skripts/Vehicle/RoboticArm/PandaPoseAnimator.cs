using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PandaPoseAnimator : MonoBehaviour
{
    // moveable gimbal parts
    [Header("Panda gameobjects")]
    public Transform A1;
    public Transform A2;
    public Transform A3;
    public Transform A4;
    public Transform A5;
    public Transform A6;
    public Transform A7;

    public Transform finger1;
    public Transform finger2;

    [Header("Jointstates")]
    public bool overrideValues;

    public float valueA1;
    public float valueA2;
    public float valueA3;
    public float valueA4;
    public float valueA5;
    public float valueA6;
    public float valueA7;
    public float valueFinger1;
    public float valueFinger2;

    [Header("Robotic Arm State")]
    public RoboticArmState armState;

    private Vector3 offsetA1;
    private Vector3 offsetA2;
    private Vector3 offsetA3;
    private Vector3 offsetA4;
    private Vector3 offsetA5;
    private Vector3 offsetA6;
    private Vector3 offsetA7;

    // Start is called before the first frame update
    void Start()
    {
        offsetA1 = A1.eulerAngles;
        offsetA2 = A2.localRotation.eulerAngles;
        offsetA3 = new Vector3(0, 180, 180);
        offsetA4 = new Vector3(90, 90, 90);
        offsetA5 = new Vector3(-90, 90, -90);
        offsetA6 = new Vector3(90, -90, -90);
        offsetA7 = new Vector3(90, 90, 90);
    }

    // Update is called once per frame
    void Update()
    {
        if(armState != null && !overrideValues)
        {
            //Debug.Log(armState.JointPosition);
            this.valueA1 = -Mathf.Rad2Deg*armState.JointPosition[0];
            this.valueA2 = -Mathf.Rad2Deg * armState.JointPosition[1];
            this.valueA3 = Mathf.Rad2Deg * armState.JointPosition[2];
            this.valueA4 = Mathf.Rad2Deg * armState.JointPosition[3];
            this.valueA5 = -Mathf.Rad2Deg * armState.JointPosition[4];
            this.valueA6 = -Mathf.Rad2Deg * armState.JointPosition[5];
            this.valueA7 = Mathf.Rad2Deg * armState.JointPosition[6];

            this.valueFinger1 = armState.JointPosition[7];
            this.valueFinger2 = armState.JointPosition[8];
        }

        A1.eulerAngles = offsetA1 + new Vector3(0, 0, this.valueA1);
        A2.localRotation = Quaternion.Euler(offsetA2 + new Vector3(0, 0, this.valueA2));
        A3.localRotation = Quaternion.Euler(offsetA3 + new Vector3(0, this.valueA3, 0));
        A4.localRotation = Quaternion.Euler(offsetA4 + new Vector3(this.valueA4, 0, 0));
        A5.localRotation = Quaternion.Euler(offsetA5 + new Vector3(this.valueA5, 0, 0));
        A6.localRotation = Quaternion.Euler(offsetA6 + new Vector3(this.valueA6, 0, -0));
        A7.localRotation = Quaternion.Euler(offsetA7 + new Vector3(this.valueA7, 0, 0));

        //MonoBehaviour.print(offsetA2);
        //MonoBehaviour.print(A2.eulerAngles);

        finger1.localPosition = new Vector3(0, 0.065f, -valueFinger1 / 2);
        finger2.localPosition = new Vector3(0, 0.065f, valueFinger2 / 2);
    }
}
