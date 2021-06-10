using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAutocircleHandler : MonoBehaviour {

    public GameObject buttonACSetWaypoints;
    public GameObject buttonACSetConfirm;
    public GameObject buttonACSetRadius;
    public GameObject buttonACSetHeight;

    private bool toogle = false;

    // Use this for initialization
    void Start () {
        buttonACSetWaypoints.SetActive(false);
        buttonACSetConfirm.SetActive(false);
        buttonACSetRadius.SetActive(false);
        buttonACSetHeight.SetActive(false);

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void acButtonOnClick()
    {
        toogle = !toogle;
        buttonACSetWaypoints.SetActive(true);
        buttonACSetConfirm.SetActive(true);
        buttonACSetRadius.SetActive(true);
        buttonACSetHeight.SetActive(true);
        buttonACSetWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetConfirm.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetRadius.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetHeight.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);

    }

    public void acButtonOnClick(bool state)
    {
        toogle = state;
        if (state)
        {
            buttonACSetWaypoints.SetActive(true);
            buttonACSetConfirm.SetActive(true);
            buttonACSetRadius.SetActive(true);
            buttonACSetHeight.SetActive(true);
        }
        buttonACSetWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetConfirm.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetRadius.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonACSetHeight.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);

    }
}
