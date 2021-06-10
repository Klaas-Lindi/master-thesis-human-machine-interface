using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityStandardAssets.Characters.FirstPerson;

public class ControllerHandle : MonoBehaviour {

    
    public enum ControllerMode { SPACE_MOUSE, VR, MOUSE_KEYBOARD, SAINT}
    [Header("Controller Mode")]
    public ControllerMode controllerMode;

    // FPS
    [Header("FPS Objects")]
    public GameObject controllerFPS;
    public GameObject UiFPS;
    public Text uiFPSDebug;

    // SpaceMouse
    [Header("SM Objects")]
    public GameObject controllerSM;
    public GameObject UiSM;
    public Text uiSMDebug;

    // VR
    [Header("VR Objects")]
    public GameObject controllerVR;
    public GameObject uiVR;
    public Text uiVRDebug;

    // VR
    [Header("SAINT Objects")]
    public GameObject controllerSAINT;
    public GameObject uiSAINT;
    public Text uiSAINTDebug;

    [Header("General Objects to link")]
    public PMHandler predictiveUAVPmHandler;
    public GNC gnc;
    public Logger logger;

    private void Awake()
    {
        // Disable Controller
        if (controllerSM != null)
            controllerSM.SetActive(false);
        if (controllerFPS != null)
            controllerFPS.SetActive(false);
        if(controllerVR != null)
            controllerVR.SetActive(false);
        if (controllerSAINT != null)
            controllerSAINT.SetActive(false);

        // Disable UI
        if (UiSM != null)
            UiSM.SetActive(false);
        if (UiFPS != null)
            UiFPS.SetActive(false);
        if (uiVR != null)
            uiVR.SetActive(false);
        if (uiSAINT != null)
            uiSAINT.SetActive(false);

        XRSettings.enabled = false;
        switch (controllerMode)
        {
            case ControllerMode.VR:
                XRSettings.enabled = true;
                XRSettings.LoadDeviceByName("OpenVR");
                break;
        }
    }

    // Use this for initialization
    void Start () {
        // set everything to false as default

        switch (controllerMode)
        {
            case ControllerMode.MOUSE_KEYBOARD:
                controllerFPS.SetActive(true);
                UiFPS.SetActive(true);
                controllerFPS.GetComponentInChildren<FPSHandler>().uiCamera.SetActive(true);
                logger.debugText = uiFPSDebug;

                predictiveUAVPmHandler.operatorState = controllerFPS.GetComponentInChildren<OperatorState>();
                gnc.operatorState = controllerFPS.GetComponentInChildren<OperatorState>();
                break;
            case ControllerMode.SPACE_MOUSE:
                controllerSM.SetActive(true);
                UiSM.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                controllerFPS.GetComponentInChildren<FPSHandler>().uiCamera.SetActive(false);
                logger.debugText = uiSMDebug;

                predictiveUAVPmHandler.operatorState = controllerSM.GetComponentInChildren<OperatorState>();
                gnc.operatorState = controllerSM.GetComponentInChildren<OperatorState>();
                if(Display.displays.Length > 1)
                    Display.displays[1].Activate();
                break;
            case ControllerMode.VR:
                XRSettings.enabled = true;
                controllerVR.SetActive(true);
                uiVR.SetActive(true);
                controllerFPS.GetComponentInChildren<FPSHandler>().uiCamera.SetActive(false);
                logger.debugText = uiVRDebug;

                predictiveUAVPmHandler.operatorState = controllerVR.GetComponentInChildren<OperatorState>();
                gnc.operatorState = controllerVR.GetComponentInChildren<OperatorState>();
                break;
            case ControllerMode.SAINT:
                controllerSAINT.SetActive(true);
                uiSAINT.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                //controllerSAINT.GetComponentInChildren<SAINTHandler>().uiCamera.SetActive(true);
                //logger.debugText = uiSAINTDebug;

                //gnc.operatorState = controllerSAINT.GetComponentInChildren<OperatorState>();
                break;
        }
    }
	
	// Update is called once per frame
	void Update () {

    }

    public OperatorState getActiveOperatorState()
    {
        switch (controllerMode)
        {
            case ControllerMode.MOUSE_KEYBOARD:
                return controllerFPS.GetComponentInChildren<OperatorState>();
            case ControllerMode.SPACE_MOUSE:
                return controllerSM.GetComponentInChildren<OperatorState>();
            case ControllerMode.VR:
                return controllerVR.GetComponentInChildren<OperatorState>();
        }
        return null;
    }
}
