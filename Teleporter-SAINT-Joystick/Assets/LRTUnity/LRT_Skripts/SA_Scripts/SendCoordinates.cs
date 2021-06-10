using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendCoordinates : MonoBehaviour
{
    public SemiautonomousHandler semiautonomousHandler;
    public RobotControlSAINT robotControl;
    public MessageToUser messageToUser;
    public GameObject sendCoordinates;
    private bool received = false;

    void Start()
    {
        received = false;
        messageToUser.setMessage("coordinates not received yet, please wait");
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
        semiautonomousHandler.CoordinatesReceived();
        sendCoordinates.gameObject.SetActive(false);
    }

    public void Sending()
    {
        if (!received)
            robotControl.ItemSelect = semiautonomousHandler.Coordinates;
    }

    public void Received()
    {
        received = true;
    }
}
