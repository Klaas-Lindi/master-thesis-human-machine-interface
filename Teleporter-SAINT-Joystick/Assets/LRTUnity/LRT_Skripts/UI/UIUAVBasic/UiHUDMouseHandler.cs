using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiHUDMouseHandler : MonoBehaviour {

    private bool mouseOverHUD = false;

    public bool isMouseInDisplay()
    {
        return mouseOverHUD;
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnMouseEnter()
    {
        mouseOverHUD = true;
    }

    private void OnMouseExit()
    {
        mouseOverHUD = false;
    }
}
