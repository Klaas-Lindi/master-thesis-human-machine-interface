using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiTextAnimation : MonoBehaviour {

    public bool ScreenSpaceOverlay = true;
    public float upTime;
    public float transitionTime;
    private float startTime;
    private bool activeAnimation;
    
	// Use this for initialization
	void Start () {
        activeAnimation = false;
        if (ScreenSpaceOverlay)
        {
            this.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            this.GetComponent<Canvas>().targetDisplay = 1;
            this.GetComponentInChildren<Text>().transform.rotation = new Quaternion();
            this.GetComponentInChildren<Text>().rectTransform.sizeDelta = new Vector2(200, 30);
            this.GetComponentInChildren<Text>().rectTransform.anchoredPosition = new Vector2(0, 0);
            this.GetComponentInChildren<Text>().rectTransform.anchorMin = new Vector2(0, 0);
            this.GetComponentInChildren<Text>().rectTransform.anchorMax = new Vector2(0, 0);
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (activeAnimation)
        {
            float deltaTime = (Time.realtimeSinceStartup - startTime);
            if (deltaTime < upTime)
            {
                return;
            }
            else if ((deltaTime - upTime) <= transitionTime)
            {
                this.GetComponentInChildren<CanvasRenderer>().SetAlpha(1 - ((deltaTime - upTime) / transitionTime));
            }
            else
            {
                this.gameObject.SetActive(false);
            }
        }
	}

    public void startDeclineAnimation()
    {
        activeAnimation = true; 
        this.gameObject.SetActive(true);
        startTime = Time.realtimeSinceStartup;
    }
}
