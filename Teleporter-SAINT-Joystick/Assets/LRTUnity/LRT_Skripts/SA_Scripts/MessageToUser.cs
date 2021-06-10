using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MessageToUser : MonoBehaviour
{
    public SAINTState saintState;
    public GameObject popupWindowPanel;
    public Text operatorMessage;

    // Meta msg from the teleoperartor
    private string msg = "";
    /// <summary>
    /// Set or get the operational commands of gnc
    /// </summary>
    public string Msg
    {
        get
        {
            return msg;
        }

        set
        {
            msg = value;
        }
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount == 1)
            popupWindowPanel.gameObject.SetActive(false);

        if (Input.GetMouseButtonDown(0))
            popupWindowPanel.gameObject.SetActive(false);

        if (Input.GetMouseButtonDown(1))
            popupWindowPanel.gameObject.SetActive(false);

        if (Input.GetMouseButtonDown(2))
            popupWindowPanel.gameObject.SetActive(false);
    }

    public void setMessage(string message)
    {
        popupWindowPanel.gameObject.SetActive(true);
        operatorMessage.text = message;
        string[] words = message.Split(' ');                // here you can pull out every command out of the string that is published on the topic "operator_messages"
        if ((words[0] == "close_window") || (words[0] == "close"))                     // to always be able to close the window
        {
            popupWindowPanel.gameObject.SetActive(false);
        }

    }
}
