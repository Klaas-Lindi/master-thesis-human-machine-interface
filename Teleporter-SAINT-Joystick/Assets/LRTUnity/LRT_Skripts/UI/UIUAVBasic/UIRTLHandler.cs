using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRTLHandler : MonoBehaviour {

    public UavState uavState;
    public GameObject buttonTL;
    public GameObject buttonRTL;
    public GameObject buttonLanding;

    public Sprite imageTakeoff;
    public Sprite imageLand;

    private bool takeOff = false;
    private bool landMode = false;

    // Use this for initialization
    void Start () {
        buttonRTL.SetActive(false);
        buttonLanding.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () {
		if(uavState.Condition == UavState.UavCondition.LANDED)
        {
            buttonTL.GetComponentsInChildren<Image>()[1].sprite = imageTakeoff;
        }
        else 
        {
            buttonTL.GetComponentsInChildren<Image>()[1].sprite = imageLand;
        }
	}
    public void tlButtonOnClick()
    {
        if (uavState.Condition == UavState.UavCondition.LANDED)
        {
            takeOff = true;
        }
        else
        {
            landMode = !landMode;
            buttonRTL.SetActive(true);
            buttonRTL.GetComponent<UIButtonAnimation>().setAnimationActive(landMode);
            buttonLanding.SetActive(true);
            buttonLanding.GetComponent<UIButtonAnimation>().setAnimationActive(landMode);
        }
    }

    public void tlButtonOnClick(bool state)
    {
        if (uavState.Condition != UavState.UavCondition.LANDED)
        {
            landMode = state;
            if (state)
            {
                buttonRTL.SetActive(state);
                buttonLanding.SetActive(state);
            }
            buttonRTL.GetComponent<UIButtonAnimation>().setAnimationActive(landMode);

            buttonLanding.GetComponent<UIButtonAnimation>().setAnimationActive(landMode);
        }
    }

    public bool GetButtonDown()
    {
        bool result = takeOff;
        takeOff = false;
        return result;
    }
}
