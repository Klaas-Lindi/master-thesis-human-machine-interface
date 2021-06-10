using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericMiniApp : MonoBehaviour
{
    public RobotControlSAINT robotControl;
    public GameObject SendCallback;
    public GameObject Button1, Button2, Button3;

    public void Button1Clicked()
    {
        robotControl.Callback = "button_1";            // Antwort der App an ROS
        SendCallback.gameObject.SetActive(true);
    }

    public void Button2Clicked()
    {
        robotControl.Callback = "button_2";            // Antwort der App an ROS
        SendCallback.gameObject.SetActive(true);
    }

    public void Button3Clicked()
    {
        robotControl.Callback = "button_3";            // Antwort der App an ROS
        SendCallback.gameObject.SetActive(true);
    }
}
