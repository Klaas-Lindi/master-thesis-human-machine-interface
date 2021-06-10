using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxEmptiness : MonoBehaviour
{
    public RobotControlSAINT robotControl;
    public GameObject SendCallback;

    public void BoxIsEmpty()
    {
        robotControl.Callback = "Box is empty";
        SendCallback.gameObject.SetActive(true);
    }

    public void BoxIsNotEmpty()
    {
        robotControl.Callback = "Box is not empty";
        SendCallback.gameObject.SetActive(true);
    }
}
