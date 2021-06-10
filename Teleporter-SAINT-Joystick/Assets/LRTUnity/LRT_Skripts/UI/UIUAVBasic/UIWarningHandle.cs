using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIWarningHandle : MonoBehaviour {

    public UavState uavState;

    public Text warningText;
    public Image warningSymbol;
    public Image warningPanel;

    public Sprite symbolOk;
    public Sprite symbolInfo;
    public Sprite symbolError;

    private enum WarningLevel { OK, INFO, ERROR};
    private WarningLevel warningLevel;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        warningLevel = WarningLevel.OK;
        warningText.text = "Connected!";

        if (uavState.Condition == UavState.UavCondition.LANDED)
        {
            warningLevel = WarningLevel.OK;
            warningText.text = "Connected - UAV is on the ground - Please hit Takeoff to start";
        }

        // INFO
        if (uavState.ControlState == UavState.UavControlState.MANUAL)
        {
            warningLevel = WarningLevel.INFO;
            warningText.text = "UAV in Manual Mode - No Control possible!";
        }
        if (uavState.Condition == UavState.UavCondition.IDLE)
        {
            warningLevel = WarningLevel.INFO;
            warningText.text = "UAV in IDLE mode";
        }
        if (uavState.Battery < 40f)
        {
            warningLevel = WarningLevel.INFO;
            warningText.text = "Battery low - Please Return to Home";
        }
        // ERRORs
        if (uavState.Battery < 20f)
        {
            warningLevel = WarningLevel.ERROR;
            warningText.text = "Battery empty! - Please land the UAV immediately!";
        }
        if (uavState.Condition == UavState.UavCondition.ERROR)
        {
            warningLevel = WarningLevel.ERROR;
            warningText.text = "Problem detected! - UAV in ERROR mode";
        }
        if (!uavState.IsConnected)
        {
            warningLevel = WarningLevel.ERROR;
            warningText.text = "Disconnected! - Try to connect...";
        }


        switch (warningLevel)
        {
            case WarningLevel.OK: warningSymbol.sprite = symbolOk; warningPanel.color = Color.green; break;
            case WarningLevel.INFO: warningSymbol.sprite = symbolInfo; warningPanel.color = Color.yellow; break; //= new Color(255/ 255, 174/255, 0, 255 / 255)
            case WarningLevel.ERROR: warningSymbol.sprite = symbolError; warningPanel.color = Color.red; break;
        }
	}
}
