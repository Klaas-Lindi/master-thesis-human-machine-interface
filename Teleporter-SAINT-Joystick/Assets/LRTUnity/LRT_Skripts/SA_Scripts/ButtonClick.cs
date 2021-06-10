using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClick : MonoBehaviour
{
    public GameObject SelectBoxBase;
    public GameObject deactivatecam;
    public GameObject projection;
    public Button Send, Save, SA_mode, Screenshot;

    void Start()
    {
        
        //SelectBoxBase.SetActive(false);
        deactivatecam.SetActive(true);

        Send.onClick.AddListener(SendBox);
        Save.onClick.AddListener(SaveBox);
        SA_mode.onClick.AddListener(ActivateSAMode);
        
    }

    void SendBox()
    {
       
    }

    void SaveBox()
    { 
  
    }

    void ActivateSAMode()
    {
        SelectBoxBase.SetActive(false);
        deactivatecam.SetActive(false);
    }

    void MakeScreenshot()
    {

    }
}

        

