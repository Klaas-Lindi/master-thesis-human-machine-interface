using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendCallback : MonoBehaviour
{
    public SemiautonomousHandler semiautonomousHandler;
    public RobotControlSAINT robotControl;
    public MessageToUser messageToUser;
    public GameObject sendCallback;
    private bool received = false;

    void Start()
    {
        received = false;
        messageToUser.setMessage("callback not received yet, please wait");
        StartCoroutine(WaitAndExecute());
        InvokeRepeating("Sending", 0f, 1f);
    }

    bool ReceivingToken()
    {
        if (received)
            return false;
        else
            return true;
    }

    IEnumerator WaitAndExecute()
    {
        yield return new WaitWhile(ReceivingToken);
        semiautonomousHandler.CallbackReceived();
        sendCallback.gameObject.SetActive(false);
    }

    public void Sending()
    {
        if (!received)
            robotControl.Callback = semiautonomousHandler.Callback;
    }

    public void Received()
    {
        received = true;
    }
}