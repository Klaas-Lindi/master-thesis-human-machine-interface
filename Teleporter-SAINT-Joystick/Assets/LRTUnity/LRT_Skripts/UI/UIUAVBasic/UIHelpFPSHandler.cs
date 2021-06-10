using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIHelpFPSHandler : MonoBehaviour {

    private CanvasGroup childPanel;
    public float transitionStep = 0.01f;

	// Use this for initialization
	void Start () {
        childPanel = this.GetComponentInChildren<CanvasGroup>();
        childPanel.alpha = 0;

    }
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.LeftControl))
        {
            childPanel.alpha += transitionStep;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            childPanel.alpha = 0;
        }

    }
}
