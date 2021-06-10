using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiCursor : MonoBehaviour {

    public enum UICursorMode { NONE, AUTOSCAN_CIRCLE, WAYPOINT };
    OperatorState cursorOperatorState;

	// Use this for initialization
	void Awake () {
        cursorOperatorState = this.GetComponent<OperatorState>();
    }
	
	// Update is called once per frame
	void Update () {
        this.transform.position = cursorOperatorState.OperatorPose.position;
    }

    public void SetUICursorMode(UICursorMode mode)
    {
        switch(mode)
        {
            case UICursorMode.WAYPOINT:
                cursorOperatorState.StateChanged = true;
                cursorOperatorState.ActiveAutomatism = OperatorState.Automatism.WAYPOINTS;
                break;
            case UICursorMode.AUTOSCAN_CIRCLE:
                cursorOperatorState.StateChanged = true;
                cursorOperatorState.ActiveAutomatism = OperatorState.Automatism.UAV_AUTOSCAN_CIRCLE_SET_POINT;
                break;
            case UICursorMode.NONE:
                cursorOperatorState.StateChanged = true;
                cursorOperatorState.ActiveAutomatism = OperatorState.Automatism.NONE;
                
                this.GetComponent<PMHandler>().pathViewer.ClearPath();
                break;
        }
    }
}
