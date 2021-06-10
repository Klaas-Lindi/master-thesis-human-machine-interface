using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWaypointHandler : MonoBehaviour {

    public GameObject buttonWpSetWaypoints;
    public GameObject buttonWpSetConfirm;
    public GameObject buttonWpDeleteWaypoints;
    public GameObject buttonWpSetHeight;

    private bool toogle = false;

    // Use this for initialization
    void Start()
    {
        buttonWpSetWaypoints.SetActive(false);
        buttonWpSetConfirm.SetActive(false);
        buttonWpDeleteWaypoints.SetActive(false);
        buttonWpSetHeight.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void wpButtonOnClick()
    {
        toogle = !toogle;
        buttonWpSetWaypoints.SetActive(true);
        buttonWpSetConfirm.SetActive(true);
        buttonWpDeleteWaypoints.SetActive(true);
        buttonWpSetHeight.SetActive(true);
        buttonWpSetWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpSetConfirm.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpDeleteWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpSetHeight.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);

    }

    public void wpButtonOnClick(bool state)
    {
        toogle = state;
        if (state)
        {
            buttonWpSetWaypoints.SetActive(true);
            buttonWpSetConfirm.SetActive(true);
            buttonWpDeleteWaypoints.SetActive(true);
            buttonWpSetHeight.SetActive(true);
        }
        buttonWpSetWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpSetConfirm.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpDeleteWaypoints.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);
        buttonWpSetHeight.GetComponent<UIButtonAnimation>().setAnimationActive(toogle);

    }
}
