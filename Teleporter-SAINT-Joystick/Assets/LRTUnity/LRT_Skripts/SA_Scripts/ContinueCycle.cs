using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinueCycle : MonoBehaviour
{
    public RobotControlSAINT robotControl;
    public GameObject SendCallback;

    public void ContinueBoxCycle()
    {
        robotControl.Callback = "Continue Cycle";
        SendCallback.gameObject.SetActive(true);
    }
}
